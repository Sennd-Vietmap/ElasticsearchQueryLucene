using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

/// <summary>
/// Represents a Lucene query expression that can be translated to a Lucene search.
/// This is the server-side representation of a query.
/// </summary>
public class LuceneQueryExpression : Expression, IPrintableExpression
{
    public LuceneQueryExpression(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType,
        string? luceneQueryString = null,
        int? skip = null,
        int? take = null)
    {
        EntityType = entityType;
        LuceneQueryString = luceneQueryString ?? "*:*"; // Default to match all
        Skip = skip;
        Take = take;
    }

    public override Type Type => typeof(IEnumerable<>).MakeGenericType(EntityType.ClrType);
    public override ExpressionType NodeType => ExpressionType.Extension;
    public Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType { get; }
    public string LuceneQueryString { get; }
    public int? Skip { get; }
    public int? Take { get; }

    public LuceneQueryExpression WithLuceneQuery(string luceneQuery)
        => new(EntityType, luceneQuery, Skip, Take);

    public LuceneQueryExpression WithSkip(int skip)
        => new(EntityType, LuceneQueryString, skip, Take);

    public LuceneQueryExpression WithTake(int take)
        => new(EntityType, LuceneQueryString, Skip, take);

    public void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append($"LuceneQuery({EntityType.Name})");
        if (LuceneQueryString != "*:*")
        {
            expressionPrinter.Append($".Where({LuceneQueryString})");
        }
        if (Skip.HasValue)
        {
            expressionPrinter.Append($".Skip({Skip})");
        }
        if (Take.HasValue)
        {
            expressionPrinter.Append($".Take({Take})");
        }
    }
}
