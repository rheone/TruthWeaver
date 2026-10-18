# 12: Apply a rewrite to the editor

**What to build:** Ctrl+R shows a rewrite in the Rewrite pane and leaves the editor unchanged. Add an accept key that writes the rewritten rule into the editor as one undoable edit, in the format of the open document. A rewrite that would not compile is refused with the diagnostics. The `x` key still discards the result.

**Blocked by:** none

**Status:** in progress (only the full validation run is open)

- [x] Accept replaces the editor text with the rewrite and one undo restores the earlier text and tree exactly
- [x] The rule keeps its notes where the rewrite allows it, and the status line says when a note is dropped
- [x] The page for this feature is updated in the same change (`docs/tui.md`, Rewrites and compare loses the sentence that says the editor does not change yet), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- Accept (key `a` in the Rewrite pane) and discard (`x`) already existed; this ticket added the notes handling. `RewritePlayground.CarryNotes` (new `NotedRewrite` record in Tool.Core) puts back the notes of the rule before the rewrite. Conservative rule: a note stays only when the node at its path has an identical subtree after the rewrite; every other note is dropped and counted, and the status line says how many.
- A rewrite that does not compile when printed is refused by `RuleDocument.ReplaceRule` with the first diagnostics on the status line (existing behavior, now stated in `docs/tui.md`).
- Pages updated: `docs/tui.md`, `docs/tool-core.md`. Tests: `RewriteControllerTests`, `RewritePlaygroundTests`.
- Full validation set of CLAUDE.md was not run; checked the Tui and Tool.Core rewrite tests, the doc lint tests and `dotnet format` on the changed files.
