using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using ElasticsearchQueryLucene.EntityFrameworkCore.Infrastructure;

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
    public Lucene.Net.Store.Directory? Directory { get; }

    public LuceneQueryContext(QueryContextDependencies dependencies) : base(dependencies)
    {
        // Get the Lucene directory from the context options
        var extension = dependencies.CurrentContext.Context.GetService<IDbContextOptions>()
            .FindExtension<LuceneDbContextOptionsExtension>();
        Directory = extension?.LuceneDirectory;
    }
}
