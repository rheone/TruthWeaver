# 20: Remove the `culture` argument from `EqualsConfigurable`

**What to build:** `EqualsConfigurable` compares ordinally (or ordinal-ignore-case when `ignoreCase` is set) and no longer has a `culture` argument anywhere. Ticket 12 restricted `culture` to the empty string, which leaves a vestigial argument that must be `""`; the owner decided (2026-10-03) to remove it outright. A rule that still passes `culture` (DSL, JSON, YAML or builder) gets a clear authoring diagnostic naming the argument to remove, following the pattern used for the retired `NXOR` spelling, not a silent compile break. This is a deliberate breaking change for any rule that spells `culture: ""`. The predicate keeps the name `EqualsConfigurable`; renaming is out of scope. Update the predicate schema and argument descriptions, README (including diagrams and printed output), CONTEXT.md and the predicate-catalog gap list, and migrate tests that used `culture: ""`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The failing test run (a rule with `culture`, expected to be rejected with the diagnostic) is shown before the implementation
- [ ] `EqualsConfigurable` has no `culture` argument in its factory, schema or argument descriptions
- [ ] A rule that passes `culture` is rejected with a diagnostic that names the argument and says to remove it
- [ ] Ordinal and ordinal-ignore-case behavior is unchanged and still covered by tests
- [ ] README, CONTEXT.md and the gap list no longer mention `culture` as accepted
- [ ] The full validation from CLAUDE.md passes

Source: ticket 12 comments (`culture` kept restricted to empty); owner decision 2026-10-03. See also [spec](../spec.md).
