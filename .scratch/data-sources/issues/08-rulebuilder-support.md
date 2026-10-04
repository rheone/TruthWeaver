# 08: RuleBuilder support

**What to build:** A builder rule can use variables, and can fix a value from a source while it is assembled (decision 12). `Arg.From(source, query)` is accepted wherever `RuleBuilder` takes an argument value and renders to the same JSON tree as the other formats, optionally validating the query with a supplied `IQueryValidator`. A helper reads a value from an `IDataSource` at build time and inserts the resulting literal.

**Blocked by:** 03, 04

**Status:** ready-for-agent

- [ ] A builder rule using `Arg.From` compiles to the same canonical text as the DSL form
- [ ] The eager helper produces an ordinary literal argument, not a variable
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
