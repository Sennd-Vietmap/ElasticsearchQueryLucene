using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
{
    private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
    private readonly Microsoft.EntityFrameworkCore.Storage.ITypeMappingSource _typeMappingSource;

    public LuceneQueryableMethodTranslatingExpressionVisitorFactory(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        Microsoft.EntityFrameworkCore.Storage.ITypeMappingSource typeMappingSource)
    {
        _dependencies = dependencies;
        _typeMappingSource = typeMappingSource;
    }

    public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
        => new LuceneQueryableMethodTranslatingExpressionVisitor(_dependencies, queryCompilationContext, _typeMappingSource);
}
