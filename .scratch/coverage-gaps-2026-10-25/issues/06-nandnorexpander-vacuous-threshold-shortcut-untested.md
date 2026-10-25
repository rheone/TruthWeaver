# 06 - NandNorExpander.ConvertThreshold's vacuous-constant-threshold shortcut has no test

Status: needs-triage
Severity: medium
Slice: Rewriting (src/TruthWeaver/Rewriting/NandNorExpander.cs)
Lens: boundary
Signal: line

## Gap

`NandNorExpander.ConvertThreshold` (`src/TruthWeaver/Rewriting/NandNorExpander.cs:204-221`) checks
`ThresholdSemantics.Constant(t.Comparison, t.K, operands.Count)` before attempting the full NAND/NOR
expansion: a threshold whose outcome the operand count alone already fixes (e.g. `AtLeast(0, ...)` or
`AtLeast(n+1, ...)` for `n` operands) short-circuits straight to a `ConstantExpression`, line 209. That
`return` is uncovered - nothing drives the actual NAND/NOR conversion path (`NandNorExpander.Rewrite`, not
the `Linter`'s separate `VacuousCardinality` lint or `Canonicalizer`) with a threshold that is vacuously
constant by operand count alone.

## Missing scenario

A rule using a NAND-only or NOR-only expression family (so `NandNorExpander`'s conversion path actually
runs) whose threshold is vacuously constant by operand count (e.g. `AtLeast(0, a, b)` or `AtMost(2, a, b)`
with 2 operands), asserting the NAND/NOR-rewritten tree evaluates to the same constant without going
through `ThresholdExpansion.Expand`.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `NandNorExpander.cs:209`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
