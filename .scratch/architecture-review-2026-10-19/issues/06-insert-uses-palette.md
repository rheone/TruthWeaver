# 06: Insert commands use the palette module

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 05
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** The Insert menu and predicate chooser read from the palette module. `FilterPredicates` and the per-call schema reload are deleted.

- [ ] No palette filtering logic remains in the Insert commands
- [ ] Existing Insert and catalog palette tests pass
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
