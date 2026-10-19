# 04: Mouse and dialog audit

**What to build:** A checked pass over the remaining mouse paths and dialogs, so defects like the double-click one are found and recorded. Cover divider drag, context menus, the Assign term list, the Diagnostics pane and the filtered list dialog (whether the selection resets when the filter changes and whether the highlighted row is the one Enter takes). Also check in a real terminal that mouse movement reaches the hover box, and note which terminals do not report it.

**Blocked by:** 01 (List dialogs act only on a real row).

**Status:** done (hover check still needs a person at a real terminal)

- [x] Each path above is exercised, by test where possible and by hand where a terminal is needed.
- [x] Each defect found is fixed with a test, or recorded as its own ticket under this folder.
- [x] The result lists the paths checked, what was found, and the terminals tried for hover.
- [x] `docs/tui.md` is updated if the supported mouse behavior changes.
- [ ] Mouse movement reaching the hover box was tried in a real terminal. **Not done**: no real terminal is available in this
  environment, so this item stays open. See "Hover" below for what that leaves unverified.

## Result

Paths checked, all by test (`dotnet test tests/TruthWeaver.Tool.Tui.Tests`, 500 passed):

- **Divider drag** (`tests/TruthWeaver.Tool.Tui.Tests/Workbench/MouseTests.cs`): vertical and horizontal divider drag, both
  with per-step move events and with only a press and a release, and a drag starting inside a pane (no-op). All exercised,
  all already correct; no defect.
- **Context menus** (`tests/TruthWeaver.Tool.Tui.Tests/Workbench/ContextMenuTests.cs`): right click in each of the four panes
  (editor, tree, Assign terms, Diagnostics) opens that pane's menu; a right click selects the row under the pointer first
  (`WorkbenchWindow.OnApplicationMouse`), so the menu always acts on what the pointer is over, not the last keyboard
  selection; a right click outside the four panes opens nothing; Esc closes. All exercised, all already correct; no defect.
- **Assign term list**: added `Click_OnATermOfTheAssignPane_SelectsTheTerm_Test` (`MouseTests.cs`) covering a left click
  selecting a term so a value key (`t`/`f`/`u`) acts on the clicked term. The pane rebuilds its list in place
  (`ObservableCollection.Clear()`/`Add()`, not a new `SetSource`) and clamps the kept index afterward
  (`AssignmentView.Show()`), so a rule edit keeps the right term selected. No defect.
- **Diagnostics pane**: added `Click_ThenFewerDiagnostics_KeepsTheSelectionInRange_Test` (`MouseTests.cs`). Checked by hand
  first with a throwaway probe test: selecting the last of three diagnostics and then editing the text down to one
  diagnostic left `ListView.Value` at `0`, not stuck at the old index or past the end. `ListView` clamps its own selection
  when the bound `ObservableCollection` shrinks; `DiagnosticsPane` never calls `SetSource` again after the first time, so this
  path was already safe. No defect.
- **Filtered list dialog** (`ChoiceDialog`, `tests/TruthWeaver.Tool.Tui.Tests/Views/ChoiceDialogTests.cs`): this is where a
  real defect turned up (see below). Fixed, with two new regression tests.

### Defect found and fixed: a filter edit dropped the highlight entirely, not onto a row

`ChoiceDialog`'s search box rebuilds the list with `ListView.SetSource(...)` on every keystroke (`ShowVisible()`), unlike
`AssignmentView` and `DiagnosticsPane`, which mutate an already-bound `ObservableCollection` in place. Calling `SetSource`
again replaces the list's source and clears `ListView.Value` to `null`, regardless of whether the old index was still in
range of the new, filtered list. A throwaway probe test confirmed it: selecting the last unfiltered row, then narrowing the
filter to one match, left `List.Value` at `null`, not `0` and not the old index. The effect for the author: type a filter
character, the highlight disappears, and Enter does nothing until an arrow key or a click re-establishes a selection. This
is adjacent to, but distinct from, the ticket 01 defect (list dialogs acting on the wrong row); here the dialog acted on no
row.

Fix (`src/TruthWeaver.Tool.Tui/Views/ChoiceDialog.cs`, `SearchBox.TextChanged`): after rebuilding the source, select row `0`
of the new filtered list when it has any rows. `Activate()` already handles a header landing at row `0` by moving to the
first entry of its family, so this is safe when the top match is a header line, not only a real entry.

New tests in `tests/TruthWeaver.Tool.Tui.Tests/Views/ChoiceDialogTests.cs`:

- `TextChanged_NarrowsPastThePreviousSelection_SelectsTheFirstVisibleRow_Test`: selects the last row, filters down to a
  single different match, asserts the highlight lands on it and Enter chooses it.
- `TextChanged_TopMatchIsAHeader_EnterMovesToTheFirstEntryOfTheFamily_Test`: filters to a result whose first visible row is a
  family header, asserts the highlight lands on the header and Enter moves it to the first entry rather than doing nothing.

### Hover

Mouse movement reaching the hover box (the inspect-on-hover path in `WorkbenchWindow.OnHover`) was **not tried in a real
terminal** in this task: this environment has no interactive terminal to run the TUI in, so every mouse check above went
through the headless driver and the injected-event pipeline, not a real terminal's input stream.

From documentation rather than a live check, the xterm mouse-tracking protocol that terminal emulators implement
distinguishes click-only reporting (modes 1000/1002) from any-event reporting that also sends movement with no button
held (mode 1003); a terminal or an intermediate layer (a multiplexer such as `tmux` or `screen` in particular, which has to
pass mouse sequences through deliberately) can support clicks while never emitting the motion events the hover box needs,
and whether it does is a per-terminal, per-configuration fact this task did not verify against any specific product. This
is a general protocol note, not a tried-and-confirmed list of terminal names: do not read it as a claim that any named
terminal was tested here. Confirming the hover path needs a person running the TUI in whichever terminal(s) the team
actually uses, and ticking this checklist item only after that.
