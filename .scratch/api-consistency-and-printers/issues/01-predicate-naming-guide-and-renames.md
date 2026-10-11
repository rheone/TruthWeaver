# 01: Predicate naming guide and renames

**What to build:** Write the twin-naming guide (Form A `IsX`/`IsNotX`, Form B `Verb`/`NotVerb`, the twin is the Kleene complement) into `docs/predicate-conventions.md`, then apply it to the catalog. Renames: `Equals`/`Equal` (string, numeric, scalar) become `IsEqualTo` with optional `ignoreCase` and `trim` arguments (default `false`), absorbing `EqualsConfigurable`; `EqualsIgnoreCase` is removed (it is `ignoreCase: true`); twins become `IsNotEqualTo`. Ordering becomes `IsGreaterThan`, `IsGreaterThanOrEqualTo`, `IsLessThan`, `IsLessThanOrEqualTo`, with new `IsNot…` twins that are exact complements. `Between`, `In`, `After`, `AfterNow`, `Before`, `BeforeNow`, `InMonth`, `InTimeWindow`, `OnDayOfWeek` take the `Is` prefix and `IsNot` twins. State and type tests, and the Form B predicates, keep their names. Regex and count predicates are tickets 02 and 03. Enumerate every predicate with its old and new name first, and list the result in the CHANGELOG. The packages are unpublished, so the break is allowed; there is no deprecation shim.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `docs/predicate-conventions.md` states the guide with the complete old to new table
- [ ] Every renamed predicate has its `docs/strong-k3/predicates/` page renamed, the category index and root navigation updated, and `K3ReferenceSyncChecker` passes
- [ ] `NotXTwinTable` covers the new ordering twins
- [ ] `EqualsIgnoreCase` and `EqualsConfigurable` no longer exist; their behavior is reachable through `IsEqualTo` arguments, with tests
- [ ] No old name remains in `src`, `tests`, `docs`, `samples` or `benchmarks` outside the CHANGELOG migration table
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
