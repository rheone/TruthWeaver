# 30: Reconcile ADR-0005 "Open decisions: None" with the issues log

**What to build:** ADR-0005 states "Open decisions: None", while the k3-conformance issues log still lists rows 15, 18, 19 and 27 as Open (COALESCE chains and spelling, ternary mixing, `If` arity errors, `NormalizeWhitespace`). Make the two agree: either each open row is decided and recorded in the ADR, or the ADR lists them as open. Overlaps [23](23-reconcile-issues-log-decisions.md); check that ticket first and fold this in rather than duplicate work if 23 already covers these rows.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Ticket 23's scope is compared against rows 15, 18, 19 and 27 and any overlap is noted
- [ ] The ADR and the issues log give the same status for every row
- [ ] Any row decided here is recorded with its rationale in the ADR

Source: review of PR #4, Spec axis, missing or partial item on ADR-0005.
