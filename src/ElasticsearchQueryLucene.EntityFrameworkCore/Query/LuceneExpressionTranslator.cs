using System.Linq.Expressions;
using System.Text;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

/// <summary>
/// Translates LINQ expression trees to Lucene query syntax.
/// </summary>
public class LuceneExpressionTranslator : ExpressionVisitor
{
    private readonly StringBuilder _queryBuilder = new();
    private string? _currentFieldName;

    public string Translate(Expression expression)
    {
        _queryBuilder.Clear();
        _currentFieldName = null;
        Visit(expression);
        return _queryBuilder.ToString();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Equal:
                TranslateEquality(node);
                break;
            case ExpressionType.NotEqual:
                _queryBuilder.Append("NOT (");
                TranslateEquality(node);
                _queryBuilder.Append(")");
                break;
            case ExpressionType.AndAlso:
                _queryBuilder.Append("(");
                Visit(node.Left);
                _queryBuilder.Append(" AND ");
                Visit(node.Right);
                _queryBuilder.Append(")");
                break;
            case ExpressionType.OrElse:
                _queryBuilder.Append("(");
                Visit(node.Left);
                _queryBuilder.Append(" OR ");
                Visit(node.Right);
                _queryBuilder.Append(")");
                break;
            case ExpressionType.GreaterThan:
                TranslateComparison(node, "{", "TO *]");
                break;
            case ExpressionType.GreaterThanOrEqual:
                TranslateComparison(node, "[", "TO *]");
                break;
            case ExpressionType.LessThan:
                TranslateComparison(node, "[* TO ", "}");
                break;
            case ExpressionType.LessThanOrEqual:
                TranslateComparison(node, "[* TO ", "]");
                break;
            default:
                throw new NotSupportedException($"Binary operator {node.NodeType} is not supported.");
        }
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression?.NodeType == ExpressionType.Parameter)
        {
            _currentFieldName = node.Member.Name;
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        // Value will be used in context
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "Contains" && node.Object != null)
        {
            // String.Contains
            Visit(node.Object);
            var value = GetValue(node.Arguments[0]);
            _queryBuilder.Append($"{_currentFieldName}:*{EscapeValue(value)}*");
        }
        else if (node.Method.Name == "StartsWith" && node.Object != null)
        {
            Visit(node.Object);
            var value = GetValue(node.Arguments[0]);
            _queryBuilder.Append($"{_currentFieldName}:{EscapeValue(value)}*");
        }
        else if (node.Method.Name == "EndsWith" && node.Object != null)
        {
            Visit(node.Object);
            var value = GetValue(node.Arguments[0]);
            _queryBuilder.Append($"{_currentFieldName}:*{EscapeValue(value)}");
        }
        else
        {
            throw new NotSupportedException($"Method {node.Method.Name} is not supported.");
        }
        return node;
    }

    private void TranslateEquality(BinaryExpression node)
    {
        Visit(node.Left);
        var value = GetValue(node.Right);
        _queryBuilder.Append($"{_currentFieldName}:{EscapeValue(value)}");
    }

    private void TranslateComparison(BinaryExpression node, string prefix, string suffix)
    {
        Visit(node.Left);
        var value = GetValue(node.Right);
        _queryBuilder.Append($"{_currentFieldName}:{prefix}{EscapeValue(value)} {suffix}");
    }

    private static object? GetValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expression is MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        throw new NotSupportedException($"Cannot extract value from expression type {expression.NodeType}");
    }

    private static string EscapeValue(object? value)
    {
        if (value == null) return "null";
        
        var str = value.ToString() ?? "";
        // Escape special Lucene characters
        var specialChars = new[] { '+', '-', '&', '|', '!', '(', ')', '{', '}', '[', ']', '^', '"', '~', '*', '?', ':', '\\', '/' };
        foreach (var ch in specialChars)
        {
            str = str.Replace(ch.ToString(), "\\" + ch);
        }
        return str;
    }
}
