# 01: Scalar variable, end to end in the DSL

**What to build:** A rule such as `ageAtLeast(min: from("user", "$.minAge"))` compiles and evaluates against an in-memory source. This is the tracer bullet through every layer: the kernel types `IDataSource`, `DataQueryResult` and `DataSources` in `TruthWeaver.Abstractions` (async, nodes already converted to `LiteralValue`, errors as data; ADR-0006 decisions 3 and 9); a variable-reference argument in the expression tree with term identity by predicate, argument names and every source name plus query text, and the analyzer treating identical references as one variable (decision 11); `from("source", "query")` parsed and printed in the DSL; compile-time declaration of source names with an unknown-name diagnostic and a "did you mean" suggestion (decision 4, without validators); `EvaluateAsync(context, DataSources, ct)` resolving one `String` or `Int64` match; a missing result as `Unknown` plus a `Fault`; and `FakeDataSource` in `TruthWeaver.Testing`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A rule using `from(...)` round-trips through the DSL printer and evaluates to the expected `Decision` against a `FakeDataSource`
- [ ] An undeclared source name is a compile diagnostic; a declared source not supplied to `EvaluateAsync` gives `Unknown` plus a fault
- [ ] Identical references are one analyzer variable; differing queries are different variables
- [ ] Existing `EvaluateAsync(context)` callers and rules without variables behave exactly as before
- [ ] Architecture tests still pass; `TruthWeaver.Abstractions` gains no third-party dependency
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
