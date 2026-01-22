using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using ElasticsearchQueryLucene.EntityFrameworkCore.Query;
using Xunit;

namespace ElasticsearchQueryLucene.Tests;

public class LuceneExpressionTranslatorTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public int Price { get; set; }
        public bool IsActive { get; set; }
    }

    private readonly LuceneExpressionTranslator _translator = new(new MockTypeMappingSource());

    [Fact]
    public void Translate_SimpleEquality_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category == "Books";

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Category:\"Books\"", result);
    }

    [Fact]
    public void Translate_EqualityWithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name == "Test+Value";

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("\\+", result);
    }

    [Fact]
    public void Translate_AndCondition_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category == "Books" && e.Price > 10;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("AND", result);
        Assert.Contains("Category:\"Books\"", result);
        Assert.Contains("Price:", result);
    }

    [Fact]
    public void Translate_OrCondition_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category == "Books" || e.Category == "Electronics";

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("OR", result);
        Assert.Contains("Category:\"Books\"", result);
        Assert.Contains("Category:\"Electronics\"", result);
    }

    [Fact]
    public void Translate_NotEqual_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Category != "Books";

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("NOT", result);
        Assert.Contains("Category:\"Books\"", result);
    }

    [Fact]
    public void Translate_GreaterThan_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Price > 100;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Price:{100 TO *]", result);
    }

    [Fact]
    public void Translate_GreaterThanOrEqual_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Price >= 100;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Price:[100 TO *]", result);
    }

    [Fact]
    public void Translate_LessThan_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Price < 100;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Price:[* TO 100 }", result);
    }

    [Fact]
    public void Translate_LessThanOrEqual_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Price <= 100;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Price:[* TO 100 ]", result);
    }

    [Fact]
    public void Translate_StringContains_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.Contains("Test");

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Name:*test*", result);
    }

    [Fact]
    public void Translate_StringStartsWith_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.StartsWith("Test");

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Name:Test*", result);
    }

    [Fact]
    public void Translate_StringEndsWith_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.EndsWith("Test");

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Equal("Name:*Test", result);
    }

    [Fact]
    public void Translate_ComplexAndOrCondition_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = 
            e => (e.Category == "Books" || e.Category == "Electronics") && e.Price > 10;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("AND", result);
        Assert.Contains("OR", result);
        Assert.Contains("Category:\"Books\"", result);
        Assert.Contains("Category:\"Electronics\"", result);
        Assert.Contains("Price:", result);
    }

    [Fact]
    public void Translate_MultipleAndConditions_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = 
            e => e.Category == "Books" && e.Price > 10 && e.Price < 100;

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("Category:\"Books\"", result);
        Assert.Contains("Price:{10 TO *]", result);
        Assert.Contains("Price:[* TO 100 }", result);
        var andCount = result.Split("AND").Length - 1;
        Assert.Equal(2, andCount);
    }

    [Fact]
    public void Translate_NestedConditions_ReturnsCorrectQuery()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = 
            e => (e.Category == "Books" && e.Price > 10) || (e.Category == "Electronics" && e.Price < 100);

        // Act
        var result = _translator.Translate(predicate.Body);

        // Assert
        Assert.Contains("OR", result);
        Assert.Contains("AND", result);
        Assert.Contains("Category:\"Books\"", result);
        Assert.Contains("Category:\"Electronics\"", result);
    }
}
