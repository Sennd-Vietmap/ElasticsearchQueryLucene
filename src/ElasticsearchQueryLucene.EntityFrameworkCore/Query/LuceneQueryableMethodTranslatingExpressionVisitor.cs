using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryableMethodTranslatingExpressionVisitor : QueryableMethodTranslatingExpressionVisitor
{
    private readonly LuceneExpressionTranslator _translator;

    public LuceneQueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext,
        Microsoft.EntityFrameworkCore.Storage.ITypeMappingSource typeMappingSource)
        : base(dependencies, queryCompilationContext, subquery: false)
    {
        _translator = new LuceneExpressionTranslator(typeMappingSource);
    }

    protected LuceneQueryableMethodTranslatingExpressionVisitor(
        LuceneQueryableMethodTranslatingExpressionVisitor visitor)
        : base(visitor.Dependencies, visitor.QueryCompilationContext, subquery: true)
    {
        _translator = visitor._translator;
    }

    protected override ShapedQueryExpression CreateShapedQueryExpression(IEntityType entityType)
    {
        var queryExpression = new LuceneQueryExpression(entityType);

        // Create shaper: (object[] values) => new Entity { Prop1 = (Type)values[0], ... }
        var valueBufferParameter = Expression.Parameter(typeof(object[]), "values");
        var properties = entityType.GetProperties().ToList();
        var bindings = new System.Collections.Generic.List<MemberBinding>();
        
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            
            // values[i]
            var valueExpression = Expression.ArrayIndex(
                valueBufferParameter,
                Expression.Constant(i)
            );

            // Cast to property type
            var convertedExpression = Expression.Convert(valueExpression, property.ClrType);

            if (property.PropertyInfo != null && property.PropertyInfo.CanWrite)
            {
                bindings.Add(Expression.Bind(property.PropertyInfo, convertedExpression));
            }
        }

        var newEntityExpression = Expression.MemberInit(
            Expression.New(entityType.ClrType),
            bindings
        );

        var shaperLambda = Expression.Lambda(newEntityExpression, valueBufferParameter);

        return new ShapedQueryExpression(
            queryExpression,
            shaperLambda);
    }

    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
        => new LuceneQueryableMethodTranslatingExpressionVisitor(this);

    protected override ShapedQueryExpression? TranslateWhere(ShapedQueryExpression source, LambdaExpression predicate)
    {
        if (source.QueryExpression is not LuceneQueryExpression luceneQuery)
            return null;

        // Translate the predicate to Lucene query syntax
        var luceneQueryString = _translator.Translate(predicate.Body);
        
        // Combine with existing query if present
        var combinedQuery = luceneQuery.LuceneQueryString == "*:*" 
            ? luceneQueryString 
            : $"({luceneQuery.LuceneQueryString}) AND ({luceneQueryString})";

        var newQueryExpression = luceneQuery.WithLuceneQuery(combinedQuery);
        
        return source.Update(newQueryExpression, source.ShaperExpression);
    }

    protected override ShapedQueryExpression? TranslateSkip(ShapedQueryExpression source, Expression count)
    {
        if (source.QueryExpression is not LuceneQueryExpression luceneQuery)
            return null;

        if (count is not ConstantExpression { Value: int skipCount })
            return null;

        var newQueryExpression = luceneQuery.WithSkip(skipCount);
        return source.Update(newQueryExpression, source.ShaperExpression);
    }

    protected override ShapedQueryExpression? TranslateTake(ShapedQueryExpression source, Expression count)
    {
        if (source.QueryExpression is not LuceneQueryExpression luceneQuery)
            return null;

        if (count is not ConstantExpression { Value: int takeCount })
            return null;

        var newQueryExpression = luceneQuery.WithTake(takeCount);
        return source.Update(newQueryExpression, source.ShaperExpression);
    }

    protected override ShapedQueryExpression? TranslateFirstOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefaultOnEmpty)
    {
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate) ?? source;
        }

        if (source.QueryExpression is LuceneQueryExpression luceneQuery)
        {
            var newQueryExpression = luceneQuery.WithTake(1);
            var cardinality = returnDefaultOnEmpty ? ResultCardinality.SingleOrDefault : ResultCardinality.Single;
            return new ShapedQueryExpression(newQueryExpression, source.ShaperExpression).UpdateResultCardinality(cardinality);
        }

        return null;
    }

    protected override ShapedQueryExpression? TranslateSelect(ShapedQueryExpression source, LambdaExpression selector)
    {
        if (source.QueryExpression is not LuceneQueryExpression luceneQuery)
            return null;

        // If it's an identity projection (p => p), just return the source.
        if (selector.Body == selector.Parameters[0])
        {
            return source;
        }

        // We need to rewrite the selector to use an object[] parameter instead of the entity parameter.
        // Current shaper: (object[] values) => new Pet { ... }
        // New selector: (Pet p) => new { p.Name, p.Age }
        // Result shaper: (object[] values) => { var p = source_shaper(values); return new { p.Name, p.Age }; }

        // Better approach: Inline the source shaper into the new selector.
        // Or even better: if the selector accesses properties of Pet, map them directly to the object[] buffer.

        var shaperLambdaExpression = (LambdaExpression)source.ShaperExpression;
        var visitor = new LuceneProjectionShaperExpressionVisitor(
            luceneQuery.EntityType, 
            shaperLambdaExpression.Parameters[0]);
        
        var rewrittenSelectorBody = visitor.Visit(selector.Body);
        var shaperLambda = Expression.Lambda(rewrittenSelectorBody, shaperLambdaExpression.Parameters[0]);

        return source.Update(luceneQuery, shaperLambda);
    }

    private class LuceneProjectionShaperExpressionVisitor : ExpressionVisitor
    {
        private readonly IEntityType _entityType;
        private readonly ParameterExpression _valueBufferParameter;
        private readonly List<IProperty> _properties;

        public LuceneProjectionShaperExpressionVisitor(IEntityType entityType, ParameterExpression valueBufferParameter)
        {
            _entityType = entityType;
            _valueBufferParameter = valueBufferParameter;
            _properties = entityType.GetProperties().ToList();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression param && param.Type == _entityType.ClrType)
            {
                // Property access on the entity parameter
                var property = _entityType.FindProperty(node.Member.Name);
                if (property != null)
                {
                    var index = _properties.IndexOf(property);
                    if (index != -1)
                    {
                        var arrayAccess = Expression.ArrayIndex(
                            _valueBufferParameter,
                            Expression.Constant(index)
                        );

                        return Expression.Convert(arrayAccess, node.Type);
                    }
                }
            }

            return base.VisitMember(node);
        }
    }

    // Stub implementations for unsupported operations (will be implemented in later phases)
    protected override ShapedQueryExpression? TranslateAll(ShapedQueryExpression source, LambdaExpression predicate) => null;
    protected override ShapedQueryExpression? TranslateAny(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate) ?? source;
        }

        if (source.QueryExpression is LuceneQueryExpression luceneQuery)
        {
            var newQueryExpression = luceneQuery.WithCount();
            
            // Shaper: (object[] vals) => ((int)vals[0]) > 0
            var parameter = Expression.Parameter(typeof(object[]), "values");
            var shaper = Expression.Lambda(
                Expression.GreaterThan(
                    Expression.Convert(
                        Expression.ArrayIndex(parameter, Expression.Constant(0)),
                        typeof(int)),
                    Expression.Constant(0)),
                parameter);

            return new ShapedQueryExpression(newQueryExpression, shaper).UpdateResultCardinality(ResultCardinality.Single);
        }
        return null;
    }

    protected override ShapedQueryExpression? TranslateCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
         if (predicate != null)
        {
            source = TranslateWhere(source, predicate) ?? source;
        }

        if (source.QueryExpression is LuceneQueryExpression luceneQuery)
        {
            var newQueryExpression = luceneQuery.WithCount();
            
            // Shaper: (object[] vals) => (int)vals[0]
            var parameter = Expression.Parameter(typeof(object[]), "values");
            var shaper = Expression.Lambda(
                Expression.Convert(
                    Expression.ArrayIndex(parameter, Expression.Constant(0)),
                    typeof(int)),
                parameter);

            return new ShapedQueryExpression(newQueryExpression, shaper).UpdateResultCardinality(ResultCardinality.Single);
        }
        return null;
    }

    protected override ShapedQueryExpression? TranslateDefaultIfEmpty(ShapedQueryExpression source, Expression? defaultValue) => null;
    protected override ShapedQueryExpression? TranslateDistinct(ShapedQueryExpression source) => null;
    protected override ShapedQueryExpression? TranslateElementAtOrDefault(ShapedQueryExpression source, Expression index, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateExcept(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;
    protected override ShapedQueryExpression? TranslateGroupBy(ShapedQueryExpression source, LambdaExpression keySelector, LambdaExpression? elementSelector, LambdaExpression? resultSelector) => null;
    protected override ShapedQueryExpression? TranslateGroupJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateIntersect(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;
    protected override ShapedQueryExpression? TranslateJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateLastOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateLeftJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    
    protected override ShapedQueryExpression? TranslateLongCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        if (predicate != null)
        {
            source = TranslateWhere(source, predicate) ?? source;
        }

        if (source.QueryExpression is LuceneQueryExpression luceneQuery)
        {
            var newQueryExpression = luceneQuery.WithCount();
            
            // Shaper: (object[] vals) => (long)((int)vals[0])
            var parameter = Expression.Parameter(typeof(object[]), "values");
            var shaper = Expression.Lambda(
                Expression.Convert(
                    Expression.Convert(
                        Expression.ArrayIndex(parameter, Expression.Constant(0)),
                        typeof(int)),
                    typeof(long)),
                parameter);

            return new ShapedQueryExpression(newQueryExpression, shaper).UpdateResultCardinality(ResultCardinality.Single);
        }
        return null;
    }
    protected override ShapedQueryExpression? TranslateMax(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateMin(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateOfType(ShapedQueryExpression source, Type ofType) => null;
    protected override ShapedQueryExpression? TranslateOrderBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
    {
        if (source.QueryExpression is not LuceneQueryExpression luceneQuery)
            return null;

        var memberName = GetMemberName(keySelector);
        if (memberName == null) return null;

        var newQueryExpression = luceneQuery.WithSort(memberName, ascending);
        return source.Update(newQueryExpression, source.ShaperExpression);
    }

    protected override ShapedQueryExpression? TranslateThenBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
    {
        if (source.QueryExpression is not LuceneQueryExpression luceneQuery)
            return null;

        var memberName = GetMemberName(keySelector);
        if (memberName == null) return null;

        var newQueryExpression = luceneQuery.WithSort(memberName, ascending);
        return source.Update(newQueryExpression, source.ShaperExpression);
    }

    private static string? GetMemberName(LambdaExpression keySelector)
    {
        if (keySelector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        
        // Handle UnaryExpression (e.g., convert node)
        if (keySelector.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operand)
        {
            return operand.Member.Name;
        }

        return null;
    }

    protected override ShapedQueryExpression? TranslateReverse(ShapedQueryExpression source) => null;
    protected override ShapedQueryExpression? TranslateRightJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression collectionSelector) => null;
    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression collectionSelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateSingleOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateSkipWhile(ShapedQueryExpression source, LambdaExpression predicate) => null;
    protected override ShapedQueryExpression? TranslateSum(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateTakeWhile(ShapedQueryExpression source, LambdaExpression predicate) => null;
    protected override ShapedQueryExpression? TranslateUnion(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;

    protected override ShapedQueryExpression? TranslateAverage(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateCast(ShapedQueryExpression source, Type castType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateConcat(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateContains(ShapedQueryExpression source, Expression item)
    {
        throw new NotImplementedException();
    }
}
