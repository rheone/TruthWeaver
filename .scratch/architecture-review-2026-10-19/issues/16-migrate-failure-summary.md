# 16: Controllers use the failure summary

**Status:** done
**Type:** task
**Blocked by:** 15
**Source:** architecture review of 2026-10-19, candidate 6

**What to build:** Migrate the 12+ hand-rolled first-error formatters to the shared summary, in two batches by directory (controllers and commands first, then workbench, sessions and hosting). Keep the verb prefixes ("Not applied:", "Not saved:", "No image:").

- [x] No `FirstOrDefault` on error severity remains for status wording
- [x] Status messages are unchanged except for fixed wording drift, which the PR lists
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

Added `DiagnosticSummary.FirstError(diagnostics, fallback)` (public, `Tool.Core.Configuration`) for callers that hold only diagnostics; `ToolResult.FailureSummary` calls it. Batch 1 (controllers, commands) and batch 2 (workbench, sessions) now use one of the two. Verb prefixes are unchanged.

Wording drift fixed:
- Rule test session (`RuleTestSession`) errors showed the message only; they now show `code: message` like every other status line.
- `RequireCurrentRule` ("Cannot ...: the rule text has errors") showed `(code)`; it now shows `(code: message)`.

Left alone: `TuiStartup.Describe` (all errors, multi-line, for process exit text) and `RuleTreeEditor.FirstError` (falls back to the first diagnostic of any severity, in the core, for a failure message). Neither is a first-error status formatter; `DiagnosticSummary` holds the only `FirstOrDefault` on error severity besides `RuleTreeEditor`.
