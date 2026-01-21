using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.Conventions;

public class LuceneConventionSetPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.PropertyAddedConventions.Add(new LuceneAttributeConvention());
        return conventionSet;
    }
}
