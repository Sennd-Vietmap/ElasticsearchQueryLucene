# Elasticsearch Query DSL to Lucene Converter

A .NET library to convert Elasticsearch JSON Query DSL to Lucene Query Syntax.

## Features
- Supports Term level queries (term, terms, match, etc.)
- Supports Range queries with inclusive/exclusive boundaries
- Supports Boolean queries (must, should, must_not, filter)
- Handles nested query structures recursively
- Implements Visitor, Composite, and Strategy patterns
- Lucene special character escaping

## Usage

```csharp
using ElasticsearchQueryLucene.Core.Converters;

string dsl = "{ ... }";
var parser = new QueryParser();
var visitor = new LuceneQueryVisitor();

var queryNode = parser.Parse(dsl);
queryNode.Accept(visitor);
string lucene = visitor.GetResult();
```

## Project Structure
- `src/ElasticsearchQueryLucene.Core`: Core logic and models
- `src/ElasticsearchQueryLucene.Console`: Demo application
- `test/ElasticsearchQueryLucene.Tests`: Unit and integration tests
