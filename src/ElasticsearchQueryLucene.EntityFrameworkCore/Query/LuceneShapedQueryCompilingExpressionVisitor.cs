using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore.Query;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;

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
        // Use the QueryContextParameter from the compilation context to ensure parameter binding works (no double wrapping)
        var queryContextParameter = QueryCompilationContext.QueryContextParameter;
        
        // Extract sort fields
        var sortFieldsStr = luceneQuery.SortFields.Select(s => s.Field).ToArray();
        var sortAscending = luceneQuery.SortFields.Select(s => s.Ascending).ToArray();

        // Create the query execution expression
        // ExecuteLuceneQuery(queryContext, queryString, skip, take, sortFields, sortAscending, entityType)
        var executeMethod = typeof(LuceneShapedQueryCompilingExpressionVisitor)
            .GetMethod(nameof(ExecuteLuceneQuery), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        var executeCall = Expression.Call(
            executeMethod,
            queryContextParameter,
            Expression.Constant(luceneQuery.LuceneQueryString),
            Expression.Constant(luceneQuery.Skip, typeof(int?)),
            Expression.Constant(luceneQuery.Take, typeof(int?)),
            Expression.Constant(sortFieldsStr),
            Expression.Constant(sortAscending),
            Expression.Constant(luceneQuery.EntityType, typeof(Microsoft.EntityFrameworkCore.Metadata.IEntityType)));

        // Apply Shaper: IEnumerable<object[]> -> IEnumerable<TEntity>
        var shaper = (LambdaExpression)shapedQueryExpression.ShaperExpression;
        var shaperDelegate = shaper.Compile(); // Compile Expression -> Delegate
        
        // Enumerable.Select(executeCall, shaperDelegate)
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(object[]), shaper.ReturnType);

        var shapedCall = Expression.Call(selectMethod, executeCall, Expression.Constant(shaperDelegate));

        // Return the body directly. Do NOT wrap in Expression.Lambda as the caller will do that.
        return shapedCall;
    }


    private static IEnumerable<object[]> ExecuteLuceneQuery(
        QueryContext queryContext,
        string luceneQueryString,
        int? skip,
        int? take,
        string[] sortFields,
        bool[] sortAscending,
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType)
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
        // Build PerFieldAnalyzer based on EntityType attributes
        var defaultAnalyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
        var fieldAnalyzers = new Dictionary<string, Lucene.Net.Analysis.Analyzer>();
        
        foreach (var prop in entityType.GetProperties())
        {
            var attr = prop.PropertyInfo?.GetCustomAttributes(typeof(LuceneFieldAttribute), true)
                        .OfType<LuceneFieldAttribute>().FirstOrDefault();
                        
            if (attr != null && !attr.Tokenized)
            {
                // Use KeywordAnalyzer for non-tokenized fields
                fieldAnalyzers[prop.Name] = new KeywordAnalyzer();
            }
        }
        
        var analyzer = new PerFieldAnalyzerWrapper(defaultAnalyzer, fieldAnalyzers);

        // Parse the Lucene query string
        var parser = new Lucene.Net.QueryParsers.Classic.QueryParser(
            Lucene.Net.Util.LuceneVersion.LUCENE_48,
            "_all", 
            analyzer);
        
        parser.AllowLeadingWildcard = true;

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
                // Detect sort type based on property type from EntityType
                var propName = sortFields[i];
                var prop = entityType.FindProperty(propName);
                
                var sortType = SortFieldType.STRING;
                if (prop != null)
                {
                    if (prop.ClrType == typeof(int) || prop.ClrType == typeof(int?)) sortType = SortFieldType.INT32;
                    else if (prop.ClrType == typeof(long) || prop.ClrType == typeof(long?)) sortType = SortFieldType.INT64;
                    else if (prop.ClrType == typeof(float) || prop.ClrType == typeof(float?)) sortType = SortFieldType.SINGLE;
                    else if (prop.ClrType == typeof(double) || prop.ClrType == typeof(double?)) sortType = SortFieldType.DOUBLE;
                }
                
                sortFieldList.Add(new SortField(sortFields[i], sortType, !sortAscending[i]));
            }
            sort = new Sort(sortFieldList.ToArray());
        }

        // Execute the search
        var maxResults = (skip ?? 0) + (take ?? 1000);
        TopDocs topDocs;
        
        if (sort != null)
        {
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

        var properties = entityType.GetProperties().ToList();
        
        // Materialize results as object[]
        for (int i = startIndex; i < endIndex; i++)
        {
            var scoreDoc = topDocs.ScoreDocs[i];
            var doc = searcher.Doc(scoreDoc.Doc);
            
            var values = new object[properties.Count];
            
            for (int j = 0; j < properties.Count; j++)
            {
                var prop = properties[j];
                var fieldValue = doc.Get(prop.Name);
                
                if (fieldValue != null)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(fieldValue, prop.ClrType);
                        values[j] = convertedValue;
                    }
                    catch
                    {
                        // defaults
                    }
                }
            }
            
            yield return values;
        }
    }
}
