# 01: Scalar variable, end to end in the DSL

**What to build:** A rule such as `ageAtLeast(min: from("user", "$.minAge"))` compiles and evaluates against an in-memory source. This is the tracer bullet through every layer: the kernel types `IDataSource`, `DataQueryResult` and `DataSources` in `TruthWeaver.Abstractions` (async, nodes already converted to `LiteralValue`, errors as data; ADR-0006 decisions 3 and 9); a variable-reference argument in the expression tree with term identity by predicate, argument names and every source name plus query text, and the analyzer treating identical references as one variable (decision 11); `from("source", "query")` parsed and printed in the DSL; compile-time declaration of source names with an unknown-name diagnostic and a "did you mean" suggestion (decision 4, without validators); `EvaluateAsync(context, DataSources, ct)` resolving one `String` or `Int64` match; a missing result as `Unknown` plus a `Fault`; and `FakeDataSource` in `TruthWeaver.Testing`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A rule using `from(...)` round-trips through the DSL printer and evaluates to the expected `Decision` against a `FakeDataSource`
- [x] An undeclared source name is a compile diagnostic; a declared source not supplied to `EvaluateAsync` gives `Unknown` plus a fault
- [x] Identical references are one analyzer variable; differing queries are different variables
- [x] Existing `EvaluateAsync(context)` callers and rules without variables behave exactly as before
- [x] Architecture tests still pass; `TruthWeaver.Abstractions` gains no third-party dependency
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- Public API added: `VariableReference`, `IDataSource`, `DataQueryResult`, `DataQueryErrorKind`, `DataSources`, `VariableFailureKind`, `VariableResolutionException` (all `TruthWeaver.Abstractions`), `TermIdentity.Variables` and `TermIdentity.FormatArguments()`, `DataSourceDeclarations` and `CompilerOptions.DataSources`, diagnostic `TRE0024` (`DiagnosticCodes.UndeclaredDataSource`), the `CompiledRule.EvaluateAsync(context, services, dataSources, options, cancellationToken)` overload, and `FakeDataSource` (`TruthWeaver.Testing`).
- The ticket's `EvaluateAsync(context, DataSources, ct)` is the existing `EvaluateAsync(context, services, ...)` with a `DataSources?` parameter after `services`, because the engine's evaluate method already requires an `IServiceProvider`.
- `DataSourceDeclarations` only collects names for now; ticket 05 adds the optional query validator per name.
- A variable inside an array literal (`[from(...)]`) is a syntax error; only a whole argument can be a reference.
- The JSON and YAML printers write a variable as `{ "from": ..., "query": ... }` so a compiled rule never loses a reference when printed; reading it back (parsers and `rule-tree.schema.json`) is ticket 03.
- The evaluator implements ticket 02's resolution rules in the same change (the same code path), so ticket 02 adds its own tests and docs on top.
