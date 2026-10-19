# 19: One source for fallback keys

**What to build:** A key in the code and the key in the docs cannot drift apart. A test checks every command key and second key against the key tables of `docs/tui.md`, and the static key properties that exist only for tests go away.

**Blocked by:** 13 (Fallback keys that really arrive).

**Status:** ready-for-agent

- [ ] A test fails when a command key or second key is missing from the key tables of `docs/tui.md`, or when the tables list a key that no command has.
- [ ] The tests read keys from the commands, with no test-only static key properties.
- [ ] The test names the command and the key in its failure message.
- [ ] `docs/tui.md` passes the new test.
