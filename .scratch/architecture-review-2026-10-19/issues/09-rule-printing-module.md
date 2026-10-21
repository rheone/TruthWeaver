# 09: One Core module prints a rule in a format

**Status:** done
**Type:** task
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 3

**What to build:** One Core module prints a rule, or a rule with definitions, in a format with style options (notes, operator style, pretty width, line endings, indented JSON). `RuleDocument` delegates to it and keeps only history and snapshots. The pretty width stops living in the export catalog.

- [ ] `RuleDocument` Print and Convert contain no print policy
- [ ] Print policy is tested in one place
- [ ] Output of the workbench Format and Convert is unchanged
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
