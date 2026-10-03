# 20: Remove the `culture` argument from `EqualsConfigurable`

**What to build:** `EqualsConfigurable` compares ordinally (or ordinal-ignore-case when `ignoreCase` is set) and no longer has a `culture` argument anywhere. Ticket 12 restricted `culture` to the empty string, which leaves a vestigial argument that must be `""`; the owner decided (2026-10-03) to remove it outright. A rule that still passes `culture` (DSL, JSON, YAML or builder) gets a clear authoring diagnostic naming the argument to remove, following the pattern used for the retired `NXOR` spelling, not a silent compile break. This is a deliberate breaking change for any rule that spells `culture: ""`. The predicate keeps the name `EqualsConfigurable`; renaming is out of scope. Update the predicate schema and argument descriptions, README (including diagrams and printed output), CONTEXT.md and the predicate-catalog gap list, and migrate tests that used `culture: ""`.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] The failing test run (a rule with `culture`, expected to be rejected with the diagnostic) is shown before the implementation
- [ ] `EqualsConfigurable` has no `culture` argument in its factory, schema or argument descriptions
- [ ] A rule that passes `culture` is rejected with a diagnostic that names the argument and says to remove it
- [ ] Ordinal and ordinal-ignore-case behavior is unchanged and still covered by tests
- [ ] README, CONTEXT.md and the gap list no longer mention `culture` as accepted
- [ ] The full validation from CLAUDE.md passes

Source: ticket 12 comments (`culture` kept restricted to empty); owner decision 2026-10-03. See also [spec](../spec.md).

## Comments

- 2026-10-03 (PR #4 review): until this ticket lands, a non-empty `culture` throws `ArgumentException` at evaluation time, which surfaces as Unknown plus a Fault. Existing rules that set `culture: "en-US"` therefore silently change from True/False to Unknown. Make the new authoring diagnostic cover that case, and call the change out in the changelog or README as breaking.
- 2026-10-03 (implementation): `culture` removed from the `EqualsConfigurable` factory, schema and descriptions. No predicate-specific rejection was added: the existing `UnknownArgument` diagnostic (all front ends) now carries a `Hint` suggestion "Remove the argument 'culture'." whenever an undeclared argument has no near-miss declared name, so the retired argument is named with removal advice for DSL, JSON, YAML and builder alike. This also changes the suggestion for any other undeclared argument that has no near-name match (previously no suggestion). README (with a breaking-change callout, diagrams and printed output), CONTEXT.md and the gap list updated; tests migrated off `culture: ""`.
