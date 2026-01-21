using System;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;

[AttributeUsage(AttributeTargets.Property)]
public class LuceneFieldAttribute : Attribute
{
    public bool Stored { get; set; } = true;
    public bool Tokenized { get; set; } = true;
    public string? Analyzer { get; set; }
}
