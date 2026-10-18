# 14: Undo and redo

**What to build:** Editor edits and tree-pane edits (insert, delete, rewrite accept, convert) can be undone and redone with the usual keys, and the Edit menu shows the shortcuts. The first step is to check what exists today; if the editor already has undo, the work is the tree-pane and dialog edits and the menu entries.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Ctrl+Z and Ctrl+Y (or the keys the menu shows) undo and redo an editor change and a tree-pane change in one history.
- [ ] A rewrite accept, a convert and a generated rule each undo in one step.
- [ ] A test covers undo after each kind of edit.
- [ ] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
