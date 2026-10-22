# 24: Test gaps and test tidy

**What to build:** The tests cover what ticket 14 promises and follow the naming rule. A test shows whether the first click of a header double-click moves the highlight. Tests that start with a key (such as `AltW_...`) or a table name start with the member under test. The test helper that repeats key literals reads them from the commands. The tick-box hint text is a named constant.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] A test covers the first click of a header double-click.
- [x] Every new test name starts with the member under test and follows `{MemberUnderTest}_{Scenario}_{Expectation}_Test`.
- [x] No key literal is duplicated between the commands and a test helper (key objects are copied when pressed, as they are mutable).
- [x] The tick-box hint text is a constant that the dialog and its test share.
- [x] The architecture test for test names passes.
