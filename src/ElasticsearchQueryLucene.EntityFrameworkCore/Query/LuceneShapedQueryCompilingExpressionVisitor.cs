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
        
        // Create the query execution expression
        var executeMethod = typeof(LuceneShapedQueryCompilingExpressionVisitor)
            .GetMethod(nameof(ExecuteLuceneQuery), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(luceneQuery.EntityType.ClrType);

        var executeCall = Expression.Call(
            executeMethod,
            queryContextParameter,
            Expression.Constant(luceneQuery.LuceneQueryString),
            Expression.Constant(luceneQuery.Skip),
            Expression.Constant(luceneQuery.Take));

        return Expression.Lambda(executeCall, queryContextParameter);
    }

    private static IEnumerable<T> ExecuteLuceneQuery<T>(
        QueryContext queryContext,
        string luceneQueryString,
        int? skip,
        int? take) where T : class, new()
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

        // Execute the search
        var maxResults = (skip ?? 0) + (take ?? 1000);
        var topDocs = searcher.Search(query, maxResults);

        // Apply skip/take
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
