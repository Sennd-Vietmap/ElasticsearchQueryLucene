using System.Collections.Generic;
using Lucene.Net.Store;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Infrastructure;

public class LuceneDbContextOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    public Lucene.Net.Store.Directory? LuceneDirectory { get; private set; }
    public string? IndexName { get; private set; }

    public LuceneDbContextOptionsExtension()
    {
    }

    protected LuceneDbContextOptionsExtension(LuceneDbContextOptionsExtension copyFrom)
    {
        LuceneDirectory = copyFrom.LuceneDirectory;
        IndexName = copyFrom.IndexName;
    }

    public virtual DbContextOptionsExtensionInfo Info => _info ??= new LuceneOptionsExtensionInfo(this);

    public virtual void ApplyServices(IServiceCollection services)
    {
        var builder = new EntityFrameworkServicesBuilder(services);
        
        builder.TryAdd<IConventionSetPlugin, Metadata.Conventions.LuceneConventionSetPlugin>();
        builder.TryAdd<IDatabase, Storage.LuceneDatabaseWrapper>();
        builder.TryAdd<IDatabaseCreator, Storage.LuceneDatabaseCreator>();
        builder.TryAdd<Storage.ILuceneDatabase, Storage.LuceneDatabase>();
        builder.TryAdd<ITypeMappingSource, Storage.LuceneTypeMappingSource>();
        
        // Query services required by model builder and runtime
        builder.TryAdd<IQueryContextFactory, Query.LuceneQueryContextFactory>();
        builder.TryAdd<IQueryCompilationContextFactory, Query.LuceneQueryCompilationContextFactory>();
        builder.TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, Query.LuceneQueryableMethodTranslatingExpressionVisitorFactory>();
        builder.TryAdd<IShapedQueryCompilingExpressionVisitorFactory, Query.LuceneShapedQueryCompilingExpressionVisitorFactory>();
        builder.TryAdd<IStructuralTypeMaterializerSource, Query.LuceneEntityMaterializerSource>();
    }

    public virtual void Validate(IDbContextOptions options)
    {
    }

    public virtual LuceneDbContextOptionsExtension WithDirectory(Lucene.Net.Store.Directory directory)
    {
        var clone = Clone();
        clone.LuceneDirectory = directory;
        return clone;
    }

    public virtual LuceneDbContextOptionsExtension WithIndexName(string indexName)
    {
        var clone = Clone();
        clone.IndexName = indexName;
        return clone;
    }

    protected virtual LuceneDbContextOptionsExtension Clone() => new(this);

    private sealed class LuceneOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        public LuceneOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        public override bool IsDatabaseProvider => true;

        public override string LogFragment => "using Lucene index search ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Lucene:IndexName"] = ((LuceneDbContextOptionsExtension)Extension).IndexName ?? "Default";
        }
    }
}
