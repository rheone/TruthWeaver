# 05: Command keys never lost to the focused pane

**What to build:** Every workbench command key works wherever the focus is. Today the focused editor takes F3 for Find next, so Save into the document never runs from the editor. A test checks every command key against the key bindings of the editor, the list and the text field, so a new clash fails the build, and it replaces the hand-written list of keys to avoid in the comment on the commands.

**Decisions (settled by grilling):**
- Save into the document moves to a free key (the clash test of this ticket checks it); the editor keeps F3 for Find next. The menu and the key reference show the new key.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Save into the document runs from the editor, the tree and the other panes.
- [x] A test fails when a command key is also bound by the editor, a list or a text field, except the keys the window owns on purpose (Undo and Redo).
- [x] The comment on the commands names the test instead of a hand-written list.
- [x] `docs/tui.md` and the key reference show the current key.
