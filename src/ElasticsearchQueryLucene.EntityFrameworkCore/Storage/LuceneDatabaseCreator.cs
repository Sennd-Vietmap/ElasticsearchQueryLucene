using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneDatabaseCreator : IDatabaseCreator
{
    public bool EnsureCreated() => true;
    public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public bool EnsureDeleted() => true;
    public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public bool CanConnect() => true;
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
}
