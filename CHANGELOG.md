# Changelog

All notable changes to this project will be documented in this file.

## [1.2.0] - 2026-01-21

### Added
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
