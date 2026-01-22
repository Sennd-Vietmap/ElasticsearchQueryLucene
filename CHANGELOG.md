# Changelog

All notable changes to this project will be documented in this file.

## [2.0.2] - 2026-01-23

### Added
- **Developer Experience (DX) Pack**:
    - **Diagnostic Logging**: Integrated `Microsoft.Extensions.Logging` to expose raw Lucene queries, sort criteria, and execution time directly in the console/logs.
    - **Lucene Explorer Dashboard**: New ASP.NET Core Middleware (`UseLuceneExplorer`) that provides a web UI to browse indexes and a "Search Playground" to test raw queries.
    - **Field Boosting**: Added `.Boost()` LINQ extension (via `EF.Functions.Boost`) to influence search relevance by weighting specific fields.

## [2.0.1] - 2026-01-22

### Fixed
- **Legacy Test Stabilization**: 
    - Modernized 42 unit tests in `ElasticsearchQueryLucene.Tests` to align with v2.0 quoting and case-sensitivity behavior.
    - Resolved provider registration conflicts in `LuceneProviderTests` by isolating `InMemory` from `Lucene` provider.
- **Materialization**: Added null-safety check for value-type materialization to prevent `NullReferenceException` on missing fields.

## [2.0.0] - 2026-01-22

### Added
- **v2.0 Architecture Upgrade**:
    - **Strict Type Mapping**: Implemented `LuceneTypeMappingSource` to centralize .NET-to-Lucene conversion.
    - **Value Converter Support**: Support for `DateTime` (ticks) and `Guid` (string) with automated round-trip transformation during materialization.
    - **Parameterized Queries**: Support for variables in LINQ (e.g., `.Skip(count)`) via expression evaluation and a new `@@NAME@@` placeholder system during compilation.
    - **Enhanced Projection Shaper**: Improved support for anonymous types and specific field selection.
    - **Robust Identity Tracking**: Integrated with `IStateManager` to return tracked entities and resolve identity conflicts.
- **Functional Testing**:
    - Comprehensive test suite in `test/ElasticsearchQueryLucene.FunctionalTests`.
    - 8 passing test cases covering full CRUD, paging, sorting, and complex type support.

## [1.4.0] - 2026-01-22

### Added
- **Aggregations Support**:
    - Implemented `Count()`, `LongCount()`, and `Any()` LINQ translation.
    - Optimized `Count/Any` execution to use Lucene's `TotalHits` (metadata only) avoiding document materialization.
- **Improved Result Cardinality Handling**:
    - Implemented `TranslateFirstOrDefault` with correct `ResultCardinality.Single` or `ResultCardinality.SingleOrDefault`.
    - Updated execution visitor to wrap queries with `Enumerable.Single()` or `SingleOrDefault()` based on cardinality.
- **Demo Enhancements**:
    - Added verification steps for Aggregations and FirstOrDefault.
    - Verified end-to-end functionality including Updates and Deletes with correct state persistence.

### Fixed
- **Runtime Type Mismatch**:
    - Resolved `ArgumentException: The type Pet does not represent a sequence` by overriding `VisitExtension` in `LuceneShapedQueryCompilingExpressionVisitor` to bypass incorrect base validation for scalar results.
- **Pipeline Stability**:
    - Fixed `ValueBuffer` vs `object[]` transport issues in the query pipeline.

## [1.3.0] - 2026-01-21

### Added
- **EF Core Lucene Provider (In Progress)**:
    - Initial infrastructure for a custom EF Core provider for Lucene.Net 4.8.
    - Support for Fluent API and Data Annotations (`[LuceneField]`) to configure Lucene-specific metadata.
    - Implemented Update Pipeline (Phase 3) for entity creation and document indexing.
    - Implemented Query Pipeline Materialization (Phase 4) using `IStructuralTypeMaterializerSource` for EF Core 10.
    - **Implemented LINQ Translation (Phase 5)**:
        - Full LINQ-to-Lucene query translation via `LuceneExpressionTranslator`
        - Support for `Where()` with equality, comparison, boolean logic, and string methods
        - Support for `Skip()`, `Take()`, `FirstOrDefault()`, and `Select()`
        - Automatic Lucene special character escaping
        - Proper range query syntax with inclusive/exclusive brackets
        - 27 passing tests for query translation functionality
    - **Implemented Full CRUD & State Management (Phase 6)**:
        - Update operation using Lucene.Net `UpdateDocument`
        - Delete operation using Lucene.Net `DeleteDocuments`
        - Primary key extraction with `GetKeyValue` helper
        - Query execution infrastructure with `LuceneShapedQueryCompilingExpressionVisitor`
        - Actual Lucene search execution with result materialization
        - Support for Skip/Take in query execution
        - 6 comprehensive CRUD test cases
    - **Implemented Advanced Search Features (Phase 7)**:
        - `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending` translation and execution
        - Automatic Lucene `Sort` object creation with correct type mapping (Int32, Int64, Single, Double)
        - `EF.Functions.LuceneMatch` extension for raw Lucene query syntax integration
        - Verify raw query translation in LINQ pipeline
        - 42 passing tests covering all phases
    - Integrated `IndexWriter` lifecycle management within the EF Core `ILuceneDatabase`.
    - Added comprehensive unit tests for provider configuration and metadata mapping.

## [1.2.0] - 2026-01-21

### Added
- **NuGet Publishing Setup**: 
    - Configured project metadata for `ElasticsearchQueryLucene.Core`.
    - Included `README.md` and `MAPPING.md` in the NuGet package.
    - Added GitHub Actions workflow for automated publishing on tag push.
- **Input Validation Rules**: 
    - 100KB JSON size limit.
    - 5-level query nesting depth limit.
    - Detailed error reporting with line/column pointers for invalid JSON.
- **Improved Stability**: Prevents recursion overflow and memory issues from malicious or malformed DSL.

## [1.1.0] - 2026-01-21

### Added
- Integrated **Lucene.Net** (4.8.0-beta) for real-world validation.
- Enhanced Console application with book dataset and live search execution.
- Detailed architectural documentation in README.
- `.gitignore` file for .NET development.

### Fixed
- Namespace ambiguity between `Lucene.Net.QueryParsers.Classic` and local `QueryParser`.
- Recursive parentheses wrapping for complex boolean queries.
- Range query bracket mapping (inclusive/exclusive).

## [1.0.0] - 2026-01-21

### Added
- Initial implementation of Elasticsearch DSL to Lucene Query Syntax converter.
- Support for Term, Terms, Match, Match Phrase, Prefix, Wildcard, Fuzzy, Regexp, Exists, IDs queries.
- Recursive Boolean query processing (must, should, must_not, filter).
- Lucene special character escaping utility.
- XUnit test suite with 100% pass rate.
- Implementation of Visitor, Composite, and Strategy patterns.
