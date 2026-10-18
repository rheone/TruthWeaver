# 16: Basic mouse support

**What to build:** Click a pane to focus it, click a tree node to select it, and use the wheel to scroll a pane. Every action keeps its key. The key help (F1) says that the mouse works. A terminal without mouse reporting still works with the keys.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] A click focuses the pane under it, and a click on a tree node selects that node
- [ ] The wheel scrolls the focused or hovered pane
- [ ] No action is available only by mouse
- [ ] The page for this feature is updated in the same change (`docs/tui.md`, Keys), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
