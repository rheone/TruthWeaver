# 05 - NandNorExpander's SubsetCost resource-bailout path has no test

Status: needs-triage
Severity: high
Slice: Rewriting (src/TruthWeaver/Rewriting/NandNorExpander.cs)
Lens: boundary
Signal: line, branch

## Gap

`NandNorExpander.Rewrite` (`src/TruthWeaver/Rewriting/NandNorExpander.cs:60-80`) has two independent
size bailouts before it attempts the actual NAND/NOR conversion: the `ExpressionTools.Size(primitive) >
maxNodes` check (line 66-69, covered) and the `SubsetCost(primitive, maxNodes, ...) > maxNodes` check
(line 72-74, uncovered - `return null;` at line 74 never executes). `SubsetCost` is a lower-bound estimate
of how many nodes the threshold expansions alone will add, specifically so a tree whose primitive form is
small but whose threshold expansions would blow it up gets rejected before the (expensive) actual
conversion runs - the same family of concern as tickets 04/05/06 on this branch (expansion-driven blowup).
Nothing currently drives a tree through this second, cost-based bailout rather than the simpler size-based
one. Separately, `ThresholdCost`'s `Exactly` case (line 112-115) has two independent guard conditions
(`t.K >= 1`, `t.K <= n - 1`) and branch 108 is only 50% (3/6) covered - not every combination of a `K` at
the low boundary (`K == 0`) and the high boundary (`K == n`) is exercised together.

## Missing scenario

A `ThresholdExpression` (or nested tree of them) whose `ExpressionTools.Size` is under `maxNodes` but whose
`AtLeast`/`AtMost`/`Exactly` operand counts make `SubsetCost`'s estimate exceed `maxNodes`, asserting
`NandNorExpander.Rewrite` (via the public NAND/NOR rewrite entry point) returns `null` rather than
attempting the conversion. Separately, an `Exactly(k, ...)` threshold with `k == 0` and one with `k == n`
(the two boundary values that make one side of the `Exactly` cost sum vacuous).

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line/branch coverage signal clears for `NandNorExpander.cs:72-74` and branch `108`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
