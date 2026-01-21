using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;

public static class LucenePropertyBuilderExtensions
{
    public static PropertyBuilder IsStored(this PropertyBuilder propertyBuilder, bool stored = true)
    {
        propertyBuilder.HasAnnotation(LuceneAnnotationNames.Stored, stored);
        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> IsStored<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, bool stored = true)
        => (PropertyBuilder<TProperty>)IsStored((PropertyBuilder)propertyBuilder, stored);

    public static PropertyBuilder IsTokenized(this PropertyBuilder propertyBuilder, bool tokenized = true)
    {
        propertyBuilder.HasAnnotation(LuceneAnnotationNames.Tokenized, tokenized);
        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> IsTokenized<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, bool tokenized = true)
        => (PropertyBuilder<TProperty>)IsTokenized((PropertyBuilder)propertyBuilder, tokenized);

    public static PropertyBuilder HasAnalyzer(this PropertyBuilder propertyBuilder, string analyzerName)
    {
        propertyBuilder.HasAnnotation(LuceneAnnotationNames.Analyzer, analyzerName);
        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> HasAnalyzer<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string analyzerName)
        => (PropertyBuilder<TProperty>)HasAnalyzer((PropertyBuilder)propertyBuilder, analyzerName);
}
