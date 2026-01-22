using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage.Internal.Mapping;

public class LuceneBoolTypeMapping : LuceneTypeMapping
{
    public LuceneBoolTypeMapping() : base(typeof(bool))
    {
    }

    protected LuceneBoolTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new LuceneBoolTypeMapping(parameters);
}
