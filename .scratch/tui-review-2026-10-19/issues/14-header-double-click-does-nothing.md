# 14: Header double-click does nothing

**What to build:** In a list dialog, a double-click on a group header line (such as `--- Numeric ---`) does nothing, as ticket 01 decided and `docs/tui.md` says. Today it moves the highlight to the first entry of the group. Enter on a header still moves the highlight to the first entry.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A double-click on a header leaves the highlight and the dialog as they were.
- [ ] Enter on a header still moves the highlight to the first entry of its group.
- [ ] A test covers both, in the plain and the filtered dialog.
- [ ] `docs/tui.md` still matches the behavior.
