# TruthWeaver.DataSources.Json

A JSON data source for the `from(...)` references of a rule, with JSONPath (RFC 9535) queries.

## Purpose

A rule term can take its value from data with a `from("source", "query")` argument. This package answers the query against
a JSON document. It also checks the query syntax when the rule compiles, so a malformed query is a diagnostic and not a
fault at run time. The check looks at syntax only. Whether the data holds a match is known when the rule is evaluated.

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `IDataSource` and `IQueryValidator`.

## Key types

| Type | Role |
| --- | --- |
| `JsonDataSource` | An `IDataSource` over one JSON document. Create it with `Parse`, `TryParse` or `Create`. |
| `JsonQueryValidator` | An `IQueryValidator` that checks a JSONPath query without a document. Use `JsonQueryValidator.Instance`. |
| `JsonPaths` | Parses a JSONPath query. |

## Notes

`JsonQueryValidator` accepts exactly the queries that `JsonDataSource` accepts, so a YAML data source can share it.
