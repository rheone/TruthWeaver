# 31: An Insert group cannot have an empty family set

**What to build:** A developer cannot add an Insert menu group that stands for no generator family. Creating a group with an empty family set fails at once, with a message that names the group, and not only in an indirect test later. A temp-folder prefix in the second-key tests that still names an old key gets an honest name.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Creating an `InsertGroup` with `RuleOperatorFamilies.None` fails with a message that names the group title.
- [x] A test covers the failure and a test covers a valid group.
- [x] The Insert menu and the Generate dialog work as before, and the existing generate tests pass without changes to their expectations.
- [x] The temp-folder prefix in `SecondKeyTests` no longer names a key that the test does not use.
