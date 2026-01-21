# Elasticsearch Query DSL to Lucene Converter

A lightweight .NET library designed to bridge the gap between Elasticsearch's JSON-based Query DSL and the classic Lucene Query Syntax (String).

## 🚀 Key Features

-   **Comprehensive Query Support**:
    -   **Term Level**: `term`, `terms`, `match`, `match_phrase`, `prefix`, `wildcard`, `fuzzy`, `regexp`, `exists`, `ids`.
    -   **Ranges**: Support for `gte`, `gt`, `lte`, `lt` with correct `[]` and `{}` bracket mapping.
    -   **Boolean Logic**: Full support for `must` (+), `should` (OR), `must_not` (-), and `filter` (+) with recursive nesting.
-   **Architecture-First Design**: Built using industry-standard patterns:
    -   **Visitor Pattern**: Separates traversal from syntax generation.
    -   **Composite Pattern**: Handles hierarchical query structures.
    -   **Strategy Pattern**: Routes JSON nodes to specific converters.
-   **Production Ready**:
    -   Automatic Lucene character escaping (e.g., `+`, `-`, `&&`, etc.).
    -   Comprehensive XUnit test suite.
    -   Verified against real `Lucene.Net` execution.

## 🏗 Architecture

The converter operates in two main stages:
1.  **Parsing**: `QueryParser` reads the JSON and builds a `QueryNode` tree (Domain Model).
2.  **Conversion**: `LuceneQueryVisitor` traverses the tree and generates the final Lucene string.

## 💻 Usage

### Basic Conversion
```csharp
using ElasticsearchQueryLucene.Core.Converters;

string json = "{\"term\": {\"user.id\": \"kimchy\"}}";
var parser = new QueryParser();
var visitor = new LuceneQueryVisitor();

var queryNode = parser.Parse(json);
queryNode.Accept(visitor);

Console.WriteLine(visitor.GetResult()); // Output: user.id:kimchy
```

### Real-Data Testing
The project includes a demo console application that indexes a sample book dataset and executes converted queries against `Lucene.Net`.

```bash
dotnet run --project src/ElasticsearchQueryLucene.Console
```

## 📂 Project Structure

| Path | Description |
| :--- | :--- |
| `src/ElasticsearchQueryLucene.Core` | Core logic, models, and visitor implementation. |
| `src/ElasticsearchQueryLucene.Console` | Live demo with real data and Lucene search engine. |
| `test/ElasticsearchQueryLucene.Tests` | Unit and integration tests. |

## 🛠 Prerequisites
- .NET 8.0 or later.
- `Lucene.Net` (included in demo project).
