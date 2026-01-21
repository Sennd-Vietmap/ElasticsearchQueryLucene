using System.Text.Json;
using ElasticsearchQueryLucene.Core.Models;

namespace ElasticsearchQueryLucene.Core.Converters;

public class QueryParser
{
    public QueryNode Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("query", out var queryElement))
        {
            return ParseNode(queryElement);
        }

        return ParseNode(root);
    }

    private QueryNode ParseNode(JsonElement element)
    {
        if (element.TryGetProperty("bool", out var boolElement))
        {
            return ParseBool(boolElement);
        }
        if (element.TryGetProperty("term", out var termElement))
        {
            return ParseTerm(termElement);
        }
        if (element.TryGetProperty("terms", out var termsElement))
        {
            return ParseTerms(termsElement);
        }
        if (element.TryGetProperty("match", out var matchElement))
        {
            return ParseMatch(matchElement);
        }
        if (element.TryGetProperty("match_phrase", out var matchPhraseElement))
        {
            return ParseMatchPhrase(matchPhraseElement);
        }
        if (element.TryGetProperty("prefix", out var prefixElement))
        {
            return ParsePrefix(prefixElement);
        }
        if (element.TryGetProperty("wildcard", out var wildcardElement))
        {
            return ParseWildcard(wildcardElement);
        }
        if (element.TryGetProperty("fuzzy", out var fuzzyElement))
        {
            return ParseFuzzy(fuzzyElement);
        }
        if (element.TryGetProperty("regexp", out var regexpElement))
        {
            return ParseRegexp(regexpElement);
        }
        if (element.TryGetProperty("exists", out var existsElement))
        {
            return ParseExists(existsElement);
        }
        if (element.TryGetProperty("ids", out var idsElement))
        {
            return ParseIds(idsElement);
        }
        if (element.TryGetProperty("range", out var rangeElement))
        {
            return ParseRange(rangeElement);
        }

        // Default or error handling
        throw new NotSupportedException($"Query type not supported or invalid structure: {element.GetRawText()}");
    }

    private BoolQueryNode ParseBool(JsonElement element)
    {
        var node = new BoolQueryNode();
        if (element.TryGetProperty("must", out var must))
        {
            foreach (var item in must.EnumerateArray()) node.Must.Add(ParseNode(item));
        }
        if (element.TryGetProperty("should", out var should))
        {
            foreach (var item in should.EnumerateArray()) node.Should.Add(ParseNode(item));
        }
        if (element.TryGetProperty("must_not", out var mustNot))
        {
            foreach (var item in mustNot.EnumerateArray()) node.MustNot.Add(ParseNode(item));
        }
        if (element.TryGetProperty("filter", out var filter))
        {
            foreach (var item in filter.EnumerateArray()) node.Filter.Add(ParseNode(item));
        }
        if (element.TryGetProperty("minimum_should_match", out var msm))
        {
            node.MinimumShouldMatch = msm.GetInt32();
        }
        return node;
    }

    private TermQueryNode ParseTerm(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new TermQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private TermsQueryNode ParseTerms(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        var values = property.Value.EnumerateArray().Select(v => v.GetString() ?? string.Empty).ToList();
        return new TermsQueryNode { Field = property.Name, Values = values };
    }

    private MatchQueryNode ParseMatch(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new MatchQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private MatchPhraseQueryNode ParseMatchPhrase(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new MatchPhraseQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private PrefixQueryNode ParsePrefix(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new PrefixQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private WildcardQueryNode ParseWildcard(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new WildcardQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private FuzzyQueryNode ParseFuzzy(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new FuzzyQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private RegexpQueryNode ParseRegexp(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        return new RegexpQueryNode { Field = property.Name, Value = property.Value.GetString() ?? string.Empty };
    }

    private ExistsQueryNode ParseExists(JsonElement element)
    {
        var field = element.GetProperty("field").GetString() ?? string.Empty;
        return new ExistsQueryNode { Field = field };
    }

    private IdsQueryNode ParseIds(JsonElement element)
    {
        var values = element.GetProperty("values").EnumerateArray().Select(v => v.GetString() ?? string.Empty).ToList();
        return new IdsQueryNode { Values = values };
    }

    private RangeQueryNode ParseRange(JsonElement element)
    {
        var property = element.EnumerateObject().First();
        var field = property.Name;
        var rangeProps = property.Value;
        
        var node = new RangeQueryNode { Field = field };
        if (rangeProps.TryGetProperty("gte", out var gte)) node.Gte = gte.GetRawText().Trim('"');
        if (rangeProps.TryGetProperty("gt", out var gt)) node.Gt = gt.GetRawText().Trim('"');
        if (rangeProps.TryGetProperty("lte", out var lte)) node.Lte = lte.GetRawText().Trim('"');
        if (rangeProps.TryGetProperty("lt", out var lt)) node.Lt = lt.GetRawText().Trim('"');
        
        return node;
    }
}
