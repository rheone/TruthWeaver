# 03 - Linter.LintInspection's Analyzer.Profile-null path and per-kind truth table are undertested

Status: needs-triage
Severity: high
Slice: Analysis (src/TruthWeaver/Analysis/Linter.cs)
Lens: boundary
Signal: line, branch

## Gap

`Linter.LintInspection` (`src/TruthWeaver/Analysis/Linter.cs:205-260`) is the `RedundantInspection` lint
rule. Two related gaps in the same method:

1. Line 209 (`return;` inside `if (Analyzer.Profile(...) is not { } profile) { return; }`, line 207) is
   uncovered: no test exercises `LintInspection` with an operand whose profile is unavailable (see ticket 02
   in this effort - the same `Analyzer.Profile` returning `null`), so the lint's own early-exit for that case
   is unverified.
2. The per-`InspectionKind` truth tables at lines 214-227 (`testedCanHold`/`otherCanHold`) are only 50%
   branch-covered at lines 219, 223, 224 and 225 - each is an `A || B` expression and only one of the two
   ways it can be `true` is exercised. `InspectionKind.IsKnown` (line 219's `_ =>` arm) is a real, reachable
   case (`Ast/Expression.cs:42`), not a defensive default - it is the fourth `InspectionKind` and the
   `RedundantInspection` lint is not proven correct for it in both directions.

## Missing scenario

(a) Lint a rule whose inspected operand exceeds `CompilerOptions.MaxAnalysisTerms` (so `Analyzer.Profile`
returns `null`), asserting no `RedundantInspection` finding is produced and no exception is thrown. (b) For
each of `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`, a pair of operand profiles that make the relevant
`||` true through its *other* operand than whatever the existing tests already cover (e.g. for `IsKnown`'s
`otherCanHold = profile.CanBeUnknown`, and `testedCanHold = profile.CanBeTrue || profile.CanBeFalse` with
only `CanBeTrue` true in existing coverage - add a case where only `CanBeFalse` is true).

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line/branch coverage signal clears for `Linter.cs:209` and the four listed branches
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
