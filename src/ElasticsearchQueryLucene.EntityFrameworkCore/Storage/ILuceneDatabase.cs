using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Update;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public interface ILuceneDatabase
{
    int SaveChanges(IList<IUpdateEntry> entries);
    Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken);

    // DX Pack: Index Inspection
    IEnumerable<IReadOnlyDictionary<string, string[]>> GetIndexDocuments(int skip = 0, int take = 100, string? query = null);
}
