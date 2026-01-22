using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage.Internal.Mapping;

public class LuceneStringTypeMapping : LuceneTypeMapping
{
    public LuceneStringTypeMapping() : base(typeof(string))
    {
    }

    protected LuceneStringTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new LuceneStringTypeMapping(parameters);
}
