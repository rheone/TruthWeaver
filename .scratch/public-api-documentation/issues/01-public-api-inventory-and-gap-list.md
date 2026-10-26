# 01: Inventory the public API and list documentation gaps

**What to build:** Produce a list of every public type and member in the seven non-test packages (use Roslyn or reflection over the built assemblies, not memory), and for each say which markdown page covers it, or that none does. Also list pages that are stale, duplicated, orphaned (not linked) or in the wrong place. Write the result as a table per package in the ticket folder. No documentation is edited.

**Blocked by:** None (can start immediately)

**Status:** needs-triage (do not start or dispatch until the owner approves explicitly)

- [ ] A per-package table lists every public type with its covering page or `none`
- [ ] Stale, duplicated and orphaned pages are listed
- [ ] The method used is reproducible (a script in `scripts/` or the commands in the table's header)
- [ ] Nothing outside `.scratch/` and `scripts/` changed
- [ ] The full validation set from `CLAUDE.md` passes
