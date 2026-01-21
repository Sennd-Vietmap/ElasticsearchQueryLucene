using ElasticsearchQueryLucene.EntityFrameworkCore.Infrastructure;
using Lucene.Net.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;

public static class LuceneDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseLucene(
        this DbContextOptionsBuilder optionsBuilder,
        Lucene.Net.Store.Directory directory,
        string? indexName = null)
    {
        var extension = optionsBuilder.Options.FindExtension<LuceneDbContextOptionsExtension>()
            ?? new LuceneDbContextOptionsExtension();

        extension = extension.WithDirectory(directory);
        
        if (indexName != null)
        {
            extension = extension.WithIndexName(indexName);
        }

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseLucene<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Lucene.Net.Store.Directory directory,
        string? indexName = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseLucene((DbContextOptionsBuilder)optionsBuilder, directory, indexName);
}
