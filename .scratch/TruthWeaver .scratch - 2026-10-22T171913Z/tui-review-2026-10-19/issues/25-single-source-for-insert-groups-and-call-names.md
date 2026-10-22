# 25: Single source for Insert groups and call names

**What to build:** One list defines the Insert menu groups, and the Generate rule families read it, so a new group needs one edit. One documented rule finds the name of a term call, with a test. Behavior does not change.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The Insert menu groups and the Generate rule families use one list of group identifiers, not matching title strings in two places.
- [x] A group with no generator family fails a test, and does not throw from a static initializer on first use.
- [x] `TermCalls.NameOf` has one documented rule, and a test covers a call, a bare name and a call with spaces.
- [x] The thin `SameCall` wrapper is gone or has a reason in its summary.
- [x] All existing tests pass without changes to their expectations.
