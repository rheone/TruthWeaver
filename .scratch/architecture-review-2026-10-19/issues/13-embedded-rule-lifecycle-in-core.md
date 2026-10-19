# 13: Core owns the embedded-rule lifecycle

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 12
**Source:** architecture review of 2026-10-19, candidate 5

**What to build:** Core opens an embedded rule, edits it and writes it back safely, including the check that the file changed on disk. The logic that now sits in the terminal UI session moves to Core.

- [ ] The on-disk conflict case is tested without a Tui reference
- [ ] Patch mode choice and baseline reset are Core behavior
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
