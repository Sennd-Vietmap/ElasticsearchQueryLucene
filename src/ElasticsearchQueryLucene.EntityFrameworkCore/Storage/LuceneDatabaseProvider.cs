using Microsoft.EntityFrameworkCore.Storage;
using ElasticsearchQueryLucene.EntityFrameworkCore.Infrastructure;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneDatabaseProvider : DatabaseProvider<LuceneDbContextOptionsExtension>
{
    public LuceneDatabaseProvider(DatabaseProviderDependencies dependencies)
        : base(dependencies)
    {
    }

    public override string Name => "Lucene";
}
