using ElasticsearchQueryLucene.Core.Converters;

Console.WriteLine("Elasticsearch DSL to Lucene Query Converter");
Console.WriteLine("-------------------------------------------");

var dsl = @"
{
  ""query"": {
    ""bool"": {
      ""must"": [{ ""term"": { ""brand"": ""apple"" } }],
      ""should"": [
        { ""match"": { ""color"": ""red"" } },
        { ""match"": { ""color"": ""blue"" } }
      ],
      ""filter"": [{ ""range"": { ""price"": { ""lte"": 500 } } }]
    }
  }
}";

var parser = new QueryParser();
var visitor = new LuceneQueryVisitor();

try
{
    var queryNode = parser.Parse(dsl);
    queryNode.Accept(visitor);
    var lucene = visitor.GetResult();

    Console.WriteLine("Input DSL:");
    Console.WriteLine(dsl);
    Console.WriteLine("\nOutput Lucene Syntax:");
    Console.WriteLine(lucene);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
