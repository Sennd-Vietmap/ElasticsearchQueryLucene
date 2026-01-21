using Microsoft.EntityFrameworkCore.Storage;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneTypeMappingSource : TypeMappingSource
{
    public LuceneTypeMappingSource(TypeMappingSourceDependencies dependencies) : base(dependencies)
    {
    }

    protected override CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo)
    {
        return null; // Let base handle common types or return basic mappings
    }
}
