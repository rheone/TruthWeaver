# 01: List dialogs act only on a real row

**What to build:** A user picks from a list dialog (Choose, and the filtered palette and catalog dialogs) with the mouse or Enter, and the dialog only ends when a real entry is chosen. A double-click on blank space below the rows, on a group header line such as `--- Numeric ---`, or on an empty filter result no longer closes the dialog with no result. Enter on a group header does nothing, so the dialog does not close and reopen with the search text and scroll position lost. A double-click on a real row still acts on it.

**Decisions (settled by grilling):**
- A double-click on a real row chooses it and closes the dialog, as Enter does. A double-click on blank space does nothing.
- Enter on a group header moves the highlight to the first entry of the group; the dialog stays open and keeps the search text and the scroll position.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A double-click on blank space in a list dialog does not close it and returns no result.
- [ ] A double-click or Enter on a group header line leaves the dialog open with the search text and the scroll position kept.
- [ ] A double-click on a real row, and Enter, choose that row, in both the plain and the filtered dialog.
- [ ] Tests cover blank space, a header, a real row and an empty filter result (a test dialog harness may be needed, since the dialogs need an application).
- [ ] The callers that reopen the dialog for a header no longer need the loop, or keep it only as a guard.
- [ ] `docs/tui.md` describes the current dialog mouse behavior, with no "changed from" text.
