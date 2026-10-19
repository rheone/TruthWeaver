# 17: Export dialog polish

**What to build:** The export dialog never hides the note about a missing image renderer, and the folder export does not write on a stray Enter. The one-line note about the missing renderer shows on its own line under the format list, whatever the chosen entry says in its hint. Enter in the Types list of the folder export moves the focus to the path field and writes nothing.

**Decisions (settled by grilling):**
- Enter in the Types list of the folder export moves the focus to the path field. Enter in the path field, or the To File button, then writes.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The image note is visible when an entry with its own hint is chosen.
- [x] Enter in the Types list of the folder export writes nothing and focuses the path field.
- [x] Tests cover both.
- [x] `docs/tui.md` describes the current keys.
