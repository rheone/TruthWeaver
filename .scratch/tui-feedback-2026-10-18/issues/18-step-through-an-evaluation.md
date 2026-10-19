# 18: Step through an evaluation

**What to build:** The user steps through an evaluation: the tree pane highlights the node being evaluated and shows the value it returned, one node per step, in evaluation order. It uses the same trace the engine already produces and does not change any evaluation result.

**Blocked by:** 12

**Status:** ready-for-agent

- [x] Next and Previous keys move the highlight through the trace.
- [x] Each step shows the node, its value as a word and, for a skipped node, the word `skipped`.
- [x] A fault shows its message at the node.
- [x] A test steps a rule with an `AND` short-circuit and a skipped node.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
