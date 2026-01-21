using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneDatabaseWrapper : IDatabase
{
    private readonly ILuceneDatabase _luceneDatabase;

    public LuceneDatabaseWrapper(ILuceneDatabase luceneDatabase)
    {
        _luceneDatabase = luceneDatabase;
    }

    public int SaveChanges(IList<IUpdateEntry> entries) => _luceneDatabase.SaveChanges(entries);

    public Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken)
        => _luceneDatabase.SaveChangesAsync(entries, cancellationToken);

    public Func<QueryContext, TResult> CompileQuery<TResult>(Expression query, bool async)
    {
        throw new NotImplementedException();
    }

    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async)
    {
        throw new NotImplementedException();
    }
}
