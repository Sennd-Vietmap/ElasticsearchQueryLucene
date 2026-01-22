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
        int? take = null,
        System.Collections.Generic.IReadOnlyList<(string Field, bool Ascending)>? sortFields = null,
        bool isCount = false)
    {
        EntityType = entityType;
        LuceneQueryString = luceneQueryString ?? "*:*"; // Default to match all
        Skip = skip;
        Take = take;
        SortFields = sortFields ?? new System.Collections.Generic.List<(string Field, bool Ascending)>();
        IsCount = isCount;
    }

    public bool IsCount { get; }

    public LuceneQueryExpression WithCount()
        => new LuceneQueryExpression(EntityType, LuceneQueryString, Skip, Take, SortFields, true);

    public override Type Type => typeof(IEnumerable<object[]>);
    public override ExpressionType NodeType => ExpressionType.Extension;
    public Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType { get; }
    public string LuceneQueryString { get; }
    public int? Skip { get; }
    public int? Take { get; }
    public System.Collections.Generic.IReadOnlyList<(string Field, bool Ascending)> SortFields { get; }

    public LuceneQueryExpression WithLuceneQuery(string luceneQuery)
        => new(EntityType, luceneQuery, Skip, Take, SortFields);

    public LuceneQueryExpression WithSkip(int skip)
        => new(EntityType, LuceneQueryString, skip, Take, SortFields);

    public LuceneQueryExpression WithTake(int take)
        => new(EntityType, LuceneQueryString, Skip, take, SortFields);

    public LuceneQueryExpression WithSort(string field, bool ascending)
    {
        var newSort = new System.Collections.Generic.List<(string Field, bool Ascending)>(SortFields)
        {
            (field, ascending)
        };
        return new LuceneQueryExpression(EntityType, LuceneQueryString, Skip, Take, newSort);
    }

    public void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append($"LuceneQuery({EntityType.Name})");
        if (LuceneQueryString != "*:*")
        {
            expressionPrinter.Append($".Where({LuceneQueryString})");
        }
        foreach (var sort in SortFields)
        {
            expressionPrinter.Append($".OrderBy({sort.Field}, {(sort.Ascending ? "asc" : "desc")})");
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
