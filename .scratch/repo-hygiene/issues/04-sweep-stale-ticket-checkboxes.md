# 04: Sweep stale ticket checkboxes

**What to build:** Every ticket marked `done` has its checklist match reality, so the tracker can be trusted.

**Blocked by:** None (can start immediately)

**Status:** ready

- [x] A script in `scripts/` lists tickets with `Status: done` and at least one `- [ ]` item, excluding `engine-v1` and `_superseded` Script: `scripts/list-open-done-tickets.ps1`.
- [x] For each listed item, check the evidence (code, tests, commit) and tick it, or write a one-line note on why it stays open
- [x] The most common item is the closing "XML docs, tests named per CLAUDE.md, full validation set" line; tick it only after confirming the ticket's commit passed the pre-commit hook Rule applied: ticked only where CI is green on the branch head and the feature commit exists; the pre-commit hook pass cannot be proven from history, so those lines carry a note instead.
- [x] No ticket status changes without the owner's agreement
- [ ] Re-running the script lists only items with a written note Note 2026-10-10: the only unnoted items left belong to tickets 01 and 02 of this effort, which are in flight; run `pwsh scripts/list-open-done-tickets.ps1` again after they land.

See also [spec](../spec.md).
