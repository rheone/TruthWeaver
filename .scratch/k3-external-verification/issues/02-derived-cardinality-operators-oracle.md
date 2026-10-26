# 02 - Confirm derived/cardinality operator and If oracle exhaustiveness

Status: ready-for-agent
Category: enhancement
Severity: high
Slice: Derived/cardinality operators (AtLeast, AtMost, Exactly, ExactlyOne, GreaterThan, LessThan, Any, All, None, Between) and If
Blocked by: none

## What to build

Confirmed proof that every derived/cardinality operator and `If` is genuinely exhaustively tested against the independent `K3Oracle`, across every tested arity and threshold value, not just covered by a representative sample.

## Gap

`K3ConformanceTests.cs` drives the threshold family (AtLeast/AtMost/GreaterThan/LessThan/Exactly) and ExactlyOne against `K3Oracle.Cardinality` across arities 2-4 and every legal `k`; `AnyAllNoneTests.cs` and `BetweenTests.cs` run their own exhaustive `Assignments(arity)`-style loops against the oracle; `IfTests.cs` does the same for `If`'s three operands. This audit has not yet independently re-confirmed that each of these loops is actually exhaustive (covers every value combination at its stated arity range, every boundary `k`) rather than a near-exhaustive sample that happens to pass.

## Missing scenario

For each of AtLeast, AtMost, Exactly, ExactlyOne, GreaterThan, LessThan, Any, All, None, Between, and If: re-derive what "exhaustive" means for that operator's arity and threshold range, and confirm the existing test's loop bounds actually cover it — including the boundary values most likely to hide a defect (`k == 0`, `k == n`, the smallest and largest tested arity). If a boundary or combination is missing, add it.

## Acceptance criteria

- [ ] Each listed operator's oracle-backed coverage is confirmed to include every value combination at its tested arity range and every boundary threshold value.
- [ ] Any coverage gap found is closed with a test following this repo's naming/structure conventions.
- [ ] No `docs/strong-k3/*.md` page is changed unless this audit finds one factually wrong.
- [ ] The validation list in CLAUDE.md passes.
