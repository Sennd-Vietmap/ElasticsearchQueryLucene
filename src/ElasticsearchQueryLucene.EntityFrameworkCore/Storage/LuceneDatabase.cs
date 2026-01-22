using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticsearchQueryLucene.EntityFrameworkCore.Infrastructure;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneDatabase : ILuceneDatabase
{
    private readonly ITypeMappingSource _typeMappingSource;
    IDbContextOptions _options;
    public LuceneDatabase(IDbContextOptions options, ITypeMappingSource typeMappingSource)
    {
        _options = options;
        _typeMappingSource = typeMappingSource;
    }

    public int SaveChanges(IList<IUpdateEntry> entries)
    {
        var extension = _options.FindExtension<LuceneDbContextOptionsExtension>();
        if (extension?.LuceneDirectory == null) return 0;

        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
        using var writer = new IndexWriter(extension.LuceneDirectory, config);

        var count = 0;
        foreach (var entry in entries)
        {
            switch (entry.EntityState)
            {
                case EntityState.Added:
                    var doc = CreateDocument(entry);
                    writer.AddDocument(doc);
                    count++;
                    break;

                case EntityState.Modified:
                    var keyValue = GetKeyValue(entry);
                    var term = new Term("__pk", keyValue);
                    var updatedDoc = CreateDocument(entry);
                    writer.UpdateDocument(term, updatedDoc);
                    count++;
                    break;

                case EntityState.Deleted:
                    var deleteKeyValue = GetKeyValue(entry);
                    var deleteTerm = new Term("__pk", deleteKeyValue);
                    writer.DeleteDocuments(deleteTerm);
                    count++;
                    break;
            }
        }

        writer.Commit();
        return count;
    }

    public Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken)
    {
        return Task.FromResult(SaveChanges(entries));
    }

    public IEnumerable<IReadOnlyDictionary<string, string[]>> GetIndexDocuments(int skip = 0, int take = 100, string? query = null)
    {
        var extension = _options.FindExtension<LuceneDbContextOptionsExtension>();
        if (extension?.LuceneDirectory == null) yield break;

        if (!DirectoryReader.IndexExists(extension.LuceneDirectory)) yield break;

        using var reader = DirectoryReader.Open(extension.LuceneDirectory);
        var searcher = new IndexSearcher(reader);
        
        Lucene.Net.Search.Query luceneQuery;
        if (string.IsNullOrWhiteSpace(query))
        {
            luceneQuery = new MatchAllDocsQuery();
        }
        else
        {
            try
            {
                var parser = new Lucene.Net.QueryParsers.Classic.QueryParser(LuceneVersion.LUCENE_48, "__pk", new StandardAnalyzer(LuceneVersion.LUCENE_48));
                parser.AllowLeadingWildcard = true;
                luceneQuery = parser.Parse(query);
            }
            catch
            {
                // Fallback if query is invalid
                luceneQuery = new MatchAllDocsQuery();
            }
        }

        var topDocs = searcher.Search(luceneQuery, (skip + take));
        var end = Math.Min(skip + take, topDocs.ScoreDocs.Length);

        for (int i = skip; i < end; i++)
        {
            var doc = searcher.Doc(topDocs.ScoreDocs[i].Doc);
            var dict = new Dictionary<string, string[]>();
            foreach (var field in doc.Fields)
            {
                var val = field.GetStringValue() ?? field.GetNumericValue()?.ToString() ?? "(binary)";
                if (dict.TryGetValue(field.Name, out var values))
                {
                    var newValues = new string[values.Length + 1];
                    Array.Copy(values, newValues, values.Length);
                    newValues[values.Length] = val;
                    dict[field.Name] = newValues;
                }
                else
                {
                    dict[field.Name] = new[] { val };
                }
            }
            yield return dict;
        }
    }

    private Document CreateDocument(IUpdateEntry entry)
    {
        var doc = new Document();
        var entityType = entry.EntityType;
        var pkProperty = entityType.FindPrimaryKey()?.Properties.FirstOrDefault();

        // Add System PK field
        if (pkProperty != null)
        {
            var pkValue = entry.GetCurrentValue(pkProperty)?.ToString();
            if (pkValue != null)
            {
                // Store YES to verify if needed, Index NOT_ANALYZED (StringField behavior)
                doc.Add(new StringField("__pk", pkValue, Field.Store.YES)); 
            }
        }

        foreach (var property in entityType.GetProperties())
        {
            var value = entry.GetCurrentValue(property);
            if (value == null) continue;

            // Resolve Mapping and Converter
            var mapping = _typeMappingSource.FindMapping(property);
            if (mapping?.Converter != null)
            {
                value = mapping.Converter.ConvertToProvider(value);
            }

            var isStored = property[LuceneAnnotationNames.Stored] as bool? ?? true;
            var isTokenized = property[LuceneAnnotationNames.Tokenized] as bool? ?? true;
            var fieldName = property.Name;
            var storeMode = isStored ? Field.Store.YES : Field.Store.NO;

            if (value is int i)
            {
                doc.Add(new Int32Field(fieldName, i, storeMode));
            }
            else if (value is long l)
            {
                doc.Add(new Int64Field(fieldName, l, storeMode));
            }
            else if (value is float f)
            {
                doc.Add(new SingleField(fieldName, f, storeMode));
            }
            else if (value is double d)
            {
                doc.Add(new DoubleField(fieldName, d, storeMode));
            }
            else if (value is DateTime dt)
            {
                doc.Add(new StringField(fieldName, dt.ToString("O"), storeMode));
            }
            else if (value is Guid guid)
            {
                doc.Add(new StringField(fieldName, guid.ToString(), storeMode));
            }
            else
            {
                var strValue = value.ToString();
                if (isTokenized)
                {
                    doc.Add(new TextField(fieldName, strValue, storeMode));
                }
                else
                {
                    doc.Add(new StringField(fieldName, strValue, storeMode));
                }
            }
        }

        return doc;
    }

    private string GetKeyValue(IUpdateEntry entry)
    {
        var keyProperty = entry.EntityType.FindPrimaryKey()?.Properties.FirstOrDefault();
        if (keyProperty == null)
        {
            throw new InvalidOperationException($"Entity type {entry.EntityType.Name} does not have a primary key defined.");
        }

        var value = entry.GetCurrentValue(keyProperty);
        var mapping = _typeMappingSource.FindMapping(keyProperty);
        if (mapping?.Converter != null)
        {
            value = mapping.Converter.ConvertToProvider(value);
        }

        return value?.ToString() ?? throw new InvalidOperationException("Primary key value cannot be null.");
    }
}
