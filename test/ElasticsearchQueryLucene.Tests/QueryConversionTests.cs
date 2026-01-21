using ElasticsearchQueryLucene.Core.Converters;
using Xunit;

namespace ElasticsearchQueryLucene.Tests;

public class QueryConversionTests
{
    private readonly QueryParser _parser = new();

    private string Convert(string json)
    {
        var queryNode = _parser.Parse(json);
        var visitor = new LuceneQueryVisitor();
        queryNode.Accept(visitor);
        var result = visitor.GetResult();
        System.Console.WriteLine($"RESULT: {result}");
        return result;
    }

    [Fact]
    public void TermQuery_ShouldConvert()
    {
        var json = "{\"term\": {\"user.id\": \"kimchy\"}}";
        var result = Convert(json);
        Assert.Equal("user.id:kimchy", result);
    }

    [Fact]
    public void TermsQuery_ShouldConvert()
    {
        var json = "{\"terms\": {\"tag\": [\"search\", \"open\"]}}";
        var result = Convert(json);
        Assert.Equal("tag:(search OR open)", result);
    }

    [Fact]
    public void MatchQuery_ShouldConvert()
    {
        var json = "{\"match\": {\"msg\": \"hello world\"}}";
        var result = Convert(json);
        Assert.Equal("msg:(hello world)", result);
    }

    [Fact]
    public void MatchPhraseQuery_ShouldConvert()
    {
        var json = "{\"match_phrase\": {\"msg\": \"hello world\"}}";
        var result = Convert(json);
        Assert.Equal("msg:\"hello world\"", result);
    }

    [Fact]
    public void RangeQuery_Inclusive_ShouldConvert()
    {
        var json = "{\"range\": {\"age\": {\"gte\": 10, \"lte\": 20}}}";
        var result = Convert(json);
        Assert.Equal("age:[10 TO 20]", result);
    }

    [Fact]
    public void RangeQuery_Exclusive_ShouldConvert()
    {
        var json = "{\"range\": {\"age\": {\"gt\": 10, \"lt\": 20}}}";
        var result = Convert(json);
        Assert.Equal("age:{10 TO 20}", result);
    }

    [Fact]
    public void BoolQuery_Complex_ShouldConvert()
    {
        var json = @"
        {
          ""bool"": {
            ""must"": [{ ""term"": { ""brand"": ""apple"" } }],
            ""should"": [
              { ""match"": { ""color"": ""red"" } },
              { ""match"": { ""color"": ""blue"" } }
            ],
            ""filter"": [{ ""range"": { ""price"": { ""lte"": 500 } } }]
          }
        }";
        var result = Convert(json);
        // +brand:apple +price:[* TO 500] +(color:(red) OR color:(blue))
        Assert.Contains("+brand:apple", result);
        Assert.Contains("+price:[* TO 500]", result);
        Assert.Contains("+(color:(red) OR color:(blue))", result);
    }

    [Fact]
    public void AdvancedExample_ShouldConvert()
    {
        var json = @"
        {
          ""bool"": {
            ""must"": [
              { ""term"": { ""category"": ""smartphone"" } }
            ],
            ""should"": [
              { ""term"": { ""brand"": ""apple"" } },
              { ""term"": { ""brand"": ""samsung"" } }
            ],
            ""must_not"": [
              { ""term"": { ""condition"": ""used"" } }
            ]
          }
        }";
        var result = Convert(json);
        // +category:smartphone -condition:used +(brand:apple OR brand:samsung)
        Assert.Contains("+category:smartphone", result);
        Assert.Contains("-condition:used", result);
        Assert.Contains("+(brand:apple OR brand:samsung)", result);
    }

    [Fact]
    public void SpecialCharacters_ShouldBeEscaped()
    {
        var json = "{\"term\": {\"name\": \"john+doe\"}}";
        var result = Convert(json);
        Assert.Equal("name:john\\+doe", result);
    }
}
