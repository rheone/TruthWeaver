# 08: Share term results across many rules

**Group:** B run at scale

**What to build:** When a rule set shares predicates, evaluate each term once per context across all rules (extend the per-rule memoization). Performance feature for policy sets.

**Blocked by:** 07

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
