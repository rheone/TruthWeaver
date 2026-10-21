# 08: Inspect uses the palette module

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 06, 07
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** Inspect reads predicate facts from the palette module and no longer depends on the Structure controller.

- [ ] Inspect has no reference to the Structure controller
- [ ] ADR-0017 decision 4 matches the code
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

Amend ADR-0004 and correct ADR-0017 decision 4 in ticket 05 (see ticket 04 Answer).
