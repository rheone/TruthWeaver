# 05: Compile-time query validation

**What to build:** A malformed query becomes a compile diagnostic. `IQueryValidator` (stateless `Validate(string query)` returning problems) lives in `TruthWeaver.Abstractions`; a source name may be declared with one; the diagnostic carries a code, the path or span of the query string and the validator's message (decision 4). `TruthWeaver.DataSources.Json` ships `JsonQueryValidator`, which parses the JSONPath without a document. Names declared without a validator are not checked. Validation is syntax only.

**Blocked by:** 01, 04

**Status:** ready-for-agent

- [ ] A malformed JSONPath is a diagnostic in DSL, JSON and YAML rules, pointing at the query string
- [ ] A valid query, and a name declared without a validator, produce no diagnostic
- [ ] `JsonQueryValidator` accepts valid RFC 9535 queries and reports syntax errors with a position
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
