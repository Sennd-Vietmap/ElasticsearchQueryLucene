using System;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage.Internal.Mapping;

public class LuceneDateTimeTypeMapping : LuceneTypeMapping
{
    // Convert DateTime to Ticks (long) for sorting and range queries
    private static readonly ValueConverter<DateTime, long> _dateTimeToTicks 
        = new ValueConverter<DateTime, long>(v => v.Ticks, v => new DateTime(v));

    public LuceneDateTimeTypeMapping() : base(new CoreTypeMappingParameters(
        typeof(DateTime), 
        converter: _dateTimeToTicks))
    {
    }

    protected LuceneDateTimeTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new LuceneDateTimeTypeMapping(parameters);
}
