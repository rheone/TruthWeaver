# 17: Color-independent value marks

**What to build:** Every mark of True, False and Unknown in the workbench (`TruthColors`, the tree, the decision line, the results) also shows a letter or symbol, so no value depends on color alone. The workbench honors `NO_COLOR` by drawing without color. A test checks that no value mark is color-only.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] Each place that colors a truth value also writes T, F or U (or the symbol the page lists)
- [ ] With `NO_COLOR` set, the workbench uses no color and every value is still readable
- [ ] A test fails when a value mark has a color and no text mark
- [ ] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
