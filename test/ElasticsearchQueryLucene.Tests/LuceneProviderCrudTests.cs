using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Xunit;

namespace ElasticsearchQueryLucene.Tests;

/// <summary>
/// Comprehensive CRUD tests for the Lucene provider.
/// Verifies the full Create, Read, Update, Delete cycle.
/// </summary>
public class LuceneProviderCrudTests
{
    [Fact]
    public void FullCrudCycle_WorksCorrectly()
    {
        // Arrange
        using var directory = new RAMDirectory();
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

        // 1. CREATE
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        {
            var doc = new Document();
            doc.Add(new StringField("Id", "1", Field.Store.YES));
            doc.Add(new TextField("Title", "Draft Book", Field.Store.YES));
            doc.Add(new TextField("Status", "Draft", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Verify Create
        using (var reader = DirectoryReader.Open(directory))
        {
            Assert.Equal(1, reader.NumDocs);
            var doc = reader.Document(0);
            Assert.Equal("Draft Book", doc.Get("Title"));
        }

        // 2. READ (Implicitly done above, but verifying more details)
        using (var reader = DirectoryReader.Open(directory))
        {
            var doc = reader.Document(0);
            Assert.Equal("1", doc.Get("Id"));
            Assert.Equal("Draft", doc.Get("Status"));
        }

        // 3. UPDATE
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        {
            var updatedDoc = new Document();
            updatedDoc.Add(new StringField("Id", "1", Field.Store.YES));
            updatedDoc.Add(new TextField("Title", "Published Book", Field.Store.YES));
            updatedDoc.Add(new TextField("Status", "Published", Field.Store.YES));
            
            // Update by Term (Id)
            writer.UpdateDocument(new Term("Id", "1"), updatedDoc);
            writer.Commit();
        }

        // Verify Update
        using (var reader = DirectoryReader.Open(directory))
        {
            Assert.Equal(1, reader.NumDocs); // Count should explicitly be 1, not 2
            var doc = reader.Document(0);
            Assert.Equal("Published Book", doc.Get("Title"));
            Assert.Equal("Published", doc.Get("Status"));
        }

        // 4. DELETE
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        {
            writer.DeleteDocuments(new Term("Id", "1"));
            writer.Commit();
        }

        // Verify Delete
        using (var reader = DirectoryReader.Open(directory))
        {
            Assert.Equal(0, reader.NumDocs);
        }
    }

    [Fact]
    public void BulkCreate_And_Read_WorksCorrectly()
    {
         // Arrange
        using var directory = new RAMDirectory();
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        // Bulk Create
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        {
            for (int i = 0; i < 100; i++)
            {
                var doc = new Document();
                doc.Add(new StringField("Id", i.ToString(), Field.Store.YES));
                doc.Add(new TextField("Content", $"Item {i}", Field.Store.YES));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        // Verify
        using (var reader = DirectoryReader.Open(directory))
        {
            Assert.Equal(100, reader.NumDocs);
            var doc = reader.Document(50);
            Assert.Equal("50", doc.Get("Id"));
            Assert.Equal("Item 50", doc.Get("Content"));
        }
    }
}
