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
*   **Analysis Chain**: Supports `StandardAnalyzer` (Tokenizer -> LowerCaseFilter -> StopFilter).
*   **Type Safety**: Strong typing for field names via `CreateShapedQueryExpression`.

## Architecture Keywords
*   **Component**: `ILuceneDatabase` (Storage Interface).
*   **Pattern**: Repository / Unit of Work (via DbContext).
*   **Dependency Injection**: `IServiceCollection` extension via `UseLucene()`.
*   **Metadata**: Fluent API `IsStored()`, `IsTokenized()` & Data Annotations `[LuceneField]`.
*   **Version Compatibility**: .NET 8.0, EF Core 10.0.0, Lucene.Net 4.8.0-beta.

## Usage vector
Ideal for: RAG (Retrieval-Augmented Generation) Knowledge Base, Local Full-Text Search, NoSQL Document Storage, High-Speed Filtering.
