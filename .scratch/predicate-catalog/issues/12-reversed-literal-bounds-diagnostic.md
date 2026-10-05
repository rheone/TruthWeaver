# 12: Compile-time diagnostic for reversed literal bounds

**What to build:** Catalog rule 7 (`CONTEXT.md`) says reversed bounds (`lower > upper`) on `Between` and `Outside` (numeric and date-time) are a compile-time diagnostic when both bounds are literals, and are never swapped. Today `PredicateSchema` and the compiler have no argument-validation hook, so the predicates throw `ArgumentException` at evaluation. The engine records a `Fault` and answers `Unknown`. Add the compile-time diagnostic for literal bounds and keep the evaluation-time `ArgumentException` for bounds that are not literals.

This needs an owner design decision first. Questions to settle:

1. The hook: an optional validator on `PredicateSchema` (a delegate over the literal arguments that returns problems), or a declared relation between two arguments (for example "lower <= upper"), or a hard-coded check for the `Between` and `Outside` names.
2. The diagnostic: its code (a new `DiagnosticCode`) and its severity (error, so `Compile` returns no rule).
3. Where the validation runs: in the Validate stage of the pipeline (ADR-0003), after argument kinds are checked and before Analyze.
4. Whether `DataSources` variable arguments and other non-literal bounds are skipped (recommended: skipped).

Recommendation: an optional validator delegate on the schema, run in the Validate stage, one new error code, non-literal bounds skipped.

**Blocked by:** none (owner decision recorded 2026-10-04)

**Status:** done

- [x] The owner records the decision on the hook, the code and the stage in the ticket comments
- [x] Tests first: reversed literal bounds on numeric `Between`, numeric `Outside`, date-time `Between` and date-time `Outside` each give the diagnostic with the position of the predicate call, and `Compile` returns no rule
- [x] Equal bounds and ordered bounds compile
- [x] A reversed bound that is not a literal still faults at evaluation (`Unknown` plus a `Fault`) and is never swapped
- [x] `docs/predicates.md`, the diagnostics reference and the gap list describe the diagnostic
- [x] The full validation from CLAUDE.md passes

Source: [predicate-catalog ticket 04](04-scalar-and-numeric-comparison.md) and [ticket 06](06-date-time-comparison.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

- 2026-10-04 owner decision: accept the recommendation in full. (1) Hook: an optional validator delegate on `PredicateSchema` over the literal arguments that returns problems, `null` by default so every existing schema is unchanged. (2) Diagnostic: one new error-severity code, so `Compile` returns no rule. (3) Stage: the Validate stage, after argument kinds are checked and before Analyze. (4) Non-literal bounds (data-source variable arguments and anything not a literal) are skipped; they still fault at evaluation (`ArgumentException`, so `Unknown` plus a `Fault`) and are never swapped. Equal and ordered bounds compile.
- 2026-10-04: Done. `PredicateSchema.ArgumentValidator` (`Func<PredicateArguments, IReadOnlyList<PredicateArgumentProblem>>?`, an init property so the positional constructor is unchanged) and the new `PredicateArgumentProblem(Message, Expected, Found, Suggestion)` in `TruthWeaver.Abstractions`. `RuleNodeCompiler.BuildTerm` calls it only when the call raised no argument error, with the literal arguments (defaults included, variables excluded), and turns each problem into `DiagnosticCodes.InvalidArgumentValue` (`TRE0026`, error) at the term's span and path, with the problem's expected/found text and its suggestion as a hint. All four front ends share `RuleNodeCompiler` (`RuleBuilder.Compile` goes through JSON), so they give the same result. `ScalarPredicateCore.Range` sets the validator, which covers numeric (`Int64`, `Decimal`) and date-time `Between`/`Outside`; it skips when either bound is absent, and suggests swapping. The evaluation-time `ArgumentException` is unchanged. Tests: `tests/TruthWeaver.Tests/PredicateCatalog/ReversedBoundsDiagnosticTests.cs`.
