# 13: Core owns the embedded-rule lifecycle

**Status:** done
**Type:** task
**Blocked by:** 12
**Source:** architecture review of 2026-10-19, candidate 5

**What to build:** Core opens an embedded rule, edits it and writes it back safely, including the check that the file changed on disk. The logic that now sits in the terminal UI session moves to Core.

- [x] The on-disk conflict case is tested without a Tui reference
- [x] Patch mode choice and baseline reset are Core behavior
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

**Decision from ticket 12:** move the session only; keep the outline, source index, reader and patcher as separate modules; no CLI patch option.

## Result

`EmbeddedRuleSession` moved from the Tui project to `TruthWeaver.Tool.Core.Patching` as a public type with XML docs (`Results` became `ToolResults`). Four new tests in `EmbeddedRuleSessionTests` cover the on-disk conflict, no conflict, baseline reset after `Commit`, and round-trip mode, with no Tui reference. Patch mode choice and baseline reset were already inside the session, so they are now Core behavior. `docs/tool-core.md` has an "Editing an embedded rule" section. No observable behavior change, so no CHANGELOG entry.
