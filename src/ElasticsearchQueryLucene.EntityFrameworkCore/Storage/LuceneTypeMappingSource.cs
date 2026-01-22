using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneTypeMappingSource : TypeMappingSource
{
    public LuceneTypeMappingSource(TypeMappingSourceDependencies dependencies) : base(dependencies)
    {
    }

    protected override CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;

        if (clrType == null)
            return null;

        // Support common primitive types that Lucene can store as strings
        if (clrType == typeof(int) || clrType == typeof(int?))
            return new LuceneTypeMapping(clrType, typeof(int));

        if (clrType == typeof(long) || clrType == typeof(long?))
            return new LuceneTypeMapping(clrType, typeof(long));

        if (clrType == typeof(bool) || clrType == typeof(bool?))
            return new LuceneTypeMapping(clrType, typeof(bool));

        if (clrType == typeof(string))
            return new LuceneTypeMapping(clrType, typeof(string));

        if (clrType == typeof(double) || clrType == typeof(double?))
            return new LuceneTypeMapping(clrType, typeof(double));

        if (clrType == typeof(float) || clrType == typeof(float?))
            return new LuceneTypeMapping(clrType, typeof(float));

        if (clrType == typeof(decimal) || clrType == typeof(decimal?))
            return new LuceneTypeMapping(clrType, typeof(decimal));

        if (clrType == typeof(DateTime) || clrType == typeof(DateTime?))
            return new LuceneTypeMapping(clrType, typeof(DateTime));

        return null;
    }
}
