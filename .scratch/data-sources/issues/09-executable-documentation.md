# 09: Executable documentation

**What to build:** The documentation matches the shipped behaviour and is checked by `dotnet test`. Settle the proposed member names (`DataSourceDeclarations`, `JsonQueryValidator`, `QueryProblem`, `Arg.From`, `GetAsync`, `FakeDataSource.With`, `EvaluationOptions`), update `docs/data-sources.md` and the README section to match, and replace each `doctest:skip` with a real marker per `docs/doc-examples.md`. Add the new package to the README Packages table, the CONTEXT.md package summary and ADR-0004's note.

**Blocked by:** 02, 03, 05, 06, 07, 08

**Status:** ready-for-agent

- [ ] No `doctest:skip` is left in the data-sources material except structure-only diagrams
- [ ] The doc example checks pass in `dotnet test`
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
