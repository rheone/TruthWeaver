# 04 - Close the quantifier combinatorial-sweep gap

Status: ready-for-agent
Category: enhancement
Severity: high
Slice: Quantifiers (FORALL, EXISTS)
Blocked by: none

## What to build

A genuinely exhaustive combinatorial proof that FORALL/EXISTS evaluate correctly when combined with every other operator family, matching the rigor every other operator family already has — not the current representative-sample coverage.

## Gap

`QuantifierLayerAgreementTests.cs` checks FORALL/EXISTS against the oracle (expressed as `K3Oracle.And`/`K3Oracle.Or` over a collection's element values) through a fixed list of composed expressions: a quantifier combined with AND, OR, XOR, IMPLIES, IF, ANY, a threshold operator, COALESCE, and NAND. This is the one operator family in scope whose test is a representative list rather than a true combinatorial sweep over quantifier kind x outer operator x element count, unlike `K3ConformanceTests`'s arity sweep or `AnyAllNoneTests`'/`BetweenTests`'s own `Assignments(arity)` loops.

## Missing scenario

For a small element-count range (e.g. 0-3 elements, matching the arity range the other exhaustive tests use), sweep every combination of {FORALL, EXISTS} x {every element-value assignment in `{True, False, Unknown}`} x {the outer operators the existing fixed list already covers, plus any core connective it's missing} and confirm the quantifier's evaluated value, folded into the outer expression, agrees with the oracle at every point. Pay particular attention to the zero-element case (FORALL over an empty collection is vacuously True; EXISTS over an empty collection is vacuously False) and to a mix of True/False/Unknown elements that could expose an off-by-one in how the quantifier folds its elements.

## Acceptance criteria

- [ ] A combinatorial sweep (not a fixed representative list) exhaustively proves FORALL/EXISTS correctness at small element counts (0-3), including the zero-element vacuous-truth case, against the independent oracle.
- [ ] The sweep covers every outer-operator combination the existing fixed list covered, at minimum.
- [ ] Any defect the sweep surfaces is fixed and reported as its own finding (critical/high per this effort's severity floor), not silently folded into this ticket's own scope.
- [ ] The validation list in CLAUDE.md passes.
