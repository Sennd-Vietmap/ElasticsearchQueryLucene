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
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;

namespace ElasticsearchQueryLucene.EntityFrameworkCore.Storage;

public class LuceneDatabase : ILuceneDatabase
{
    private readonly IDbContextOptions _options;

    public LuceneDatabase(IDbContextOptions options)
    {
        _options = options;
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
                    var term = new Term("Id", keyValue);
                    var updatedDoc = CreateDocument(entry);
                    writer.UpdateDocument(term, updatedDoc);
                    count++;
                    break;

                case EntityState.Deleted:
                    var deleteKeyValue = GetKeyValue(entry);
                    var deleteTerm = new Term("Id", deleteKeyValue);
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

    private Document CreateDocument(IUpdateEntry entry)
    {
        var doc = new Document();
        var entityType = entry.EntityType;

        foreach (var property in entityType.GetProperties())
        {
            var value = entry.GetCurrentValue(property);
            if (value == null) continue;

            var isStored = property[LuceneAnnotationNames.Stored] as bool? ?? true;
            var isTokenized = property[LuceneAnnotationNames.Tokenized] as bool? ?? true;

            var fieldName = property.Name;
            
            if (isTokenized)
            {
                doc.Add(new TextField(fieldName, value.ToString(), isStored ? Field.Store.YES : Field.Store.NO));
            }
            else
            {
                doc.Add(new StringField(fieldName, value.ToString(), isStored ? Field.Store.YES : Field.Store.NO));
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

        var keyValue = entry.GetCurrentValue(keyProperty);
        return keyValue?.ToString() ?? throw new InvalidOperationException("Primary key value cannot be null.");
    }
}
