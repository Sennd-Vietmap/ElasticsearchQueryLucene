using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneDatabaseWrapper : Database
{
    private readonly ILuceneDatabase _luceneDatabase;

    public LuceneDatabaseWrapper(
        DatabaseDependencies dependencies,
        ILuceneDatabase luceneDatabase)
        : base(dependencies)
    {
        _luceneDatabase = luceneDatabase;
    }

    public override int SaveChanges(IList<IUpdateEntry> entries)
        => _luceneDatabase.SaveChanges(entries);

    public override Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default)
        => _luceneDatabase.SaveChangesAsync(entries, cancellationToken);
}
