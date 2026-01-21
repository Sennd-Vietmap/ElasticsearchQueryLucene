using System.Text;
using ElasticsearchQueryLucene.Core.Interfaces;
using ElasticsearchQueryLucene.Core.Models;
using ElasticsearchQueryLucene.Core.Utils;

namespace ElasticsearchQueryLucene.Core.Converters;

public class LuceneQueryVisitor : IQueryVisitor
{
    private readonly StringBuilder _sb = new();

    public string GetResult() => _sb.ToString().Trim();

    public void Visit(TermQueryNode node)
    {
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:{LuceneEscapeUtils.EscapeValue(node.Value)}");
    }

    public void Visit(TermsQueryNode node)
    {
        var values = string.Join(" OR ", node.Values.Select(LuceneEscapeUtils.EscapeValue));
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:({values})");
    }

    public void Visit(MatchQueryNode node)
    {
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:({node.Value})");
    }

    public void Visit(MatchPhraseQueryNode node)
    {
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:\"{node.Value}\"");
    }

    public void Visit(PrefixQueryNode node)
    {
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:{LuceneEscapeUtils.EscapeValue(node.Value)}*");
    }

    public void Visit(WildcardQueryNode node)
    {
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:{node.Value}");
    }

    public void Visit(FuzzyQueryNode node)
    {
        var fuzziness = node.Fuzziness ?? 2;
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:{LuceneEscapeUtils.EscapeValue(node.Value)}~{fuzziness}");
    }

    public void Visit(RegexpQueryNode node)
    {
        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:/{node.Value}/");
    }

    public void Visit(ExistsQueryNode node)
    {
        _sb.Append($"_exists_:{LuceneEscapeUtils.EscapeField(node.Field)}");
    }

    public void Visit(IdsQueryNode node)
    {
        var values = string.Join(" ", node.Values.Select(v => $"\"{v}\""));
        _sb.Append($"_id:({values})");
    }

    public void Visit(RangeQueryNode node)
    {
        var startBracket = node.Gt != null ? "{" : "[";
        var startValue = node.Gte ?? node.Gt ?? "*";
        var endBracket = node.Lt != null ? "}" : "]";
        var endValue = node.Lte ?? node.Lt ?? "*";

        _sb.Append($"{LuceneEscapeUtils.EscapeField(node.Field)}:{startBracket}{startValue} TO {endValue}{endBracket}");
    }

    public void Visit(BoolQueryNode node)
    {
        var clauses = new List<string>();

        foreach (var must in node.Must)
        {
            var visitor = new LuceneQueryVisitor();
            must.Accept(visitor);
            var res = visitor.GetResult();
            clauses.Add($"+{WrapIfComplex(must, res)}");
        }

        foreach (var filter in node.Filter)
        {
            var visitor = new LuceneQueryVisitor();
            filter.Accept(visitor);
            var res = visitor.GetResult();
            clauses.Add($"+{WrapIfComplex(filter, res)}");
        }

        foreach (var mustNot in node.MustNot)
        {
            var visitor = new LuceneQueryVisitor();
            mustNot.Accept(visitor);
            var res = visitor.GetResult();
            clauses.Add($"-{WrapIfComplex(mustNot, res)}");
        }

        if (node.Should.Any())
        {
            var shouldClauses = new List<string>();
            foreach (var should in node.Should)
            {
                var visitor = new LuceneQueryVisitor();
                should.Accept(visitor);
                shouldClauses.Add(visitor.GetResult());
            }

            var shouldStr = string.Join(" OR ", shouldClauses);
            if (clauses.Any())
            {
                clauses.Add($"+({shouldStr})");
            }
            else
            {
                clauses.Add($"({shouldStr})");
            }
        }

        _sb.Append(string.Join(" ", clauses));
    }

    private string WrapIfComplex(QueryNode node, string result)
    {
        if (node is BoolQueryNode) return $"({result})";
        return result;
    }
}
