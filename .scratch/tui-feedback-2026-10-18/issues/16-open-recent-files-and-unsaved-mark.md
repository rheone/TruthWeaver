# 16: Open recent files and unsaved-changes mark

**What to build:** The File menu lists the recently opened files, and the window title shows a mark when the rule has unsaved edits. The list is kept in the preferences file and holds only paths, never rule text.

**Blocked by:** 05

**Status:** done

- [x] The File menu lists up to ten recent files; a missing file is shown as unavailable and can be removed.
- [x] The unsaved mark appears after an edit and disappears after a save.
- [x] `workbench.json` stores the list, and the preferences section of `docs/tui.md` says so.
- [x] A test covers the list order, the cap and the mark.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
