using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    public LuceneShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
    }

    protected override Expression VisitExtension(Expression extensionExpression)
    {
        if (extensionExpression is ShapedQueryExpression shapedQueryExpression)
        {
            return VisitShapedQuery(shapedQueryExpression);
        }

        return base.VisitExtension(extensionExpression);
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
            Expression.Constant(luceneQuery.Skip, typeof(Expression)),
            Expression.Constant(luceneQuery.Take, typeof(Expression)),
            Expression.Constant(sortFieldsStr),
            Expression.Constant(sortAscending),
            Expression.Constant(luceneQuery.EntityType, typeof(Microsoft.EntityFrameworkCore.Metadata.IEntityType)),
            Expression.Constant(luceneQuery.IsCount));

        // Apply Shaper: IEnumerable<object[]> -> IEnumerable<TEntity>
        var shaper = (LambdaExpression)shapedQueryExpression.ShaperExpression;
        var shaperDelegate = shaper.Compile(); // Compile Expression -> Delegate
        
        // Enumerable.Select(executeCall, shaperDelegate)
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(object[]), shaper.ReturnType);

        var shapedCall = Expression.Call(selectMethod, executeCall, Expression.Constant(shaperDelegate));

        // Return the body directly. Do NOT wrap in Expression.Lambda as the caller will do that.
        var finalCall = shapedCall;

        // Apply Tracking if needed
        // Only track if the result type is the Entity type (avoid Count(), Projections, etc.)
        if (QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll
            && shaper.ReturnType == luceneQuery.EntityType.ClrType)
        {
             var methodInfo = typeof(LuceneShapedQueryCompilingExpressionVisitor)
                .GetMethod(nameof(TrackEntities), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
             
             var trackMethod = methodInfo!.MakeGenericMethod(shaper.ReturnType);

             finalCall = Expression.Call(
                trackMethod, 
                shapedCall, 
                queryContextParameter,
                Expression.Constant(luceneQuery.EntityType));
        }

        if (shapedQueryExpression.ResultCardinality == ResultCardinality.Single)
        {
             var singleMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Single" && m.GetParameters().Length == 1)
                .MakeGenericMethod(shaper.ReturnType);
            
             return Expression.Call(singleMethod, finalCall);
        }
        else if (shapedQueryExpression.ResultCardinality == ResultCardinality.SingleOrDefault)
        {
             var singleOrDefaultMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "SingleOrDefault" && m.GetParameters().Length == 1)
                .MakeGenericMethod(shaper.ReturnType);
            
             return Expression.Call(singleOrDefaultMethod, finalCall);
        }

        return finalCall;
    }

    private class LuceneQueryParser : Lucene.Net.QueryParsers.Classic.QueryParser
    {
        private readonly Microsoft.EntityFrameworkCore.Metadata.IEntityType _entityType;

        public LuceneQueryParser(Lucene.Net.Util.LuceneVersion matchVersion, string f, Lucene.Net.Analysis.Analyzer a, Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType)
            : base(matchVersion, f, a)
        {
            _entityType = entityType;
        }

        protected override Lucene.Net.Search.Query GetFieldQuery(string field, string queryText, bool quoted)
        {
            var p = _entityType.FindProperty(field);
            if (p != null)
            {
                var targetType = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                if (targetType == typeof(int))
                {
                    if (int.TryParse(queryText, out var i))
                    {
                        return Lucene.Net.Search.NumericRangeQuery.NewInt32Range(field, i, i, true, true);
                    }
                }
                else if (targetType == typeof(long))
                {
                    if (long.TryParse(queryText, out var l))
                    {
                        return Lucene.Net.Search.NumericRangeQuery.NewInt64Range(field, l, l, true, true);
                    }
                }
                else if (targetType == typeof(float))
                {
                    if (float.TryParse(queryText, out var f))
                    {
                        return Lucene.Net.Search.NumericRangeQuery.NewSingleRange(field, f, f, true, true);
                    }
                }
                else if (targetType == typeof(double))
                {
                    if (double.TryParse(queryText, out var d))
                    {
                        return Lucene.Net.Search.NumericRangeQuery.NewDoubleRange(field, d, d, true, true);
                    }
                }
            }
            return base.GetFieldQuery(field, queryText, quoted);
        }

        protected override Lucene.Net.Search.Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            var p = _entityType.FindProperty(field);
            if (p != null)
            {
                var targetType = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                if (targetType == typeof(int))
                {
                    int? i1 = part1 == "*" ? null : int.TryParse(part1, out var v1) ? v1 : null;
                    int? i2 = part2 == "*" ? null : int.TryParse(part2, out var v2) ? v2 : null;
                    return Lucene.Net.Search.NumericRangeQuery.NewInt32Range(field, i1, i2, startInclusive, endInclusive);
                }
                else if (targetType == typeof(long))
                {
                    long? l1 = part1 == "*" ? null : long.TryParse(part1, out var v1) ? v1 : null;
                    long? l2 = part2 == "*" ? null : long.TryParse(part2, out var v2) ? v2 : null;
                    return Lucene.Net.Search.NumericRangeQuery.NewInt64Range(field, l1, l2, startInclusive, endInclusive);
                }
                else if (targetType == typeof(float))
                {
                    float? f1 = part1 == "*" ? null : float.TryParse(part1, out var v1) ? v1 : null;
                    float? f2 = part2 == "*" ? null : float.TryParse(part2, out var v2) ? v2 : null;
                    return Lucene.Net.Search.NumericRangeQuery.NewSingleRange(field, f1, f2, startInclusive, endInclusive);
                }
                else if (targetType == typeof(double))
                {
                    double? d1 = part1 == "*" ? null : double.TryParse(part1, out var v1) ? v1 : null;
                    double? d2 = part2 == "*" ? null : double.TryParse(part2, out var v2) ? v2 : null;
                    return Lucene.Net.Search.NumericRangeQuery.NewDoubleRange(field, d1, d2, startInclusive, endInclusive);
                }
            }
            return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }
    }

    private static IEnumerable<T> TrackEntities<T>(IEnumerable<T> source, QueryContext queryContext, Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType)
    {
        if (source == null) yield break;

        var primaryKey = entityType.FindPrimaryKey();
        // ...
#pragma warning disable EF1001 // Internal EF Core API usage.
        var stateManager = queryContext.Context.GetService<IStateManager>();
#pragma warning restore EF1001 // Internal EF Core API usage.
                              // ...

        foreach (var entity in source)
        {
            if (entity == null) continue;
            if (primaryKey == null)
            {
                yield return entity;
                continue;
            }

            // Identity Resolution
            var keyValues = new object?[primaryKey.Properties.Count];
            for(int i=0; i<primaryKey.Properties.Count; i++)
            {
                keyValues[i] = primaryKey.Properties[i].GetGetter().GetClrValue(entity);
            }

#pragma warning disable EF1001
            var entry = stateManager.TryGetEntry(primaryKey, keyValues!);
#pragma warning restore EF1001

            if (entry != null)
            {
                yield return (T)entry.Entity;
            }
            else
            {
                // Not tracked? Attach.
                // We use Attach because if we just return the new instance, EF doesn't know about it.
                // But since we checked StateManager, we know it's not tracked.
                queryContext.Context.Attach(entity);
                yield return entity;
            }
        }
    }


    private static int? EvaluateInt(Expression? expression, QueryContext queryContext)
    {
        if (expression == null) return null;
        if (expression is ConstantExpression constant) return (int)constant.Value!;
        
        if (expression.NodeType == ExpressionType.Extension && expression is QueryParameterExpression qp)
        {
            if (queryContext.Parameters.TryGetValue(qp.Name, out var value))
            {
                return (int)value!;
            }
        }
        
        try
        {
            var lambda = Expression.Lambda<Func<int>>(Expression.Convert(expression, typeof(int)));
            return lambda.Compile()();
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<object[]> ExecuteLuceneQuery(
        QueryContext queryContext,
        string luceneQueryString,
        Expression? skipExpression,
        Expression? takeExpression,
        string[] sortFields,
        bool[] sortAscending,
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType,
        bool isCount)
    {
        var skip = EvaluateInt(skipExpression, queryContext);
        var take = EvaluateInt(takeExpression, queryContext);
        // Resolve placeholders in luceneQueryString (e.g., @@name@@)
        if (luceneQueryString.Contains("@@"))
        {
            luceneQueryString = Regex.Replace(luceneQueryString, "@@(.+?)@@", match =>
            {
                var paramName = match.Groups[1].Value;
                if (queryContext.Parameters.TryGetValue(paramName, out var value))
                {
                    // Basic formatting for Lucene. For dates, etc., we might need more logic here too.
                    return value?.ToString() ?? "null";
                }
                return match.Value; // Keep if not found (unexpected)
            });
        }

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

        // Parse the Lucene query string using our type-aware parser
        var parser = new LuceneQueryParser(
            Lucene.Net.Util.LuceneVersion.LUCENE_48,
            "_all", 
            analyzer,
            entityType);
        
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

        if (isCount)
        {
             var totalHits = searcher.Search(query, 1).TotalHits;
             yield return new object[] { totalHits };
             yield break;
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
                
                // Set default value for value types to avoid shaper cast exceptions
                if (prop.ClrType.IsValueType && Nullable.GetUnderlyingType(prop.ClrType) == null)
                {
                    values[j] = Activator.CreateInstance(prop.ClrType)!;
                }

                var fieldValue = doc.Get(prop.Name);
                
                if (fieldValue != null)
                {
                    try
                    {
                        var typeMappingSource = queryContext.Context.GetService<Microsoft.EntityFrameworkCore.Storage.ITypeMappingSource>();
                        var mapping = typeMappingSource.FindMapping(prop);
                        var converter = mapping?.Converter;
                        var targetType = converter?.ProviderClrType ?? (Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType);
                        
                        object? convertedValue;
                        if (targetType == typeof(bool))
                        {
                            convertedValue = bool.Parse(fieldValue);
                        }
                        else if (targetType == typeof(int))
                        {
                            convertedValue = int.Parse(fieldValue);
                        }
                        else if (targetType == typeof(long))
                        {
                            convertedValue = long.Parse(fieldValue);
                        }
                        else if (targetType == typeof(float))
                        {
                            convertedValue = float.Parse(fieldValue);
                        }
                        else if (targetType == typeof(double))
                        {
                            convertedValue = double.Parse(fieldValue);
                        }
                        else if (targetType == typeof(DateTime))
                        {
                            convertedValue = DateTime.Parse(fieldValue, null, System.Globalization.DateTimeStyles.RoundtripKind);
                        }
                        else if (targetType == typeof(Guid))
                        {
                            convertedValue = Guid.Parse(fieldValue);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(fieldValue, targetType);
                        }

                        if (converter != null && convertedValue != null)
                        {
                            values[j] = converter.ConvertFromProvider(convertedValue);
                        }
                        else
                        {
                            values[j] = convertedValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error materializing property '{prop.Name}' from value '{fieldValue}': {ex.Message}", ex);
                    }
                }
            }
            
            yield return values;
        }
    }
}
