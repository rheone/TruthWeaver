# 05: Compile-time query validation

**What to build:** A malformed query becomes a compile diagnostic. `IQueryValidator` (stateless `Validate(string query)` returning problems) lives in `TruthWeaver.Abstractions`; a source name may be declared with one; the diagnostic carries a code, the path or span of the query string and the validator's message (decision 4). `TruthWeaver.DataSources.Json` ships `JsonQueryValidator`, which parses the JSONPath without a document. Names declared without a validator are not checked. Validation is syntax only.

**Blocked by:** 01, 04

**Status:** done

- [x] A malformed JSONPath is a diagnostic in DSL, JSON and YAML rules, pointing at the query string
- [x] A valid query, and a name declared without a validator, produce no diagnostic
- [x] `JsonQueryValidator` accepts valid RFC 9535 queries and reports syntax errors with a position
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- **Public API.** `IQueryValidator.Validate(string) : IReadOnlyList<QueryProblem>` and `QueryProblem(string Message, int? Position = null)` in `TruthWeaver.Abstractions`; `DataSourceDeclarations.Add(name, validator)`, the `DataSourceDeclarations[name]` indexer (get and set) and diagnostic `TRE0025` (`DiagnosticCodes.MalformedDataQuery`) in `TruthWeaver`; `JsonQueryValidator.Instance` in `TruthWeaver.DataSources.Json`. A bare `Add(name)` never discards a validator already given.
- One `TRE0025` per problem. The DSL span is the quoted query token (a position inside the query cannot be mapped through string escapes, so the position is in the message); JSON and YAML point at `$.args.x.query`. An undeclared source reports `TRE0024` only.
- JsonPath.Net 2.2.0 throws `IndexOutOfRangeException` for a query ending right after a dot (`$.`); `JsonPaths.TryParse` turns that into a normal problem at the end of the query. `JsonQueryValidator` and `JsonDataSource` share that parse, so they accept the same queries.
- The YAML criterion is covered with a validator test double in `QueryValidationTests` (core); the real JSONPath validator against YAML is tested in ticket 06.
