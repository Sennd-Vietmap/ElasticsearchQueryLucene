using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneExecutionStrategyFactory : IExecutionStrategyFactory
{
    private readonly ExecutionStrategyDependencies _dependencies;

    public LuceneExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public IExecutionStrategy Create() => new NonRetryingExecutionStrategy(_dependencies);
}
