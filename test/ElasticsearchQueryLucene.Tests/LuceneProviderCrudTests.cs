using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;
using ElasticsearchQueryLucene.EntityFrameworkCore.Metadata;

namespace ElasticsearchQueryLucene.Tests;

public class LuceneProviderCrudTests
{
    private class Book
    {
        public int Id { get; set; }
        
        [LuceneField(Stored = true, Tokenized = true)]
        public string Title { get; set; } = "";
        
        [LuceneField(Stored = true, Tokenized = false)]
        public string Author { get; set; } = "";
        
        [LuceneField(Stored = true, Tokenized = false)]
        public int Year { get; set; }
    }

    private class BookContext : DbContext
    {
        private readonly Lucene.Net.Store.Directory _directory;

        public BookContext(Lucene.Net.Store.Directory directory)
        {
            _directory = directory;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLucene(_directory, "books");
        }

        public DbSet<Book> Books => Set<Book>();
    }

    [Fact]
    public void Create_AddsDocumentToLucene()
    {
        // Arrange
        using var directory = new RAMDirectory();
        
        // Act
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Year = 1925 };
            context.Books.Add(book);
            context.SaveChanges();
        }

        // Assert
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(1, reader.NumDocs);
        
        var doc = reader.Document(0);
        Assert.Equal("The Great Gatsby", doc.Get("Title"));
        Assert.Equal("F. Scott Fitzgerald", doc.Get("Author"));
        Assert.Equal("1925", doc.Get("Year"));
    }

    [Fact]
    public void Update_ModifiesExistingDocument()
    {
        // Arrange
        using var directory = new RAMDirectory();
        
        // Create initial document
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 1, Title = "Original Title", Author = "Original Author", Year = 2000 };
            context.Books.Add(book);
            context.SaveChanges();
        }

        // Act - Update the document
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 1, Title = "Updated Title", Author = "Updated Author", Year = 2024 };
            context.Books.Update(book);
            context.SaveChanges();
        }

        // Assert
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(1, reader.NumDocs); // Should still be 1 document
        
        var doc = reader.Document(0);
        Assert.Equal("Updated Title", doc.Get("Title"));
        Assert.Equal("Updated Author", doc.Get("Author"));
        Assert.Equal("2024", doc.Get("Year"));
    }

    [Fact]
    public void Delete_RemovesDocumentFromLucene()
    {
        // Arrange
        using var directory = new RAMDirectory();
        
        // Create initial document
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 1, Title = "To Be Deleted", Author = "Test Author", Year = 2020 };
            context.Books.Add(book);
            context.SaveChanges();
        }

        // Act - Delete the document
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 1, Title = "To Be Deleted", Author = "Test Author", Year = 2020 };
            context.Books.Remove(book);
            context.SaveChanges();
        }

        // Assert
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(0, reader.NumDocs);
    }

    [Fact]
    public void FullCrudCycle_WorksCorrectly()
    {
        // Arrange
        using var directory = new RAMDirectory();

        // Create
        using (var context = new BookContext(directory))
        {
            context.Books.Add(new Book { Id = 1, Title = "Book 1", Author = "Author 1", Year = 2020 });
            context.Books.Add(new Book { Id = 2, Title = "Book 2", Author = "Author 2", Year = 2021 });
            context.Books.Add(new Book { Id = 3, Title = "Book 3", Author = "Author 3", Year = 2022 });
            context.SaveChanges();
        }

        // Verify Create
        using (var reader = DirectoryReader.Open(directory))
        {
            Assert.Equal(3, reader.NumDocs);
        }

        // Update
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 2, Title = "Updated Book 2", Author = "Updated Author 2", Year = 2023 };
            context.Books.Update(book);
            context.SaveChanges();
        }

        // Delete
        using (var context = new BookContext(directory))
        {
            var book = new Book { Id = 3, Title = "Book 3", Author = "Author 3", Year = 2022 };
            context.Books.Remove(book);
            context.SaveChanges();
        }

        // Final verification
        using (var reader = DirectoryReader.Open(directory))
        {
            Assert.Equal(2, reader.NumDocs);
            
            // Verify the updated document exists
            bool foundUpdated = false;
            for (int i = 0; i < reader.NumDocs; i++)
            {
                var doc = reader.Document(i);
                if (doc.Get("Id") == "2")
                {
                    Assert.Equal("Updated Book 2", doc.Get("Title"));
                    foundUpdated = true;
                }
            }
            Assert.True(foundUpdated, "Updated document not found");
        }
    }

    [Fact]
    public void BulkOperations_WorkCorrectly()
    {
        // Arrange
        using var directory = new RAMDirectory();

        // Act - Bulk create
        using (var context = new BookContext(directory))
        {
            for (int i = 1; i <= 10; i++)
            {
                context.Books.Add(new Book { Id = i, Title = $"Book {i}", Author = $"Author {i}", Year = 2020 + i });
            }
            var count = context.SaveChanges();
            Assert.Equal(10, count);
        }

        // Assert
        using var reader = DirectoryReader.Open(directory);
        Assert.Equal(10, reader.NumDocs);
    }
}
