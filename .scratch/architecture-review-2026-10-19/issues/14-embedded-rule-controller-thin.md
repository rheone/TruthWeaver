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

## Review notes (code review of tickets 13 and 14, 2026-11-02; recorded, not acted on)

Judgement calls from the review. The owner decided that hard findings are fixed and the rest is recorded here.

- `EmbeddedRuleSession.Label` and the "It has no view" wording describe a status-line concern inside a public Core type.
- `Commit` re-reads the file for the new baseline and keeps the old baseline when the re-read fails; the `<returns>` text does not say so.
- `ChangedOnDisk` compares the text of the rule only, not the whole file, so a change elsewhere in the file is not a conflict. This was the behavior before the move, and no test covers it.
- Tests do not cover `Open` failure paths (no path, not a rule), `Outline` or `CreateDocument`; some `Prepare` tests use `Value!` without asserting `Succeeded` first.
- `Commit` is called inside `Assert.True` in `Commit_AfterWrite_ResetsTheBaseline_Test`.
