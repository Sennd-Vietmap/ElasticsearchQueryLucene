using System;
using System.Linq.Expressions;
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
        // For now, return a constant to satisfy the build
        // In reality, this would compile a method call to Lucene search
        return Expression.Constant(null);
    }
}
