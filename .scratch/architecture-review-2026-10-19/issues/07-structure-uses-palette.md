# 07: Structure controller uses the palette module

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 05
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** Expression picking and palette filtering in the Structure controller use the palette module. `FilterPalette`, the catalog search and the cached schema load are deleted.

- [ ] No palette filtering logic remains in the Structure controller
- [ ] Existing Structure palette tests pass
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
