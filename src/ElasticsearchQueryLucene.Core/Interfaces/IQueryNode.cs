namespace ElasticsearchQueryLucene.Core.Interfaces;

public interface IQueryNode
{
    void Accept(IQueryVisitor visitor);
}
