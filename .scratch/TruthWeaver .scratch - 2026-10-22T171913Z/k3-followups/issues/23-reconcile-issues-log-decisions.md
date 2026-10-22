# 23: Reconcile the issues-log with the 2026-10-03 owner decisions

**What to build:** The k3-conformance issues-log records the owner's answers from the review: row 15 (`COALESCE` is the canonical form, `??` accepted as input) Kept; row 18 (ternary mixed with `AND`/`OR` stays an error) Kept; row 19 (`If` operand-count errors stay `MalformedTree`; `If` gets a `?:` C-style tree spelling) Resolved by ticket 21; row 27 (`RuleText.NormalizeWhitespace` stays in `TruthWeaver.Printing`; prefix `!` accepted with a space, printed without) Resolved by ticket 21. Also record the `EqualsConfigurable` `culture` removal (ticket 20) and the counted-operator overloads (ticket 22), and update the "Summary for review" so it lists only what is still open (the deferred predicate-catalog questions 2, 4, 7 and 8). Update ADR-0005 only if one of the decisions above is not already recorded there.

**Blocked by:** 20, 21, 22

**Status:** done

- [x] Rows 15, 18, 19 and 27 carry their final status and a pointer to the deciding ticket or ADR
- [x] The `culture` removal and the counted-operator overloads are recorded
- [x] The "Summary for review" lists only genuinely open questions
- [x] No decision is left unrecorded in either the issues-log or ADR-0005
- [x] `dotnet build` and `dotnet csharpier check .` pass

Source: owner decisions 2026-10-03. See [issues-log](../../k3-conformance/issues-log.md).

## Comments

- 2026-10-03 (implementation): rows 15, 19, 27 and the "Open decisions" entries were already reconciled by ticket 30. Added issues-log rows 42 (`culture` removal, ticket 20) and 43 (counted-operator overloads, ticket 22), recorded both in ADR-0005 "Open decisions" (they were not there), and rewrote the log summary to the genuinely open items: the deferred predicate-catalog questions 2, 4, 7, 8, plus row 18 (ternary wording), which stays open pending ticket 31 and so cannot be dropped from the list. Row 18 keeps status Open. Docs only; no source changed.
