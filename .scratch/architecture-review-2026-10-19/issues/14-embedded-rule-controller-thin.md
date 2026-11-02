# 14: Embedded-rule controller keeps only prompts and wording

**Status:** done
**Type:** task
**Blocked by:** 13
**Source:** architecture review of 2026-10-19, candidate 5

**What to build:** The terminal UI controller keeps prompts and status text and delegates the lifecycle to Core.

- [x] The session class in the Tui project is deleted or only adapts
- [x] Existing embedded-rule Workbench tests pass
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

Done by the move in ticket 13: the Tui `Sessions/EmbeddedRuleSession.cs` is deleted, and `EmbeddedRuleController` calls the Core `EmbeddedRuleSession` directly. The controller keeps the prompts, the confirmation wording (`Summary`, `KindText`) and the status text; the open, prepare, commit and baseline logic is in Core. The 22 `EmbeddedRule` tests in `TruthWeaver.Tool.Tui.Tests` pass unchanged. No observable behavior change, so no CHANGELOG entry.
