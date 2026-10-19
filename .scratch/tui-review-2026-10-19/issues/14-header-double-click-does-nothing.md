# 14: Header double-click does nothing

**What to build:** In a list dialog, a double-click on a group header line (such as `--- Numeric ---`) does nothing, as ticket 01 decided and `docs/tui.md` says. Today it moves the highlight to the first entry of the group. Enter on a header still moves the highlight to the first entry.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] A double-click on a header leaves the highlight and the dialog as they were.
- [x] Enter on a header still moves the highlight to the first entry of its group.
- [x] A test covers both, in the plain and the filtered dialog.
- [x] `docs/tui.md` still matches the behavior.
