# Elasticsearch DSL to Lucene Syntax Mapping Guide

This document provides a detailed reference for how Elasticsearch Query DSL (JSON) is converted into Lucene Query Syntax (String) by this library.

## 1. Term-Level Queries

| Elasticsearch DSL (JSON) | Lucene Syntax (String) | Description |
| :--- | :--- | :--- |
| `{"term": {"user.id": "kimchy"}}` | `user.id:kimchy` | Exact match search (non-analyzed). |
| `{"terms": {"tag": ["search", "open"]}}` | `tag:(search OR open)` | Matches any value in the provided array. |
| `{"match": {"msg": "hello world"}}` | `msg:(hello world)` | Full-text query. |
| `{"match_phrase": {"msg": "hello world"}}` | `msg:"hello world"` | Exact phrase match with sequence preservation. |
| `{"prefix": {"user": "ki"}}` | `user:ki*` | Matches fields starting with the prefix. |
| `{"wildcard": {"user": "k?m*"}}` | `user:k?m*` | Supports `?` (single char) and `*` (multi char). |
| `{"fuzzy": {"user": "ki"}}` | `user:ki~2` | Levenshtein distance based fuzzy search. |
| `{"regexp": {"name": "s.*y"}}` | `name:/s.*y/` | Regular expression search. |
| `{"exists": {"field": "user"}}` | `_exists_:user` | Finds documents where the field is non-null. |
| `{"ids": {"values": ["1", "4"]}}` | `_id:("1" "4")` | Finds documents by unique ID list. |

## 2. Range Queries

Mapping of range boundaries to Lucene brackets:
-   `gte` / `lte` -> Inclusive `[]`
-   `gt` / `lt`   -> Exclusive `{}`

| Elasticsearch DSL | Lucene Syntax | Description |
| :--- | :--- | :--- |
| `{"range": {"age": {"gte": 10, "lte": 20}}}` | `age:[10 TO 20]` | Range from 10 to 20 (inclusive). |
| `{"range": {"age": {"gt": 10, "lt": 20}}}` | `age:{10 TO 20}` | Range from 10 to 20 (exclusive). |
| `{"range": {"age": {"gte": 10}}}` | `age:[10 TO *]` | Greater than or equal to 10. |
| `{"range": {"date": {"gte": "now-1d/d"}}}` | `date:[now-1d/d TO *]` | Supports Date Math expressions. |

## 3. Boolean/Compound Queries

| DSL Clause | Lucene Operator | Resulting Syntax Example |
| :--- | :--- | :--- |
| `must` | `+` (AND) | `+field1:A +field2:B` |
| `should` | `OR` | `(field1:A OR field1:B)` |
| `must_not` | `-` (NOT) | `-field1:A` |
| `filter` | `+` | `+field1:A` (equivalent to must in string syntax) |

### 🧠 Nested Boolean Logic
When `bool` blocks are nested, the library automatically wraps them in parentheses to preserve logical precedence.

**Example:**
```json
{
  "bool": {
    "must": [{ "term": { "category": "books" } }],
    "should": [
      { "term": { "genre": "sci-fi" } },
      { "term": { "genre": "fantasy" } }
    ]
  }
}
```
**Converted Lucene:**
`+category:books +(genre:sci-fi OR genre:fantasy)`

## 4. Special Character Escaping

The following characters are automatically escaped with a backslash (`\`) to prevent Lucene parsing errors:
`+ - && || ! ( ) { } [ ] ^ " ~ * ? : \ /`

**Example:**
-   Input: `{"term": {"name": "john+doe"}}`
-   Output: `name:john\+doe`

## 🛡️ Validation Constraints
- **Max JSON Size**: 100KB.
- **Max Nesting Depth**: 5 levels.
- **Invalid JSON**: Triggers a `FormatException` with line/column details.
