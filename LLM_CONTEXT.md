# System Context: ElasticsearchQueryLucene EF Core Provider

## Identity & Purpose
**Entity Framework Core Provider** implementing **Lucene.Net 4.8** as a backing store. Bridges relational **ORM paradigms** (LINQ, Change Tracking) with **Information Retrieval (IR)** concepts (Inverted Index, TF-IDF/BM25 Ranking). Designed for high-performance full-text search integration within .NET ecosystems.

## Core Capabilities (LLM Semantic Tags)

### 1. Abstract Syntax Tree (AST) Translation
*   **Mechanism**: `QueryableMethodTranslatingExpressionVisitor` pipeline.
*   **Input**: .NET `Expression<Func<T, bool>>` (LINQ Lambda).
*   **Output**: Lucene Query Syntax (DSL).
*   **Operations**:
    *   `BinaryExpression` (==, !=, >, <) -> `TermQuery`, `RangeQuery`.
    *   `MethodCallExpression` (Contains, StartsWith) -> `WildcardQuery`, `PrefixQuery`.
    *   `BooleanExpression` (&&, ||) -> `BooleanQuery` (MUST, SHOULD).
    *   `OrderBy` -> `SortField` (INT32/64, SINGLE, DOUBLE, STRING).
    *   **Aggregations**: `Count()` using `TotalHits`, `Any()`.
    *   **Metadata**: Support for `DateTime` (Ticks) and `Guid` via `ValueConverter`.
    *   **Parameters**: Dynamic resolution via `@@NAME@@` placeholders.

### 2. Inverted Index Management (CRUD)
*   **Write Pipeline**: Integrated with `DbContext.SaveChanges()`.
*   **State Handling**:
    *   `EntityState.Added` -> `IndexWriter.AddDocument()`.
    *   `EntityState.Modified` -> `IndexWriter.UpdateDocument()` (Term-based replacement).
    *   `EntityState.Deleted` -> `IndexWriter.DeleteDocuments()`.
*   **Concurrency**: Implicit `IndexWriter` locking; ACID-like atomic commits via `Commit()`.

### 3. Query Execution & Materialization
*   **Strategy**: `ShapedQueryCompilingExpressionVisitor`.
*   **Execution Flow**:
    1.  **Context Injection**: Resolves `LuceneDirectory` (RAM/FSDirectory).
    2.  **Searcher**: Instantiates `IndexSearcher` over `DirectoryReader`.
    3.  **Parsing**: Uses `QueryParser` (Classic) for raw text + AST result.
    4.  **Retrieval**: `searcher.Search(Query, n)` -> `TopDocs`.
    5.  **Pagination**: Offset/Limit implementation via `Skip()`/`Take()` on materialized `ScoreDoc` array.
    6.  **Hydration**: Reflection-based mapping of `Lucene.Net.Documents.Document` fields to CLR POCO properties.

### 4. Advanced Search Features
*   **Hybrid Querying**: `EF.Functions.LuceneMatch(prop, raw_query)` for direct Lucene syntax injection (Fuzzy, Proximity, Boosting).
*   **Analysis Chain**: Supports `StandardAnalyzer` (Tokenizer -> LowerCaseFilter -> StopFilter) and `PerFieldAnalyzerWrapper` (Keyword for exact match).
*   **Type Safety**: Strong typing for field names via `CreateShapedQueryExpression`.

## Architecture Keywords
*   **Component**: `ILuceneDatabase` (Storage Interface).
*   **Pattern**: Repository / Unit of Work (via DbContext).
*   **Dependency Injection**: `IServiceCollection` extension via `UseLucene()`.
*   **Metadata**: Fluent API `IsStored()`, `IsTokenized()` & Data Annotations `[LuceneField]`.
*   **Version Compatibility**: .NET 10.0, EF Core 10.0.0, Lucene.Net 4.8.0-beta.

## Usage vector
Ideal for: RAG (Retrieval-Augmented Generation) Knowledge Base, Local Full-Text Search, NoSQL Document Storage, High-Speed Filtering.

## Code Example

```csharp
// 1. Configure Services / Context
var luceneDir = new RAMDirectory();
var options = new DbContextOptionsBuilder<PetContext>()
    .UseLucene(luceneDir, "pets") // Use custom extension
    .Options;

using var context = new PetContext(options);

// 2. Define Entity
public class Pet
{
    public int Id { get; set; }
    [LuceneField(Stored = true, Tokenized = true, Analyzer = "StandardAnalyzer")]
    public string Description { get; set; }
    [LuceneField(Stored = true, Tokenized = false)] // Keyword analyzer for exact match
    public string Breed { get; set; }
}

// 3. CRUD & Search
// Add
context.Pets.Add(new Pet { Id = 1, Name = "Buddy", Breed = "Golden Retriever", Description = "Loves stick" });
context.SaveChanges();

// Search (Full Text)
var matches = context.Pets
    .Where(p => p.Description.Contains("stick"))
    .ToList();

// Search (Exact Match)
var exact = context.Pets
    .Where(p => p.Breed == "Golden Retriever")
    .ToList();

// Aggregation (Efficient)
var count = context.Pets.Count(p => p.Breed == "Golden Retriever");

// Advanced (Fuzzy)
var fuzzy = context.Pets
    .Where(p => EF.Functions.LuceneMatch(p.Description, "stik~")) // typo tolerance
    .ToList();
```
