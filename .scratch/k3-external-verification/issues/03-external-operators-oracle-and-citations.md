# 03 - Confirm external-operator oracle exhaustiveness and cite COALESCE correctly

Status: ready-for-agent
Category: enhancement
Severity: high
Slice: External (non-monotone) operators: COALESCE, IsTrue, IsFalse, IsUnknown, IsKnown
Blocked by: none

## What to build

Confirmed proof that COALESCE and the four inspections are genuinely exhaustively tested against the independent `K3Oracle`, with a correct, honest record of what externally backs COALESCE's behavior (since no logic-literature source treats it as a connective at all).

## Gap

`CoalesceTests.cs` runs an exhaustive `Assignments(arity)`-style loop against `K3Oracle.Coalesce`, and `K3LogicTests.cs` covers the four inspections scalar-wise over all three values. This audit has not yet independently re-confirmed these loops are genuinely exhaustive at their tested arities. Separately, nothing in the codebase currently records that COALESCE's correctness rests on a different kind of argument than the K3 connectives: no three-valued-logic source treats COALESCE as a connective. It traces to Codd's 1979 paper (the origin of SQL's NULL-propagation semantics) as a programming/SQL idiom, and its empty-input identity (`Unknown`) is proven by the general monoid-fold theorem (an associative operation with a two-sided identity must fold an empty input to that identity — see `ultra-review-2026-10-21` ticket 19 for the same argument already applied to `RuleBuilder.Coalesce`'s builder-time folding).

## Missing scenario

1. Confirm `CoalesceTests.cs` and `K3LogicTests.cs`'s inspection coverage are exhaustive at their tested arities; extend if a gap is found.
2. Add a comment on COALESCE's test/oracle code stating its citation is Codd (1979, SQL NULL origin) plus the monoid-identity argument (Haskell's `Data.Monoid.First`, `mconcat [] == mempty`), explicitly *not* K3 logic literature — distinguishing it from the core connectives' citation story in ticket 01.

## Acceptance criteria

- [ ] COALESCE's and the four inspections' oracle-backed coverage is confirmed exhaustive at every tested arity/value.
- [ ] Any coverage gap found is closed.
- [ ] A code comment records COALESCE's correct citation (Codd 1979 + the monoid-identity argument), distinct from the K3-connective citation story.
- [ ] No `docs/strong-k3/*.md` page is changed unless this audit finds one factually wrong.
- [ ] The validation list in CLAUDE.md passes.
