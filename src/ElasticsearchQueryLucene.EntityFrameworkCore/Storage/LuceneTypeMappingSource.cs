using System;
using Microsoft.EntityFrameworkCore.Storage;
using ElasticsearchQueryLucene.EntityFrameworkCore.Storage.Internal.Mapping;

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
            return new LuceneIntTypeMapping();

        // if (clrType == typeof(long) || clrType == typeof(long?))
        //     return new LuceneLongTypeMapping();

        if (clrType == typeof(bool) || clrType == typeof(bool?))
            return new LuceneBoolTypeMapping();

        if (clrType == typeof(string))
            return new LuceneStringTypeMapping();

        // if (clrType == typeof(double) || clrType == typeof(double?))
        //     return new LuceneDoubleTypeMapping();

        // if (clrType == typeof(float) || clrType == typeof(float?))
        //     return new LuceneFloatTypeMapping();

        // if (clrType == typeof(decimal) || clrType == typeof(decimal?))
        //     return new LuceneDecimalTypeMapping();

        if (clrType == typeof(DateTime) || clrType == typeof(DateTime?))
            return new LuceneDateTimeTypeMapping();

        return null; // Fallback or throw?
    }
}
