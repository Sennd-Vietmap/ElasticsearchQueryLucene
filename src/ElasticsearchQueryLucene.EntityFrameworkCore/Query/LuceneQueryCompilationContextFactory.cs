using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryCompilationContextFactory : IQueryCompilationContextFactory
{
    private readonly QueryCompilationContextDependencies _dependencies;

    public LuceneQueryCompilationContextFactory(QueryCompilationContextDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public QueryCompilationContext Create(bool async)
    {
        return new LuceneQueryCompilationContext(_dependencies, async);
    }
}

public class LuceneQueryCompilationContext : QueryCompilationContext
{
    public LuceneQueryCompilationContext(QueryCompilationContextDependencies dependencies, bool async)
        : base(dependencies, async)
    {
    }
}
