using ElasticsearchQueryLucene.Core.Interfaces;

namespace ElasticsearchQueryLucene.Core.Models;

public abstract class QueryNode : IQueryNode
{
    public abstract void Accept(IQueryVisitor visitor);
}

public class TermQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class TermsQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class MatchQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class MatchPhraseQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class PrefixQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class WildcardQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class FuzzyQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? Fuzziness { get; set; }

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class RegexpQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class ExistsQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class IdsQueryNode : QueryNode
{
    public List<string> Values { get; set; } = new();

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class RangeQueryNode : QueryNode
{
    public string Field { get; set; } = string.Empty;
    public string? Gte { get; set; }
    public string? Gt { get; set; }
    public string? Lte { get; set; }
    public string? Lt { get; set; }

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}

public class BoolQueryNode : QueryNode
{
    public List<QueryNode> Must { get; set; } = new();
    public List<QueryNode> Should { get; set; } = new();
    public List<QueryNode> MustNot { get; set; } = new();
    public List<QueryNode> Filter { get; set; } = new();
    public int? MinimumShouldMatch { get; set; }

    public override void Accept(IQueryVisitor visitor) => visitor.Visit(this);
}
