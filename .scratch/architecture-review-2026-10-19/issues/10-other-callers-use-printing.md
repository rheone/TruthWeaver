# 10: CLI, export catalog, tree and report use the printing module

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 09
**Source:** architecture review of 2026-10-19, candidate 3

**What to build:** The CLI `format` and `convert` commands, `ArtifactCatalog`, `RuleTree` and `RuleReport` call the printing module. The two JSON indenting paths become one.

- [ ] CLI and workbench produce the same text for the same rule and options
- [ ] `ExportParityTests` pass
- [ ] The CLI reference page is updated if any output changes
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
