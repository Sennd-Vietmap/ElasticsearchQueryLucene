# Elasticsearch Query DSL to Lucene Converter

A lightweight .NET library designed to bridge the gap between Elasticsearch's JSON-based Query DSL and the classic Lucene Query Syntax (String).

## 🚀 Key Features

-   **Comprehensive Query Support**:
    -   **Term Level**: `term`, `terms`, `match`, `match_phrase`, `prefix`, `wildcard`, `fuzzy`, `regexp`, `exists`, `ids`.
    -   **Ranges**: Support for `gte`, `gt`, `lte`, `lt` with correct `[]` and `{}` bracket mapping.
    -   **Boolean Logic**: Full support for `must` (+), `should` (OR), `must_not` (-), and `filter` (+) with recursive nesting.
-   **Strict Input Validation**:
    -   **Max Size**: 100KB per JSON string.
    -   **Max Depth**: 5 levels of query nesting to prevent stack overflow/DOS.
    -   **Detailed Errors**: Detailed error messages including line and column numbers for malformed JSON.
-   **[Detailed Mapping Guide](MAPPING.md)**: Find the exact conversion rules for every supported query type.
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
| `src/ElasticsearchQueryLucene.EntityFrameworkCore` | **EF Core Lucene Provider** (In Development) - Custom EF Core provider for Lucene.Net 4.8. |
| `src/ElasticsearchQueryLucene.Console` | Live demo with real data and Lucene search engine. |
| `test/ElasticsearchQueryLucene.Tests` | Unit and integration tests. |

## 🔬 EF Core Lucene Provider (Experimental)

An experimental Entity Framework Core provider that enables using Lucene.Net as a data store with full ORM capabilities.

### Current Status
- ✅ **Phase 1-3**: Foundation, Metadata Mapping, and Update Pipeline (Create) completed
- ✅ **Phase 4**: Query Pipeline Materialization completed
- ✅ **Phase 5**: LINQ Translation (Read) completed
- ✅ **Phase 6**: Full CRUD & State Management completed
- 🚧 **Phase 7**: Advanced Features (In Progress)

### Features Implemented
- Fluent API and Data Annotations for Lucene field configuration
- `IndexWriter` lifecycle management integrated with EF Core's `SaveChanges()`
- Custom metadata annotations (`Stored`, `Tokenized`, `Analyzer`)
- Query compilation infrastructure for EF Core 10
- **LINQ-to-Lucene query translation**:
  - `Where()` with full predicate support (equality, comparison, boolean logic, string methods)
  - `Skip()` and `Take()` for pagination
  - `OrderBy()`, `OrderByDescending()`, `ThenBy()`, `ThenByDescending()` for sorting
  - `EF.Functions.LuceneMatch()` for raw Lucene query syntax
  - `FirstOrDefault()` with optional predicates
  - Automatic Lucene special character escaping
  - Proper range query syntax
- **Full CRUD operations**:
  - Create: Add entities to Lucene index
  - Read: Query execution with LINQ translation
  - Update: Modify existing documents
  - Delete: Remove documents from index
  - Bulk operations support

### Example Usage (Preview)
```csharp
public class Book
{
    public int Id { get; set; }
    
    [LuceneField(Stored = true, Tokenized = true)]
    public string Title { get; set; }
    
    [LuceneField(Stored = true, Tokenized = false)]
    public string ISBN { get; set; }
}

public class BookContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var directory = new RAMDirectory();
        optionsBuilder.UseLucene(directory, "books");
    }
    
    public DbSet<Book> Books { get; set; }
}

// Usage
using var context = new BookContext();
context.Books.Add(new Book { Id = 1, Title = "EF Core in Action", ISBN = "978-1617298363" });
context.SaveChanges(); // Indexed to Lucene!

// Query Usage
var results = context.Books
    .Where(b => b.Title.Contains("Action"))
    .OrderBy(b => b.ISBN)
    .Skip(0)
    .Take(10)
    .ToList();

// Raw Lucene Match
var rawResults = context.Books
    .Where(b => EF.Functions.LuceneMatch(b.Title, "Core OR Entity"))
    .ToList();
```

For detailed progress, see [`task_efcore.md`](task_efcore.md).

## 🛠 Prerequisites
- .NET 8.0 or later.
- `Lucene.Net` (included in demo project).
