# 09: Executable documentation

**What to build:** The documentation matches the shipped behaviour and is checked by `dotnet test`. Settle the proposed member names (`DataSourceDeclarations`, `JsonQueryValidator`, `QueryProblem`, `Arg.From`, `GetAsync`, `FakeDataSource.With`, `EvaluationOptions`), update `docs/data-sources.md` and the README section to match, and replace each `doctest:skip` with a real marker per `docs/doc-examples.md`. Add the new package to the README Packages table, the CONTEXT.md package summary and ADR-0004's note.

**Blocked by:** 02, 03, 05, 06, 07, 08

**Status:** done

- [x] No `doctest:skip` is left in the data-sources material except structure-only diagrams
- [x] The doc example checks pass in `dotnet test`
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- **Names settled** as shipped: `DataSourceDeclarations`, `JsonQueryValidator`, `QueryProblem`, `Arg.From`, `IDataSource.GetAsync<T>` (an extension in `TruthWeaver.Building`), `FakeDataSource.With`, `EvaluationOptions.IncludeResolvedValues`. The guide's `declarations` snippet now uses the collection-initializer form `{ { "user", JsonQueryValidator.Instance }, "request" }` (an indexer initializer cannot be mixed with `Add`), and its `IncludeResolvedValues` call uses the real `EvaluateAsync(context, services, sources, options, cancellationToken)` signature.
- **Checks.** `DocExampleChecker` now declares `user` and `request` (with `JsonQueryValidator`) and registers `ageAtLeast` and `hasRole`; `docs/data-sources.md` is a third checked file in `DocExampleTests`, and the README variable example is a `doctest:rule`. Only the sequence diagram in the guide stays `doctest:skip` (structure only). The guide's C# snippets run in `DataSourcesGuideTests`.
- The package listings (README table, CONTEXT.md summary, ADR-0004 note, CHANGELOG counts) now include `TruthWeaver.DataSources.Json` (six packages).
