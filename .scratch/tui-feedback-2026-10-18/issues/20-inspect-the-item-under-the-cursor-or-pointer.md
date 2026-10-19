# 20: Inspect the item under the cursor or pointer

**What to build:** A user sees what a part of the rule means and what it is doing now. A keyboard inspect key and mouse hover show the same text for the item under the cursor or pointer, in the editor and the tree pane. Hover is a convenience: some terminals (Visual Studio's terminal, some SSH and tmux setups) do not report mouse movement, so the keyboard route must give the full information.

The text depends on the part of the rule:

- **Operator:** name, arity, one-line description (from `OperatorDefinitions`), the K3 rule in words, a small truth table for `IMPLIES` and `If`, and the node's current value, with `skipped` when it was short-circuited.
- **Predicate:** description, argument names and kinds from the predicate catalog, what it answers for a null value (`NullBehavior`), and a mark when it is an external operator.
- **Term or variable:** current value; for a `from(...)` variable the source, the path and the resolved value or the failure kind; for a `let` name, where it is defined.
- **`let` use:** the definition body and its parameters.
- **Constant or letter:** which constant `T`, `F` or `U` stands for.
- **Cardinality operator:** the threshold and how many operands are True, False and Unknown now.
- **Diagnostic span:** code, full message, suggestion and the documentation page.
- **Note:** the full note text.
- **Whole rule or sub-expression:** analyzer findings, such as always True or a redundant operand.

**Blocked by:** 12 (value display)

**Status:** done

- [x] One description provider builds the text for each part of the rule, and the keyboard key and the hover use it, so the two never differ.
- [x] A keyboard inspect key (shown in the menu and the hint line) shows the text for the item under the cursor in a panel or the status area.
- [x] Mouse hover shows the same text in a tooltip after a short delay, in the editor and the tree pane. Where the terminal reports no mouse movement, the keyboard route still works and the docs say so.
- [x] Values are shown as words, not only as color.
- [x] Operator and predicate descriptions come from `OperatorDefinitions` and the predicate catalog (the sources ticket 07 uses), not copies.
- [x] A test covers each kind of part listed above, and checks that the keyboard and hover text are equal.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
