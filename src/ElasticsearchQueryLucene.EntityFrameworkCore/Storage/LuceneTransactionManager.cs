using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

/// <summary>
/// Transaction manager for Lucene provider.
/// Lucene doesn't support traditional database transactions, so this implementation
/// provides stub implementations that either throw NotSupportedException or return null.
/// </summary>
public class LuceneTransactionManager : IDbContextTransactionManager, ITransactionEnlistmentManager
{
    private const string TransactionsNotSupportedMessage = "Lucene provider does not support transactions.";

    public virtual IDbContextTransaction BeginTransaction()
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual void CommitTransaction()
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual void RollbackTransaction()
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual Transaction? CurrentAmbientTransaction => null;

    public virtual IDbContextTransaction? CurrentTransaction => null;

    public virtual Transaction? EnlistedTransaction => null;

    public virtual void EnlistTransaction(Transaction? transaction)
        => throw new NotSupportedException(TransactionsNotSupportedMessage);

    public virtual void ResetState()
    {
        // No state to reset for Lucene
    }

    public virtual Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        ResetState();
        return Task.CompletedTask;
    }
}
