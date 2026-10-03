# 30: Reconcile ADR-0005 "Open decisions: None" with the issues log

**What to build:** ADR-0005 states "Open decisions: None", while the k3-conformance issues log still lists rows 15, 18, 19 and 27 as Open (COALESCE chains and spelling, ternary mixing, `If` arity errors, `NormalizeWhitespace`). Make the two agree: either each open row is decided and recorded in the ADR, or the ADR lists them as open. Overlaps [23](23-reconcile-issues-log-decisions.md); check that ticket first and fold this in rather than duplicate work if 23 already covers these rows.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Ticket 23's scope is compared against rows 15, 18, 19 and 27 and any overlap is noted
- [ ] The ADR and the issues log give the same status for every row
- [ ] Any row decided here is recorded with its rationale in the ADR

Source: review of PR #4, Spec axis, missing or partial item on ADR-0005.

## Comments

- 2026-10-03 (implementation): ticket 23 covers rows 15, 18, 19 and 27 too (it is still ready-for-agent), so this was folded in rather than duplicated: only those four rows and the log's Summary-for-review item 1 were touched, and 23's remaining items (culture removal, counted overloads, row 18 note, the deferred-catalog summary) are left for 23. Rows 15 (Kept), 19 and 27 (Resolved by k3-followups 21) now carry their owner answers, and ADR-0005 "Open decisions" records each with rationale. Row 18 is not decided: the owner kept the strict behaviour but decision 13 still says the ternary is "the lowest-precedence construct", so the ADR lists it as open and the log keeps it Open, both pointing to ticket 31 (needs-owner-decision, untouched). Verified against `RuleText.cs`, `WhitespaceNormalisationTests` and decision 13. Docs only; no source changed.
