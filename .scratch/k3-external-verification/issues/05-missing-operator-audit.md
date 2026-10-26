# 05 - Formalize the missing-operator audit

Status: ready-for-agent
Category: enhancement
Severity: high
Slice: Whole operator set (sanity re-check against Strong Kleene literature)
Blocked by: none

## What to build

A durable, inline-commented record of the missing-operator sanity check already performed during this effort's scoping, so the "nothing is missing" conclusion and its reasoning survive outside this conversation.

## Gap

The grilling session that scoped this effort already established the answer: comparing TruthWeaver's operator set against Strong Kleene literature finds nothing missing. Only NOT/AND/OR (`¬`/`∧`/`∨`) are externally attested as K3's own named connectives; every other operator in the implementation — XOR, EQUIVALENT, IMPLIES, NAND, NOR, PARITY, the full cardinality/threshold family, If, COALESCE, the four inspections, and the quantifiers — is the library's own derived/named convenience with no independent external name, verified by derivation rather than citation (see tickets 01-04). This was explicitly scoped as a Strong-Kleene-only sanity re-check of ADR-0005's already-closed operator-set decision, not a comparison against adjacent three-valued systems (Łukasiewicz, Bochvar, Priest's LP) and not expected to reopen that decision. Nothing currently records this conclusion anywhere a future reader (or a future review) would find it without re-deriving it.

## Missing scenario

Not applicable in the usual sense — this ticket's job is to record a conclusion already reached, not to find a new defect. Record it as a comment near `OperatorDefinitions.cs` (or wherever the operator-set enumeration most naturally lives) stating: the operator set was checked against Strong Kleene literature during the 2026-10 K3 external-verification effort; only NOT/AND/OR are literature-attested; everything else is a derived convenience (cross-reference tickets 01-04 for the per-family derivation arguments); the check was Strong-Kleene-only by design, not a comparison against Łukasiewicz/Bochvar/Priest's LP, per ADR-0005's existing scope.

## Acceptance criteria

- [ ] A comment records the missing-operator audit's conclusion, its scope (Strong Kleene only), and a cross-reference to tickets 01-04.
- [ ] If this ticket's author, while writing the comment, finds an actual gap the grilling session missed, it's reported as its own finding (critical/high per this effort's severity floor) rather than silently folded in here.
- [ ] No `docs/strong-k3/*.md` page is changed unless this audit finds one factually wrong.
- [ ] The validation list in CLAUDE.md passes.
