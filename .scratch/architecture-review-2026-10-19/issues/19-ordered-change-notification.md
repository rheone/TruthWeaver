# 19: One ordered document-or-assignment change notification

**Status:** done
**Type:** task
**Blocked by:** 03, 08
**Source:** architecture review of 2026-10-19, candidate 8

**What to build:** The window reacts to one "document or assignment changed" notification with a defined order, replacing the handler-order contract described in a code comment.

- [x] The comment-based ordering note is deleted
- [x] A test fails if the reset of the outcome runs after the window refresh
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

`Workbench.DocumentOrAssignmentChanged` is the one notification. The workbench drops the stale outcome first (`RaiseDocumentChanged`, `NotifyAssignmentChanged`), then raises it. The window refresh (status line, inspect box), `TreePane`, `ViewPane`, `AssignmentView`, `CounterfactualPane` and `EvaluationStepController` subscribe to it instead of to both events. The lazy "first in line" outcome reset and the "read Assignment first" comments are gone. `DocumentChanged` and `AssignmentChanged` stay for subscribers that need only one of them. Two tests in `WorkbenchTests` fail when the reset runs after the event (shown with a deliberately wrong order). No observable behavior change, so no docs or CHANGELOG entry.

Noticed, not changed: `docs/tui.md` (extension section) still names only `Workbench.DocumentChanged` for panes.
