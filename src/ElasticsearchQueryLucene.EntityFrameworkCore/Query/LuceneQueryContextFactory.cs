using Microsoft.EntityFrameworkCore.Query;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Query;

public class LuceneQueryContextFactory : IQueryContextFactory
{
    private readonly QueryContextDependencies _dependencies;

    public LuceneQueryContextFactory(QueryContextDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public QueryContext Create() => new LuceneQueryContext(_dependencies);
}

public class LuceneQueryContext : QueryContext
{
    public LuceneQueryContext(QueryContextDependencies dependencies) : base(dependencies)
    {
    }
}
