using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage.Internal.Mapping;

public class LuceneIntTypeMapping : LuceneTypeMapping
{
    public LuceneIntTypeMapping() : base(typeof(int))
    {
    }

    protected LuceneIntTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new LuceneIntTypeMapping(parameters);
}
