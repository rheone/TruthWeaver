# 05: One palette module with one search

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 04
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** A single palette module holds operators, constants, schema and rule predicates, definitions and the ready-made catalog. It offers "entries matching this text, grouped" and "facts about this predicate name". One search rule replaces the two near-copies.

- [ ] Own entries and catalog entries match by one rule
- [ ] The schema file is loaded once and cached in one place
- [ ] Search behavior is tested without a dialog or Workbench
- [ ] The terminal UI page (`docs/tui.md`) is updated if palette behavior it describes changes
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
