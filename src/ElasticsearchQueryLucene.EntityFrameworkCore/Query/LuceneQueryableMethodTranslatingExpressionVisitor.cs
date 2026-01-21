using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryableMethodTranslatingExpressionVisitor : QueryableMethodTranslatingExpressionVisitor
{
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
        return new ShapedQueryExpression(
            queryExpression,
            Expression.Parameter(entityType.ClrType, "e"));
    }

    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
        => new LuceneQueryableMethodTranslatingExpressionVisitor(this);

    protected override ShapedQueryExpression? TranslateAll(ShapedQueryExpression source, LambdaExpression predicate) => null;
    protected override ShapedQueryExpression? TranslateAny(ShapedQueryExpression source, LambdaExpression? predicate) => null;
    protected override ShapedQueryExpression? TranslateAverage(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateCast(ShapedQueryExpression source, Type castType) => null;
    protected override ShapedQueryExpression? TranslateConcat(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;
    protected override ShapedQueryExpression? TranslateContains(ShapedQueryExpression source, Expression item) => null;
    protected override ShapedQueryExpression? TranslateCount(ShapedQueryExpression source, LambdaExpression? predicate) => null;
    protected override ShapedQueryExpression? TranslateDefaultIfEmpty(ShapedQueryExpression source, Expression? defaultValue) => null;
    protected override ShapedQueryExpression? TranslateDistinct(ShapedQueryExpression source) => null;
    protected override ShapedQueryExpression? TranslateElementAtOrDefault(ShapedQueryExpression source, Expression index, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateExcept(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;
    protected override ShapedQueryExpression? TranslateFirstOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateGroupBy(ShapedQueryExpression source, LambdaExpression keySelector, LambdaExpression? elementSelector, LambdaExpression? resultSelector) => null;
    protected override ShapedQueryExpression? TranslateGroupJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateIntersect(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;
    protected override ShapedQueryExpression? TranslateJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateLastOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateLeftJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateLongCount(ShapedQueryExpression source, LambdaExpression? predicate) => null;
    protected override ShapedQueryExpression? TranslateMax(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateMin(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateOfType(ShapedQueryExpression source, Type ofType) => null;
    protected override ShapedQueryExpression? TranslateOrderBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending) => null;
    protected override ShapedQueryExpression? TranslateReverse(ShapedQueryExpression source) => null;
    protected override ShapedQueryExpression? TranslateRightJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateSelect(ShapedQueryExpression source, LambdaExpression selector) => null;
    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression collectionSelector) => null;
    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression collectionSelector, LambdaExpression resultSelector) => null;
    protected override ShapedQueryExpression? TranslateSingleOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefaultOnEmpty) => null;
    protected override ShapedQueryExpression? TranslateSkip(ShapedQueryExpression source, Expression count) => null;
    protected override ShapedQueryExpression? TranslateSkipWhile(ShapedQueryExpression source, LambdaExpression predicate) => null;
    protected override ShapedQueryExpression? TranslateSum(ShapedQueryExpression source, LambdaExpression selector, Type resultType) => null;
    protected override ShapedQueryExpression? TranslateTake(ShapedQueryExpression source, Expression count) => null;
    protected override ShapedQueryExpression? TranslateTakeWhile(ShapedQueryExpression source, LambdaExpression predicate) => null;
    protected override ShapedQueryExpression? TranslateThenBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending) => null;
    protected override ShapedQueryExpression? TranslateUnion(ShapedQueryExpression source1, ShapedQueryExpression source2) => null;
    protected override ShapedQueryExpression? TranslateWhere(ShapedQueryExpression source, LambdaExpression predicate) => null;
}
