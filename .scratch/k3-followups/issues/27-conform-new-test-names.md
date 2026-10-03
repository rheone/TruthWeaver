# 27: Conform the tests added on this branch to the naming convention

**What to build:** Every test added on the StrongK3+Operations branch is named `{MemberUnderTest}_{Scenario}_{Expectation}_Test`, as CLAUDE.md Testing requires. The review found about 35 non-conforming names: snake-case sentence names with no `_Test` suffix (the `*_shape_carries_its_operands_in_order` family, `Decision_collapse_*`, `Nand_and_nor_*`), PascalCase names with no suffix (`UnknownFakePredicate_*`, `Returning_Kleene_AnyValue_*`), and names where `Test` is fused to the last segment (`*ReturnsFalseTest`). Renames only; no behaviour or assertion changes. Tests that already existed on main with the old style are out of scope.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The non-conforming tests are listed by a grep over the branch diff before any rename
- [ ] Each listed test is renamed to the convention, with its XML summary still accurate
- [ ] No test body changes; the same tests pass before and after
- [ ] The full validation from CLAUDE.md passes

Source: review of PR #4, Standards axis, hard violation 1. Related: [28](28-decide-aaa-and-nsubstitute-rules.md).
