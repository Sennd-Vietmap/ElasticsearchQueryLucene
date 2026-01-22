using System;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

/// <summary>
/// Custom type mapping for Lucene provider.
/// Since Lucene stores everything as text, this provides a simple mapping wrapper.
/// </summary>
public class LuceneTypeMapping : CoreTypeMapping
{
    public LuceneTypeMapping(Type clrType, Type converterType = null)
        : base(new CoreTypeMappingParameters(clrType))
    {
    }

    protected LuceneTypeMapping(CoreTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    public override CoreTypeMapping WithComposedConverter(ValueConverter? converter, ValueComparer? comparer = null, ValueComparer? keyComparer = null, CoreTypeMapping? elementMapping = null, JsonValueReaderWriter? jsonValueReaderWriter = null)
    {
        return new LuceneTypeMapping(Parameters.WithComposedConverter(converter, comparer, keyComparer, elementMapping, jsonValueReaderWriter));
    }

    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
    {
        return new LuceneTypeMapping(parameters);
    }
}
