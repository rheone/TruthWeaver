# 11: The export catalog states each artifact group

**Status:** ready-for-agent
**Type:** task
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 4

**What to build:** Each artifact definition carries its group, and Core gives one answer for which image renderers are available. The export controller stops guessing groups from id prefixes and stops computing the missing image program twice.

- [ ] `GroupOf` and the id-prefix switch are deleted
- [ ] A test fails if any catalog entry has no group
- [ ] A new artifact needs a change in one place only
- [ ] Export status messages are unchanged
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
