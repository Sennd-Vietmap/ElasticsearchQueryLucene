using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    public LuceneShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
    }

    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        if (shapedQueryExpression.QueryExpression is not LuceneQueryExpression luceneQuery)
        {
            return Expression.Constant(null);
        }

        // Build a lambda that executes the Lucene query
        var queryContextParameter = Expression.Parameter(typeof(QueryContext), "queryContext");
        
        // Extract sort fields
        var sortFieldsStr = luceneQuery.SortFields.Select(s => s.Field).ToArray();
        var sortAscending = luceneQuery.SortFields.Select(s => s.Ascending).ToArray();

        // Create the query execution expression
        var executeMethod = typeof(LuceneShapedQueryCompilingExpressionVisitor)
            .GetMethod(nameof(ExecuteLuceneQuery), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(luceneQuery.EntityType.ClrType);

        var executeCall = Expression.Call(
            executeMethod,
            queryContextParameter,
            Expression.Constant(luceneQuery.LuceneQueryString),
            Expression.Constant(luceneQuery.Skip, typeof(int?)),
            Expression.Constant(luceneQuery.Take, typeof(int?)),
            Expression.Constant(sortFieldsStr),
            Expression.Constant(sortAscending));

        return Expression.Lambda(executeCall, queryContextParameter);
    }

    private static IEnumerable<T> ExecuteLuceneQuery<T>(
        QueryContext queryContext,
        string luceneQueryString,
        int? skip,
        int? take,
        string[] sortFields,
        bool[] sortAscending) where T : class, new()
    {
        // Get the Lucene directory from the context
        var luceneContext = queryContext as LuceneQueryContext;
        if (luceneContext?.Directory == null)
        {
            yield break;
        }

        using var reader = DirectoryReader.Open(luceneContext.Directory);
        var searcher = new IndexSearcher(reader);

        // Parse the Lucene query string
        var parser = new Lucene.Net.QueryParsers.Classic.QueryParser(
            Lucene.Net.Util.LuceneVersion.LUCENE_48,
            "_all", // Default field
            new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));

        Lucene.Net.Search.Query query;
        try
        {
            query = parser.Parse(luceneQueryString);
        }
        catch
        {
            // If parsing fails, use MatchAllDocsQuery
            query = new MatchAllDocsQuery();
        }

        // Prepare Sort
        Sort? sort = null;
        if (sortFields != null && sortFields.Length > 0)
        {
            var sortFieldList = new List<SortField>();
            for (int i = 0; i < sortFields.Length; i++)
            {
                // Default to STRING sort for now. In a real scenario, we'd map types.
                // Reverse is !ascending
                sortFieldList.Add(new SortField(sortFields[i], SortFieldType.STRING, !sortAscending[i]));
            }
            sort = new Sort(sortFieldList.ToArray());
        }

        // Execute the search
        var maxResults = (skip ?? 0) + (take ?? 1000);
        TopDocs topDocs;
        
        if (sort != null)
        {
            // Search with matching, N filters, max items, and Sort
            topDocs = searcher.Search(query, null, maxResults, sort);
        }
        else
        {
            topDocs = searcher.Search(query, maxResults);
        }

        // Apply skip/take (materialization limits)
        var startIndex = skip ?? 0;
        var endIndex = take.HasValue ? startIndex + take.Value : topDocs.ScoreDocs.Length;
        endIndex = Math.Min(endIndex, topDocs.ScoreDocs.Length);

        // Materialize results
        for (int i = startIndex; i < endIndex; i++)
        {
            var scoreDoc = topDocs.ScoreDocs[i];
            var doc = searcher.Doc(scoreDoc.Doc);
            
            var entity = new T();
            var properties = typeof(T).GetProperties();
            
            foreach (var prop in properties)
            {
                var fieldValue = doc.Get(prop.Name);
                if (fieldValue != null)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(fieldValue, prop.PropertyType);
                        prop.SetValue(entity, convertedValue);
                    }
                    catch
                    {
                        // Skip properties that can't be converted
                    }
                }
            }
            
            yield return entity;
        }
    }
}
