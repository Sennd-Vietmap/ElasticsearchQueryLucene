using Microsoft.EntityFrameworkCore;
using Lucene.Net.Store;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;
using Xunit;

namespace ElasticsearchQueryLucene.Tests;

public class LuceneProviderTests
{
    [Fact]
    public void UseLucene_ConfiguresExtension()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        using var directory = new RAMDirectory();
        
        optionsBuilder.UseLucene(directory, "TestIndex");

        var extension = optionsBuilder.Options.FindExtension<ElasticsearchQueryLucene.EntityFrameworkCore.Infrastructure.LuceneDbContextOptionsExtension>();
        
        Assert.NotNull(extension);
        Assert.Equal(directory, extension.LuceneDirectory);
        Assert.Equal("TestIndex", extension.IndexName);
    }

    [Fact]
    public void MetadataAnnotation_Stored_SetsAnnotation()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseLucene(new RAMDirectory(), "Test") 
            .Options;

        using var context = new TestDbContext(options);
        var property = context.Model.FindEntityType(typeof(TestEntity))!.FindProperty(nameof(TestEntity.Title));

        Assert.Equal(true, property![ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneAnnotationNames.Stored]);
        Assert.Equal(true, property![ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneAnnotationNames.Tokenized]);
        Assert.Equal("StandardAnalyzer", property![ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneAnnotationNames.Analyzer]);
    }

    [Fact]
    public void AttributeAnnotation_SetsAnnotation()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseLucene(new RAMDirectory(), "AttributeTest")
            .Options;

        using var context = new TestDbContext(options);
        var property = context.Model.FindEntityType(typeof(TestEntity))!.FindProperty(nameof(TestEntity.Author));

        Assert.Equal(true, property![ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneAnnotationNames.Stored]);
        Assert.Equal(false, property![ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneAnnotationNames.Tokenized]);
        Assert.Equal("KeywordAnalyzer", property![ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneAnnotationNames.Analyzer]);
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(b =>
            {
                b.Property(e => e.Title).IsStored().IsTokenized().HasAnalyzer("StandardAnalyzer");
            });
        }
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";

        [ElasticsearchQueryLucene.EntityFrameworkCore.Metadata.LuceneField(Stored = true, Tokenized = false, Analyzer = "KeywordAnalyzer")]
        public string Author { get; set; } = "";
    }
}
