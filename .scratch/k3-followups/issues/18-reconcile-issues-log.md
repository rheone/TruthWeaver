# 18: Reconcile the issues log

**What to build:** Update .scratch/k3-conformance/issues-log.md so every row reflects the final outcome (resolved, kept by decision, or still open) and refresh its 'Summary for review' block. Record that research-findings.md and spec-audit.md are the evidence. Close or re-open the follow-up spec.

**Blocked by:** 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15

**Status:** done

- [x] Every row has a current status
- [x] The summary block lists only genuinely open questions
- [x] The follow-up spec status is updated

Source: both reports. See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Every row of the k3-conformance issues log now has a Status column (Resolved, Kept, Open or Info). Rows 7, 8, 9, 10, 11, 26, 35 and 37 were updated for tickets 08, 09 and 16, and the stray extra cells of rows 22 and 23 were merged into the last cell so GitHub renders them. The summary block now cites research-findings.md and spec-audit.md as evidence and lists only the genuinely open questions: rows 15, 18, 19 and 27, plus predicate-catalog questions 2, 4, 7 and 8 (deferred).
- Tickets 08 and 09 were verified against their acceptance criteria (done in 58c5c2f and 00cfd18; only the log rows remained) and are marked done here.
- The follow-up spec is marked done; the remaining open questions are tracked in the issues log.
