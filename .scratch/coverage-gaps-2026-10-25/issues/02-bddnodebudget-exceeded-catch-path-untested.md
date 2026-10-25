# 02 - Analyzer.Profile and BddNodeCount's BddNodeBudgetExceededException catch path has no test

Status: needs-triage
Severity: critical
Slice: Analysis (src/TruthWeaver/Analysis/Analyzer.cs)
Lens: fault-path
Signal: line

## Gap

`Analyzer.Profile` (`src/TruthWeaver/Analysis/Analyzer.cs:205-219`) and `Analyzer.BddNodeCount`
(`:231-248`) each wrap their BDD build in a `try`/`catch (BddNodeBudgetExceededException) { return null; }`.
`BddNodeCount`'s own doc comment calls this "ticket 24" - the node-budget guard this very branch
(`ultra-review/crash-and-blowup-fixes`) just added to stop the analysis BDD readers from running unbounded
(ticket 24: "Give the analysis BDD readers a node budget"). Both catch blocks (lines 215-217 and 244-246)
are uncovered: nothing in the solution's test suite drives a tree through either method large/shaped enough
to actually exceed `BddManager.DefaultMaxBddNodes` and hit the catch, rather than the term-count short-circuit
a few lines above it (`DistinctTerms(root).Count > maxTerms`, which is covered).

A guard added specifically to prevent unbounded blowup, with no regression test proving it actually catches
the exception and returns `null` instead of letting the exception (or an unbounded build) propagate, is a
critical gap for this exact branch's own purpose.

## Missing scenario

A rule shaped to build a dual-rail BDD that exceeds `BddManager.DefaultMaxBddNodes` while staying under
`maxTerms` (so it reaches the BDD build, not the earlier `DistinctTerms` short-circuit) for both
`Analyzer.Profile` and `Analyzer.BddNodeCount`, asserting each returns `null` rather than throwing.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `Analyzer.cs:215-217` and `:244-246`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
