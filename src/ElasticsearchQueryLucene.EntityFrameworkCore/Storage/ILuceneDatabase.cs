using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Update;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public interface ILuceneDatabase
{
    int SaveChanges(IList<IUpdateEntry> entries);
    Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken);
}
