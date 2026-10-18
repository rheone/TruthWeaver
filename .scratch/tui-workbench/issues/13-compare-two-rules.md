# 13: Compare two rules

**What to build:** Open a second rule beside the current one and show whether it is looser, tighter, equivalent or unrelated (the looser-or-tighter comparison of the library), plus the overlap result (`RuleOverlap.Compare`) with an example assignment where they differ. The second rule loads from a file or from the clipboard and is read-only.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] The pane shows the same relation and overlap as the library calls for the same two rules
- [ ] A second rule that does not compile shows its diagnostics and no comparison
- [ ] An example assignment from the pane can be loaded into the Assign pane
- [ ] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
