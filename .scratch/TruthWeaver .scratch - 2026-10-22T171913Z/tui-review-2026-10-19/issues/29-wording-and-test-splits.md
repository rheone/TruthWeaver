# 29: Wording and test splits

**What to build:** The comments and tests say one clear thing each. The two descriptions of how `TermCalls.NameOf` finds a name agree. The `SameCall` summary says what it does and why it exists, not how it was written. The `SameCall` test is split so that each test asserts one behavior.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The summaries of `NameOf` and its tests use one description of the rule.
- [x] The `SameCall` summary says what it compares and why the display text and the typed text need it.
- [x] The `SameCall` test is split into tests that assert one result each.
- [x] All tests pass without changes to their expectations.
