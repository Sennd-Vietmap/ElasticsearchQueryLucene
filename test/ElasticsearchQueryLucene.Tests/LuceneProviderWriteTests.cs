using Microsoft.EntityFrameworkCore;
using Lucene.Net.Store;
using Lucene.Net.Index;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;
using Xunit;
using System.Linq;

namespace ElasticsearchQueryLucene.Tests;

public class LuceneProviderWriteTests
{
    [Fact]
    public void SaveChanges_AddsDocumentToLucene()
    {
        using var directory = new RAMDirectory();
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseLucene(directory)
            .Options;

        using (var context = new WriteDbContext(options))
        {
            var book = new Book { Id = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald" };
            context.Books.Add(book);
            context.SaveChanges();
        }

        // Verify using Lucene directly
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(1, reader.NumDocs);
        
        var doc = reader.Document(0);
        Assert.Equal("The Great Gatsby", doc.Get("Title"));
        Assert.Equal("F. Scott Fitzgerald", doc.Get("Author"));
    }

    private class WriteDbContext : DbContext
    {
        public WriteDbContext(DbContextOptions options) : base(options) { }
        public DbSet<Book> Books => Set<Book>();
    }

    private class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
    }
}
