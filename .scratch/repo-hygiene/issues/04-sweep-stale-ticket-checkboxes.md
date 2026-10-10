# 04: Sweep stale ticket checkboxes

**What to build:** Every ticket marked `done` has its checklist match reality, so the tracker can be trusted.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] A script in `scripts/` lists tickets with `Status: done` and at least one `- [ ]` item, excluding `engine-v1` and `_superseded`
- [ ] For each listed item, check the evidence (code, tests, commit) and tick it, or write a one-line note on why it stays open
- [ ] The most common item is the closing "XML docs, tests named per CLAUDE.md, full validation set" line; tick it only after confirming the ticket's commit passed the pre-commit hook
- [ ] No ticket status changes without the owner's agreement
- [ ] Re-running the script lists only items with a written note

See also [spec](../spec.md).
