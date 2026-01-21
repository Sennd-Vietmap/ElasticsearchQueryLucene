# Task Breakdown: EF Core Provider for Lucene.Net 4.8

Project to extend Entity Framework Core to support Lucene.Net 4.8 as a database provider.

You can use this checklist to track your progress as you implement the provider. 
Each item can be marked with [/] when in progress and [x] when completed.

Mark sure code build successfully before moving to next phase and commit code after each phase.

## Phase 1: Foundation & Dependency Injection
- [x] Create C# project `ElasticsearchQueryLucene.EntityFrameworkCore`
- [x] Add dependencies: `Microsoft.EntityFrameworkCore`, `Lucene.Net`
- [x] Implement `LuceneDbContextOptionsExtension`
- [x] Implement `LuceneServiceCollectionExtensions`
- [x] Verify `optionsBuilder.UseLucene()` registration logic

## Phase 2: Metadata & ORM Mapping
- [x] Create `LuceneAnnotationNames` for custom metadata
- [x] Implement Fluent API extensions (`IsStored`, `IsTokenized`, `HasAnalyzer`)
- [x] Implement `LuceneAttributeQuerySource` for data annotations
- [x] Verify metadata storage in `IMutableModel`

## Phase 3: Update Pipeline (Create & Delete)
- [x] Implement `ILuceneDatabase` (Writer lifecycle management)
- [x] Implement `LuceneModificationCommandBatch`
- [x] Implement `LuceneBatchExecutor` for `SaveChangesAsync()`
- [x] Verify basic document creation in Lucene index

## Phase 4: Query Pipeline - Materialization
- [x] Implement `LuceneQueryCompilationContext`
- [x] Implement `LuceneShapedQueryCompilingExpressionVisitor`
- [x] Create `LuceneEntityMaterializerSource` (using `IStructuralTypeMaterializerSource` for EF Core 10)
- [x] Verify POCO materialization stubs

## Phase 5: LINQ Translation (Read)
- [ ] Implement `LuceneQuerySqlGenerator` (Query builder)
- [ ] Map LINQ operators (`==`, `&&`, `||`, `>`, `<`)
- [ ] Integrate `ElasticsearchQueryLucene.Core` logic
- [ ] Verify simple LINQ queries

## Phase 6: Full CRUD & State Management
- [ ] Implement `UpdateDocument` logic
- [ ] Handle stable `Key` lookups
- [ ] Implement `DeleteDocuments`
- [ ] Verify full CRUD cycle

## Phase 7: Advanced Search Features
- [ ] Implement `EF.Functions.LuceneMatch`
- [ ] Implement `OrderBy` (Sort fields)
- [ ] Implement `Take()` / `Skip()` (Pagination)
- [ ] Final Integration Demo
