# 22: Key-table docs test per group

**What to build:** The test that compares the key tables of `docs/tui.md` with the commands notices a key listed under the wrong menu group, and a renamed group heading. The failure message names the command, the key and the group.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] A key listed under the wrong group fails the test.
- [x] A group heading that is renamed or missing fails with a message that names the heading, not with zero rows.
- [x] The tests do not flatten all tables into one set.
- [x] A short test shows each failure by a change of the document text kept in the test.
- [x] `docs/tui.md` passes the new checks.
