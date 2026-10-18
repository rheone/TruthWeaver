# 20: Debounced, background diagnostics

**What to build:** First check what the workbench does today when the text changes: whether it debounces and whether it compiles on the UI thread. Then make live diagnostics compile after a short pause, on a background task, and drop a result when newer text exists. A status mark shows "checking" while a compile runs. Add a test with a generated rule of thousands of nodes.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] The ticket records what the workbench did before the change
- [ ] Typing in a large rule never blocks the editor on a compile
- [ ] A stale compile result never replaces a newer one
- [ ] The status line shows "checking" while a compile runs
- [ ] The page for this feature is updated if the observable behavior changes (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
