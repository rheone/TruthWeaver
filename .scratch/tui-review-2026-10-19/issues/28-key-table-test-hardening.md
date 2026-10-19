# 28: Key-table test hardening

**What to build:** The test that compares the key tables of `docs/tui.md` with the commands cannot pass by accident and does not depend on the exact text of the document. A duplicated group heading fails the test. Several heading renames are covered, not only one. The failure tests change the document by code (move a row, delete a row, add a row, rename a heading) and do not search for a hard-coded row text. One helper finds the documented keys of a command. The test names start with a member or type under test.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A duplicated group heading fails with a message that names the heading.
- [ ] Each group heading, renamed in turn, fails with a message that names it.
- [ ] The failure tests build their changed document from the parsed tables, with no hard-coded row text.
- [ ] One helper returns the documented keys of a command, and the repeated inline lookup is gone.
- [ ] The test names follow `{MemberUnderTest}_{Scenario}_{Expectation}_Test` with a real member or type first, and the architecture naming test passes.
