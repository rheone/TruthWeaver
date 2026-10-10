# 25: Predicate twin naming and string In placement

**What to build:** Equality is `Equals` for strings and `Equal` for numbers; twins are `NotEqual`, `NotEqualsIgnoreCase`, `NotEqualsConfigurable`; order twins differ across numbers (`GreaterThanOrEqual`), collections (`NotCountLessThan`) and dates (`NotAfter`); string `In`/`NotIn` live in `CollectionPredicates`; type tests have no `NullBehavior`. Decide a naming rule. Recommended: `Not` prefix on every twin, `In`/`NotIn` for strings in `StringPredicates`, and a `NullBehavior` parameter on type tests.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the rule
- [ ] Renames update reference pages under `docs/strong-k3/` in the same change and `K3ReferenceSyncChecker` passes
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
