using System;
using System.Linq;
using System.Linq.Expressions;
using ElasticsearchQueryLucene.EntityFrameworkCore.Query;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using ElasticsearchQueryLucene.EntityFrameworkCore.Extensions;

namespace ElasticsearchQueryLucene.Tests;

/// <summary>
/// Tests for LINQ query translation to Lucene query expressions.
/// Since we can't fully execute these queries without a complete context,
/// we verify the translation logic directly.
/// </summary>
public class LuceneProviderLinqTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public int Price { get; set; }
    }

    private readonly LuceneExpressionTranslator _translator = new(new MockTypeMappingSource());

    [Fact]
    public void Where_SimpleEquality_TranslatesToLuceneQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category == "Books";

        // Act
        var luceneQuery = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Category:\"Books\"", luceneQuery);
    }

    [Fact]
    public void Where_MultipleConditions_TranslatesToLuceneQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category == "Books" && e.Price > 10;

        // Act
        var luceneQuery = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("Category:\"Books\"", luceneQuery);
        Assert.Contains("Price:{10 TO *]", luceneQuery);
        Assert.Contains("AND", luceneQuery);
    }

    [Fact]
    public void Where_OrCondition_TranslatesToLuceneQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category == "Books" || e.Category == "Electronics";

        // Act
        var luceneQuery = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("Category:\"Books\"", luceneQuery);
        Assert.Contains("Category:\"Electronics\"", luceneQuery);
        Assert.Contains("OR", luceneQuery);
    }

    [Fact]
    public void Where_StringContains_TranslatesToWildcardQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.Contains("Test");

        // Act
        var luceneQuery = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Name:*test*", luceneQuery);
    }

    [Fact]
    public void Where_LuceneMatch_TranslatesToRawQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => EF.Functions.LuceneMatch(e.Name, "foo* AND bar");

        // Act
        var luceneQuery = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Name:(foo* AND bar)", luceneQuery);
    }

    [Fact]
    public void ComplexQuery_WithLuceneMatch_TranslatesCorrectly()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = 
            e => e.Category == "Books" && EF.Functions.LuceneMatch(e.Name, "term");

        // Act
        var luceneQuery = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("Category:\"Books\"", luceneQuery);
        Assert.Contains("Name:(term)", luceneQuery);
        Assert.Contains("AND", luceneQuery);
    }
}
