using System;
using Microsoft.EntityFrameworkCore;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;

public static class LuceneDbFunctionsExtensions
{
    /// <summary>
    /// Performs a direct Lucene query match on the specified property.
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="property">The property to query.</param>
    /// <param name="query">The raw Lucene query string.</param>
    /// <returns>true if the property matches the query.</returns>
    public static bool LuceneMatch(this DbFunctions _, object property, string query)
        => throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
}
