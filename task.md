# Task Breakdown: Elasticsearch DSL to Lucene Conversion

Project dotnet core library to convert Elasticsearch DSL to Lucene Query Syntax. Console application to test the library. Use Design parterns to implement the converter: Visitor Pattern,Composite Pattern,Strategy Pattern.

You can use this checklist to track your progress as you implement the converter. 
Each item can be marked with [/] when in progress and [x] when completed.

Marksure code build successfully before moving to next phase and commit code after each phase.



## Phase 1: Project Setup & Architecture
- [x] Create C# project structure
  - [x] Initialize .NET project (Console/Library)
  - [x] Add necessary NuGet packages (JSON parsing, testing frameworks)
  - [x] Set up project folders (Models, Converters, Utils, Tests)
- [x] Design core architecture
  - [x] Define interfaces for converter pattern
  - [x] Create base query converter class
  - [x] Plan recursive traversal strategy for JSON tree

## Phase 2: Core Conversion Logic - Term Level Queries
- [x] Implement Term query converter
  - [x] `term` → `field:value`
  - [x] Handle field name escaping
- [x] Implement Terms query converter
  - [x] `terms` → `field:(value1 OR value2)`
  - [x] Handle array iteration and OR logic
- [x] Implement Match query converter
  - [x] `match` → `field:(text)`
  - [x] Handle full-text search syntax
- [x] Implement Match Phrase query converter
  - [x] `match_phrase` → `field:"exact phrase"`
  - [x] Preserve phrase order with quotes
- [x] Implement Prefix query converter
  - [x] `prefix` → `field:value*`
- [x] Implement Wildcard query converter
  - [x] `wildcard` → `field:k?m*`
  - [x] Preserve `?` and `*` characters
- [x] Implement Fuzzy query converter
  - [x] `fuzzy` → `field:value~2`
  - [x] Handle fuzziness parameter
- [x] Implement Regexp query converter
  - [x] `regexp` → `field:/pattern/`
  - [x] Wrap pattern in forward slashes
- [x] Implement Exists query converter
  - [x] `exists` → `_exists_:field`
- [x] Implement IDs query converter
  - [x] `ids` → `_id:(\"1\" \"4\" \"100\")`

## Phase 3: Range Query Conversion
- [x] Implement Range query converter
  - [x] Handle inclusive range: `[min TO max]`
  - [x] Handle exclusive range: `{min TO max}`
  - [x] Handle half-open ranges: `[min TO *]` or `[* TO max]`
  - [x] Support date math expressions: `now-1d/d`
  - [x] Map `gte`/`lte` to `[]` brackets
  - [x] Map `gt`/`lt` to `{}` brackets

## Phase 4: Boolean/Compound Query Logic
- [x] Implement Bool query converter
  - [x] Handle `must` clause → `+` operator (AND logic)
  - [x] Handle `should` clause → `OR` operator
  - [x] Handle `must_not` clause → `-` operator (NOT logic)
  - [x] Handle `filter` clause → `+` operator (no scoring)
  - [x] Implement `minimum_should_match` logic
- [x] Implement nested boolean logic
  - [x] Add parentheses wrapping for each bool block
  - [x] Handle mixed `must` + `should` combinations
  - [x] Ensure proper operator precedence
- [x] Handle empty clause arrays
  - [x] Skip empty `must: []`
  - [x] Skip empty `should: []`
  - [x] Skip empty `must_not: []`

## Phase 5: String Escaping & Safety
- [x] Implement Lucene special character escaping
  - [x] Escape: `+ - && || ! ( ) { } [ ] ^ " ~ * ? : \ /`
  - [x] Create escape utility function
  - [x] Apply escaping to field names and values
- [x] Implement operator casing enforcement
  - [x] Ensure `AND`, `OR`, `NOT` are uppercase
- [x] Handle edge cases
  - [x] Empty strings
  - [x] Null values
  - [x] Special Unicode characters

## Phase 6: Recursive JSON Traversal
- [x] Implement recursive parser
  - [x] Traverse JSON tree depth-first
  - [x] Identify query type at each node
  - [x] Route to appropriate converter
  - [x] Combine results with proper operators
- [x] Handle nested query structures
  - [x] Support multiple levels of bool nesting
  - [x] Maintain parentheses hierarchy
  - [x] Preserve logical relationships

## Phase 7: Testing & Validation
- [x] Create unit tests for term-level queries
  - [x] Test each query type individually
  - [x] Verify exact output format
- [x] Create unit tests for range queries
  - [x] Test inclusive/exclusive boundaries
  - [x] Test date math expressions
- [x] Create unit tests for boolean queries
  - [x] Test simple bool combinations
  - [x] Test nested bool structures
  - [x] Test empty clause handling
- [x] Create integration tests
  - [x] Test complex example from spec (line 66-79)
  - [x] Test advanced example from spec (line 104-120)
  - [x] Verify AC1: Valid Lucene syntax
  - [x] Verify AC2: Correct nested boolean handling
  - [x] Verify AC3: Correct range bracket formatting
- [x] Edge case testing
  - [x] Test special character escaping
  - [x] Test malformed JSON handling
  - [x] Test unsupported query types

## Phase 8: Documentation & Finalization
- [x] Create API documentation
  - [x] Document public methods
  - [x] Provide usage examples
  - [x] Document limitations
- [x] Create README
  - [x] Installation instructions
  - [x] Quick start guide
  - [x] Conversion examples
- [x] Code cleanup
  - [x] Remove debug code
  - [x] Optimize performance
  - [x] Apply code formatting standards
- [x] Final validation
  - [x] Run all tests
  - [x] Verify acceptance criteria
  - [x] Performance benchmarking

## Phase 9: NuGet Publishing
- [x] Configure NuGet metadata in `ElasticsearchQueryLucene.Core.csproj`
- [x] Add versioning and author information
- [x] Prepare `README.md` and `MAPPING.md` for package inclusion
- [x] Set up GitHub Actions workflow for automated publishing
- [x] Generate and verify `.nupkg` locally
- [/] Push to NuGet.org
