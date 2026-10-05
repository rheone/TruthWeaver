# 12: Compile-time diagnostic for reversed literal bounds

**What to build:** Catalog rule 7 (`CONTEXT.md`) says reversed bounds (`lower > upper`) on `Between` and `Outside` (numeric and date-time) are a compile-time diagnostic when both bounds are literals, and are never swapped. Today `PredicateSchema` and the compiler have no argument-validation hook, so the predicates throw `ArgumentException` at evaluation. The engine records a `Fault` and answers `Unknown`. Add the compile-time diagnostic for literal bounds and keep the evaluation-time `ArgumentException` for bounds that are not literals.

This needs an owner design decision first. Questions to settle:

1. The hook: an optional validator on `PredicateSchema` (a delegate over the literal arguments that returns problems), or a declared relation between two arguments (for example "lower <= upper"), or a hard-coded check for the `Between` and `Outside` names.
2. The diagnostic: its code (a new `DiagnosticCode`) and its severity (error, so `Compile` returns no rule).
3. Where the validation runs: in the Validate stage of the pipeline (ADR-0003), after argument kinds are checked and before Analyze.
4. Whether `DataSources` variable arguments and other non-literal bounds are skipped (recommended: skipped).

Recommendation: an optional validator delegate on the schema, run in the Validate stage, one new error code, non-literal bounds skipped.

**Blocked by:** owner decision on the four questions above

**Status:** needs-owner-action

- [ ] The owner records the decision on the hook, the code and the stage in the ticket comments
- [ ] Tests first: reversed literal bounds on numeric `Between`, numeric `Outside`, date-time `Between` and date-time `Outside` each give the diagnostic with the position of the predicate call, and `Compile` returns no rule
- [ ] Equal bounds and ordered bounds compile
- [ ] A reversed bound that is not a literal still faults at evaluation (`Unknown` plus a `Fault`) and is never swapped
- [ ] `docs/predicates.md`, the diagnostics reference and the gap list describe the diagnostic
- [ ] The full validation from CLAUDE.md passes

Source: [predicate-catalog ticket 04](04-scalar-and-numeric-comparison.md) and [ticket 06](06-date-time-comparison.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments
