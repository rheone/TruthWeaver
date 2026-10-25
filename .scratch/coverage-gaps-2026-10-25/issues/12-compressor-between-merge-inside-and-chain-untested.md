# 12 - Compressor's BETWEEN-merge-within-a-larger-AND path has no test

Status: needs-triage
Severity: medium
Slice: Rewriting (src/TruthWeaver/Rewriting/Compressor.cs)
Lens: equivalence
Signal: line

## Gap

`Compressor.Pass`'s threshold-pair-to-`BETWEEN` merge (`src/TruthWeaver/Rewriting/Compressor.cs`, around
lines 178-199) combines an `AtLeast`/`AtMost` pair over the same operands into one `BetweenExpression`.
When the pair is the AND's only two operands (`ops.Count == 2`), it returns the `BETWEEN` node directly -
that path is covered. When the pair sits inside a *larger* AND chain (more than two operands total), the
method instead splices the merged `BETWEEN` node into a rebuilt `AndExpression` alongside the other,
untouched operands (lines 186-199) - that branch is uncovered. This is a different code path with its own
chance of dropping or duplicating an operand, and it is unverified.

## Missing scenario

A rule with `AND(AtLeast(2, a, b, c), AtMost(4, a, b, c), d)` (an `AtLeast`/`AtMost` pair over the same
operands, plus at least one unrelated operand in the same `AND`), asserting the compressed tree is
`AND(BETWEEN(2, 4, a, b, c), d)` (or the equivalent canonical shape) with the unrelated operand preserved
exactly once, and that it evaluates identically to the pre-compression tree on True/False/Unknown inputs.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `Compressor.cs:186-199`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
