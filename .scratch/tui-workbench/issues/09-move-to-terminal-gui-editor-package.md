# 09: Decide on the Terminal.Gui.Editor package

The worker adds no project, package or dependency without the owner's approval.

**What to build:** A decision, then the change it calls for. Terminal.Gui 2.5 marks its `TextView` obsolete in favor of the editor of the separate `Terminal.Gui.Editor` package. The workbench uses `TextView` in three places (`RuleEditorView`, `VariablesEditorView`, `ViewPaneView`), each with a narrow `CS0618` suppression. The owner decides whether to adopt `Terminal.Gui.Editor`, which is a new dependency. If the owner approves it, replace `TextView` with the package's editor, remove the `CS0618` suppressions, keep the rule editor's diagnostic coloring and its undo and redo binding to the document history, and record the package in `Directory.Packages.props` and the architecture test for the TUI project's references. If the owner declines it, keep `TextView` and close this ticket with the reason.

**Blocked by:** none (needs the owner's decision on a new dependency)

**Status:** needs-owner-decision

- [ ] The owner approves or declines the `Terminal.Gui.Editor` dependency, and the decision is recorded in this ticket
- [ ] If approved: no `TextView` reference and no `CS0618` suppression remain in `src/TruthWeaver.Tool.Tui`
- [ ] If approved: diagnostic coloring, undo and redo through the document history, and the read-only view pane behave as before (tested)
- [ ] If approved: the package is in `Directory.Packages.props` and reaches only the TUI project, and an architecture test checks this
- [ ] The page for this feature is updated in the same change (`docs/tui.md`, the Terminal.Gui section), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
