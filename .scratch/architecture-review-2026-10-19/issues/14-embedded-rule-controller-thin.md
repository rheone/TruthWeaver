# 14: Embedded-rule controller keeps only prompts and wording

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 13
**Source:** architecture review of 2026-10-19, candidate 5

**What to build:** The terminal UI controller keeps prompts and status text and delegates the lifecycle to Core.

- [ ] The session class in the Tui project is deleted or only adapts
- [ ] Existing embedded-rule Workbench tests pass
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
