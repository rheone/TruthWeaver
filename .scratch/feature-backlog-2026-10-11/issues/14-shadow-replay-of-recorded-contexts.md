# 14: Shadow replay of recorded contexts

**Group:** C author and manage rules

**What to build:** Run recorded contexts (or decisions from ticket 02) through two rule versions and list the contexts whose decision changed, with the trace of each. The safe way to ship a rule change. Builds on 02, 06 and 13.

**Blocked by:** 02, 06, 13

**Status:** done

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Decisions (grilled 2026-10-17)

- **Home:** core library, no new package or dependency. A class beside `RuleCoverage` and `EvaluateManyAsync`, composed from 02, 06 and 13.
- **Changed context:** `Decision.Result` differs between the two versions. Options add `IsSatisfied`-only and fault-set differences. Each changed context carries both traces.
- **Inputs:** live contexts only (`IEnumerable` / `IAsyncEnumerable` of `TContext`). Replaying from decision records is a later add-on.
