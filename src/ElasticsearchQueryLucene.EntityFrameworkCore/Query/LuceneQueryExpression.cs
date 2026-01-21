using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryExpression : Expression, IPrintableExpression
{
    public LuceneQueryExpression(IEntityType entityType)
    {
        EntityType = entityType;
    }

    public override Type Type => typeof(IEnumerable<>).MakeGenericType(EntityType.ClrType);
    public override ExpressionType NodeType => ExpressionType.Extension;
    public IEntityType EntityType { get; }

    public void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append("LuceneQueryExpression(");
        expressionPrinter.Append(EntityType.Name);
        expressionPrinter.Append(")");
    }
}
