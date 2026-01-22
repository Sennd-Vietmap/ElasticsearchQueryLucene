using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryableMethodTranslatingExpressionVisitor : QueryableMethodTranslatingExpressionVisitor
{
    private readonly LuceneExpressionTranslator _translator = new();

    public LuceneQueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext, subquery: false)
    {
    }

    protected LuceneQueryableMethodTranslatingExpressionVisitor(
        LuceneQueryableMethodTranslatingExpressionVisitor visitor)
        : base(visitor.Dependencies, visitor.QueryCompilationContext, subquery: true)
    {
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
        // For Phase 5, we only support identity projection (selecting the entity itself)
        // Full projection support will come in Phase 7
        if (selector.Body == selector.Parameters[0])
        {
            return source;
        }

        // For now, return null for complex projections
        return null;
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
