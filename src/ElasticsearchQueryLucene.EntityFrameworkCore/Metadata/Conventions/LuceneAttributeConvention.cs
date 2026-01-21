using System.Reflection;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.Conventions;

public class LuceneAttributeConvention : IPropertyAddedConvention
{
    public void ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
    {
        var property = propertyBuilder.Metadata;
        var clrType = (property.DeclaringType as Microsoft.EntityFrameworkCore.Metadata.IConventionEntityType)?.ClrType;
        
        if (clrType != null)
        {
            var memberInfo = clrType.GetProperty(property.Name);
            if (memberInfo != null)
            {
                var attribute = memberInfo.GetCustomAttribute<LuceneFieldAttribute>();
                if (attribute != null)
                {
                    propertyBuilder.HasAnnotation(LuceneAnnotationNames.Stored, attribute.Stored);
                    propertyBuilder.HasAnnotation(LuceneAnnotationNames.Tokenized, attribute.Tokenized);
                    if (attribute.Analyzer != null)
                    {
                        propertyBuilder.HasAnnotation(LuceneAnnotationNames.Analyzer, attribute.Analyzer);
                    }
                }
            }
        }
    }
}
