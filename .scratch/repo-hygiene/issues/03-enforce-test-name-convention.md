# 03: Enforce the test-name convention

**What to build:** A test fails when a new test method does not follow `{MemberUnderTest}_{Scenario}_{Expectation}_Test`, and existing violations are listed in a baseline that can only shrink.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] Count the real violations first: methods with `[Fact]`, `[Theory]` or the xUnit v3 equivalents whose name does not end in `_Test`. Helpers and theory data are not tests
- [ ] A test in `TruthWeaver.Architecture.Tests` reads test assemblies by reflection and fails on a test method that breaks the convention and is not in the baseline
- [ ] The baseline is a list of exempt test names in the style of `DocumentationLintBaseline`; the check fails on a baseline entry that is now clean
- [ ] The check covers `tests/` and `samples/*.Tests`
- [ ] The failure message names the method and the expected shape
- [ ] If the violation count is large, a second ticket migrates names in batches sized to one context window; renames keep each test's behavior unchanged
- [ ] The full validation set from CLAUDE.md passes

See also [spec](../spec.md).
