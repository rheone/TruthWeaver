# 09: Decide on the Terminal.Gui.Editor package

The worker adds no project, package or dependency without the owner's approval.

**What to build:** A decision, then the change it calls for. Terminal.Gui 2.5 marks its `TextView` obsolete in favor of the editor of the separate `Terminal.Gui.Editor` package. The workbench uses `TextView` in three places (`RuleEditorView`, `VariablesEditorView`, `ViewPaneView`), each with a narrow `CS0618` suppression. The owner decides whether to adopt `Terminal.Gui.Editor`, which is a new dependency. If the owner approves it, replace `TextView` with the package's editor, remove the `CS0618` suppressions, keep the rule editor's diagnostic coloring and its undo and redo binding to the document history, and record the package in `Directory.Packages.props` and the architecture test for the TUI project's references. If the owner declines it, keep `TextView` and close this ticket with the reason.

**Blocked by:** none

**Status:** done (owner decision 2026-10-17: approved, adopt `Terminal.Gui.Editor`)

- [x] The owner approves or declines the `Terminal.Gui.Editor` dependency, and the decision is recorded in this ticket
- [x] If approved: no `TextView` reference and no `CS0618` suppression remain in `src/TruthWeaver.Tool.Tui`
- [x] If approved: diagnostic coloring, undo and redo through the document history, and the read-only view pane behave as before (tested)
- [x] If approved: the package is in `Directory.Packages.props` and reaches only the TUI project, and an architecture test checks this
- [x] The page for this feature is updated in the same change (`docs/tui.md`, the Terminal.Gui section), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- Version: `Terminal.Gui.Editor` 2.5.7, the newest stable release. It depends on Terminal.Gui 2.4.17 or later, so it runs with the pinned Terminal.Gui 2.5.0. It is in `Directory.Packages.props` and referenced by `TruthWeaver.Tool.Tui` only. `ToolTui_References_ToolCoreAndTerminalGuiOnlyThere_Test` checks that the TUI references it and that no other project does.
- `RuleEditorView`, `VariablesEditorView` and `ViewPaneView` now use `Terminal.Gui.Editor.Editor`. The diagnostic coloring is an `IVisualLineTransformer` that splits a text run where the severity changes (a run with wide characters takes the most severe mark as a whole). Undo and redo keep the "not handled" commands, so the keys reach the document history.
- Conservative choices: the gutter is off (`GutterOptions.None`), so the editors look as before; `RuleEditorView.InsertionPoint` is kept as a 0-based column and line for the tests; `VariablesView.Editor` returns the `VariablesEditorView` type because `Editor.Text` hides `View.Text`.
- `RuleEditorViewTests` covers the coloring, the cursor move, the read-only view pane, the editor's own undo in the variables editor and a source scan for `TextView` and `CS0618`.
