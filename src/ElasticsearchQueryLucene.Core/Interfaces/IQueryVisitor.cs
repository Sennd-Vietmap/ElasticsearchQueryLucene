using ElasticsearchQueryLucene.Core.Models;

namespace ElasticsearchQueryLucene.Core.Interfaces;

public interface IQueryVisitor
{
    void Visit(TermQueryNode node);
    void Visit(TermsQueryNode node);
    void Visit(MatchQueryNode node);
    void Visit(MatchPhraseQueryNode node);
    void Visit(PrefixQueryNode node);
    void Visit(WildcardQueryNode node);
    void Visit(FuzzyQueryNode node);
    void Visit(RegexpQueryNode node);
    void Visit(ExistsQueryNode node);
    void Visit(IdsQueryNode node);
    void Visit(RangeQueryNode node);
    void Visit(BoolQueryNode node);
}
