# 12: Show the value of a term or variable

**What to build:** The workbench shows the current value of a term or variable: the term under the cursor in the status line, and a value badge in the tree pane (`True`, `False` or `Unknown`, as the word and not only the color).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The value comes from the current assignment and updates when the assignment changes.
- [ ] A variable with a `from(...)` source shows its resolved value or the failure kind.
- [ ] A test covers a term, a variable and a term with no assignment.
- [ ] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
