using Microsoft.EntityFrameworkCore;
using Lucene.Net.Store;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;

namespace ElasticsearchQueryLucene.FunctionalTests.TestUtilities;

public abstract class LuceneTestBase : IDisposable
{
    private readonly RAMDirectory _directory = new RAMDirectory();

    protected DbContextOptions<TContext> CreateOptions<TContext>(string indexName = "TestIndex")
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseLucene(_directory, indexName)
            .Options;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _directory.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
