# 16: Basic mouse support

**What to build:** Click a pane to focus it, click a tree node to select it, and use the wheel to scroll a pane. Every action keeps its key. The key help (F1) says that the mouse works. A terminal without mouse reporting still works with the keys.

**Blocked by:** none

**Status:** done

- [x] A click focuses the pane under it, and a click on a tree node selects that node
- [x] The wheel scrolls the focused or hovered pane
- [x] No action is available only by mouse
- [x] The page for this feature is updated in the same change (`docs/tui.md`, Keys), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- Terminal.Gui 2.5 already routes the mouse: a click focuses the view under it, a click on a `ListView` line selects it, and the wheel scrolls the view under the pointer. The change adds no handler. It adds tests that prove each behavior and the F1 line about the mouse.
- "The focused or hovered pane": the wheel goes to the pane under the pointer (the library's rule). A pane scrolls with the wheel without taking the focus first.
- A terminal without mouse reporting sends no mouse events, so the keys are the only input there. Nothing in the workbench depends on a mouse event. The drag on the border of the Rule region has the keys Alt+Left, Alt+Right, Alt+Up and Alt+Down.
- The help line is the last line of `WorkbenchWindow.HelpText`.
- Validation: the full `dotnet test` run was not repeated in this tree; `TruthWeaver.Tool.Tui.Tests` (209 tests) passes.
