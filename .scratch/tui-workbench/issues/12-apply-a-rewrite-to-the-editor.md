# 12: Apply a rewrite to the editor

**What to build:** Ctrl+R shows a rewrite in the Rewrite pane and leaves the editor unchanged. Add an accept key that writes the rewritten rule into the editor as one undoable edit, in the format of the open document. A rewrite that would not compile is refused with the diagnostics. The `x` key still discards the result.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] Accept replaces the editor text with the rewrite and one undo restores the earlier text and tree exactly
- [ ] The rule keeps its notes where the rewrite allows it, and the status line says when a note is dropped
- [ ] The page for this feature is updated in the same change (`docs/tui.md`, Rewrites and compare loses the sentence that says the editor does not change yet), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
