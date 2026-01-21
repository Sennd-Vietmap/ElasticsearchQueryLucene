using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Xunit;

namespace ElasticsearchQueryLucene.Tests;

/// <summary>
/// Tests for basic Lucene write operations.
/// These tests verify the underlying Lucene functionality without EF Core.
/// </summary>
public class LuceneProviderWriteTests
{
    [Fact]
    public void SaveChanges_AddsDocumentToLucene()
    {
        // Arrange
        using var directory = new RAMDirectory();
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
        
        // Act - Write document
        using (var writer = new IndexWriter(directory, config))
        {
            var doc = new Document();
            doc.Add(new StringField("Id", "1", Field.Store.YES));
            doc.Add(new TextField("Title", "The Great Gatsby", Field.Store.YES));
            doc.Add(new TextField("Author", "F. Scott Fitzgerald", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Assert - Verify using Lucene directly
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(1, reader.NumDocs);
        
        var retrievedDoc = reader.Document(0);
        Assert.Equal("1", retrievedDoc.Get("Id"));
        Assert.Equal("The Great Gatsby", retrievedDoc.Get("Title"));
        Assert.Equal("F. Scott Fitzgerald", retrievedDoc.Get("Author"));
    }

    [Fact]
    public void UpdateDocument_ModifiesExistingDocument()
    {
        // Arrange
        using var directory = new RAMDirectory();
        
        // Create initial document
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48))))
        {
            var doc = new Document();
            doc.Add(new StringField("Id", "1", Field.Store.YES));
            doc.Add(new TextField("Title", "Original Title", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Act - Update document
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48))))
        {
            var updatedDoc = new Document();
            updatedDoc.Add(new StringField("Id", "1", Field.Store.YES));
            updatedDoc.Add(new TextField("Title", "Updated Title", Field.Store.YES));
            writer.UpdateDocument(new Term("Id", "1"), updatedDoc);
            writer.Commit();
        }

        // Assert
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(1, reader.NumDocs); // Should still be 1 document
        
        var retrievedDoc = reader.Document(0);
        Assert.Equal("Updated Title", retrievedDoc.Get("Title"));
    }

    [Fact]
    public void DeleteDocument_RemovesFromIndex()
    {
        // Arrange
        using var directory = new RAMDirectory();
        
        // Create initial document
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48))))
        {
            var doc = new Document();
            doc.Add(new StringField("Id", "1", Field.Store.YES));
            doc.Add(new TextField("Title", "To Be Deleted", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Act - Delete document
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48))))
        {
            writer.DeleteDocuments(new Term("Id", "1"));
            writer.Commit();
        }

        // Assert
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(0, reader.NumDocs);
    }
}
