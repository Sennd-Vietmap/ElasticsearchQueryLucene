using Microsoft.AspNetCore.Builder;
using ElasticsearchQueryLucene.EntityFrameworkCore.Diagnostics;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;

public static class LuceneAppBuilderExtensions
{
    public static IApplicationBuilder UseLuceneExplorer(this IApplicationBuilder app, string path = "/_lucene/explorer")
    {
        return app.UseMiddleware<LuceneExplorerMiddleware>(path, null);
    }

    public static IApplicationBuilder UseLuceneExplorer<TContext>(this IApplicationBuilder app, string path = "/_lucene/explorer")
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        return app.UseMiddleware<LuceneExplorerMiddleware>(path, typeof(TContext));
    }
}
