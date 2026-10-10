# 01: `ignoreCase` defaults to false

**What to build:** An author who writes `EqualsConfigurable(value: "Admin")` gets the same case-sensitive match as `Equals`, and opts in with `ignoreCase: true`. `NotEqualsConfigurable` changes with it, because it is the K3 complement twin.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The schema default for `ignoreCase` is `false` on both predicates, and the argument description says so
- [ ] The tests that relied on the old default state `ignoreCase` explicitly or assert the new behavior; one test pins the default for each predicate
- [ ] The reference pages `string-equalsconfigurable.md` and `string-notequalsconfigurable.md`, `docs/predicates.md`, any example in `docs/examples.md` and `CONTEXT.md` describe the new default with no "changed from" text
- [ ] `CHANGELOG.md` records the breaking change with a migration step: add `ignoreCase: true` to keep the old behavior
- [ ] `dotnet test tests/TruthWeaver.Tests --filter-class "*K3Reference*"` passes
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
