# 08: RuleBuilder support

**What to build:** A builder rule can use variables, and can fix a value from a source while it is assembled (decision 12). `Arg.From(source, query)` is accepted wherever `RuleBuilder` takes an argument value and renders to the same JSON tree as the other formats, optionally validating the query with a supplied `IQueryValidator`. A helper reads a value from an `IDataSource` at build time and inserts the resulting literal.

**Blocked by:** 03, 04

**Status:** done

- [x] A builder rule using `Arg.From` compiles to the same canonical text as the DSL form
- [x] The eager helper produces an ordinary literal argument, not a variable
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- **Public API.** `TruthWeaver.Building.Arg.From(string source, string query, IQueryValidator? validator = null) : VariableReference` and `TruthWeaver.Building.DataSourceExtensions.GetAsync<T>(this IDataSource, string query, CancellationToken = default) : ValueTask<T>`. `RuleBuilder` argument values additionally accept `Guid` and `VariableReference`.
- `Arg.From` reuses `VariableReference`; the builder renders it to the JSON tree form, so there is one compile path. A validator problem throws `ArgumentException` (builder input is programmer-supplied, as for an unsupported argument type); without a validator the compiler's `DataSourceDeclarations` validation still applies.
- `GetAsync<T>` supports `string`, `long`, `decimal`, `bool`, `DateTimeOffset`, `Guid` (arrays not supported) and reuses the evaluator's conversion; failures throw `InvalidOperationException` without data values, an unsupported `T` throws `NotSupportedException`. Tests: `RuleBuilderVariableTests`.
