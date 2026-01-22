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

    /// <summary>
    /// Applies a boost factor to the specified property in the Lucene query.
    /// </summary>
    /// <param name="_">The DbFunctions instance.</param>
    /// <param name="property">The property to boost.</param>
    /// <param name="boost">The boost factor (e.g., 2.0f).</param>
    /// <returns>The property value (used for chaining in LINQ).</returns>
    public static T Boost<T>(this DbFunctions _, T property, float boost)
        => throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
}
