# EF Core Provider Implementation: Key Learnings

## 1. Change Tracking & Identity Resolution
In a custom EF Core provider, simply materializing entities (e.g., creating `new Pet { ... }` from a lucene document) keeps them **Detached**. For `Update` and `Delete` to work automatically, entities must be **Tracked**.

### The Challenge
If you simply call `context.Attach(entity)` on every result:
- It works for the first query.
- It **crashes** if the context is already tracking that entity (e.g., from a previous query or manual add) because EF Core enforces **Identity Resolution** (one instance per ID per Context).

### The Solution: `IStateManager`
The correct pattern requires interacting with the internal `IStateManager`:
1.  **Check for Existing Entry**: Use `stateManager.TryGetEntry(key)` to see if the entity is already loaded.
2.  **Return Existing**: If found, return the **existing instance** (discard the new one materialized from the DB). This preserves reference equality.
3.  **Attach New**: Only attached if not found.

## 2. Accessing Internal Services
Many core components needed for deep integration (like `IStateManager`) are `internal` to EF Core.
- **Access**: Use `context.GetService<T>()` or `dependencies.StateManager` (if accessible).
- **Warnings**: Must suppress `EF1001` (Internal API usage) using `#pragma warning disable EF1001`.

## 3. Query Compilation & Injection
Logic to track entities cannot easily be put in the "Shaper" (the expression that creates the object) because the Shaper doesn't have access to the `QueryContext` (which holds the `DbContext`).
- **Visitor Pattern**: Modification must happen in `ShapedQueryCompilingExpressionVisitor`.
- **Injection**: We inject a method call (e.g., `TrackEntities`) that wraps the resulting `IEnumerable<T>`. This wrapper performs the Identity Resolution as the user iterates the results.

## 4. Type Mapping in Query Translation
Integrating `ITypeMappingSource` into the query translator ensures that values in LINQ predicates (like `Where(p => p.Age > 5)`) are formatted consistently with the fields in the index.
- **Value Conversion**: If a property has a `ValueConverter`, the translator must apply it before formatting the value for Lucene.
- **Consistency**: Centralizing mapping prevents discrepancies between `SaveChanges` (indexing) and `Query` (searching).

## 5. Select Projection Support
To support `Select(p => new { ... })`, the query pipeline must rewrite the query result shaper.
- **Identity Result**: If the selector is the identity (`p => p`), we return the full entity shaper (with tracking).
- **Custom Shapers**: For anonymous types or specific fields, we use an `ExpressionVisitor` to map property accesses (e.g., `p.Name`) to the underlying data buffer (`object[]`).
- **Performance**: Materializing only specific fields from the index via projections can significantly reduce I/O and memory overhead.

## 6. Lambda Expression Casting
The `ShaperExpression` in a `ShapedQueryExpression` is of type `Expression`. However, access to its `Parameters` (necessary for rewriting or wrapping it) requires casting to `LambdaExpression`. Failure to do this during query compilation leads to build errors or runtime failures when trying to manipulate the expression tree.

## 7. Parameter Resolution (EF Core 10)
Variables in LINQ (like `Where(p => p.Id == id)`) are represented as `QueryParameterExpression`. Since they can't be evaluated during translation, we use a placeholder system (`@@paramName@@`) and resolve them in the execution phase using `QueryContext.Parameters`. Note: In EF 10, the property is `Parameters`, not `ParameterValues`.
