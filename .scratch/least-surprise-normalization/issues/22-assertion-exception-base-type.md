# 22: One exception family for the Testing assertions

**What to build:** Rule and rewrite assertions throw `DecisionAssertionException`, and the harness and fuzzer throw their own types. Decide a single base type and a verb style (`Assert*`, fluent `Be*`/`Have*`, report `ShouldPass`). Recommended: a `TruthWeaverAssertionException` base, with the existing types deriving from it, and `HaveFaultForTerm` renamed to match its predicate-name argument.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the base type and the naming
- [ ] Existing exception types keep working or the break is recorded in `CHANGELOG.md`
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
