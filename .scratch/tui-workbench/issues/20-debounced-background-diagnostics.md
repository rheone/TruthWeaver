# 20: Debounced, background diagnostics

**What to build:** First check what the workbench does today when the text changes: whether it debounces and whether it compiles on the UI thread. Then make live diagnostics compile after a short pause, on a background task, and drop a result when newer text exists. A status mark shows "checking" while a compile runs. Add a test with a generated rule of thousands of nodes.

**Blocked by:** none

**Status:** done

- [x] The ticket records what the workbench did before the change
- [x] Typing in a large rule never blocks the editor on a compile
- [x] A stale compile result never replaces a newer one
- [x] The status line shows "checking" while a compile runs
- [x] The page for this feature is updated if the observable behavior changes (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- Before the change: no debounce. `RuleTextPane` called `RuleDocument.SetText` on every `ContentsChanged`, and `RuleDocument` compiled the text on the calling (UI) thread in the same call.
- Design: `RuleDocument.Check(text, format)` compiles without touching the document (the compiler keeps no state, so any thread may call it) and `SetText(CheckedText)` applies the result without a second compile. `Workbench.EditText` runs the 300 ms wait and the compile on a background task, then applies the result through `Workbench.Post` (`app.Invoke` in `Program`). A version counter drops a result when newer text or another document change exists.
- `Workbench.DiagnosticsDelay` defaults to zero (compile at once, same thread), so the existing tests and any non-terminal caller behave as before. Only `Program` sets 300 ms.
- Choice: reading `Workbench.Document` applies the pending text first, so save, format, undo, export and structural edits never act on replaced text. `Workbench.CommittedDocument` reads without applying; the status line uses it, and it shows `checking...`.
- Choice: typing between two pauses is one undo step (was one step per key). Recorded in `docs/tui.md`.
- Tests: `DebouncedDiagnosticsTests` (includes a 3000-term rule) and `RuleDocumentCheckTests`.
