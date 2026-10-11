# 11: Named sub-rules and parameters

**Group:** C author and manage rules

**What to build:** Reusable definitions inside the DSL (`let`-style names or a rule library), with parameters, so large rule sets do not repeat themselves. Rule joining exists but is whole-rule. A grammar, ADR and analyzer change (does the BDD see through a definition?); grill the owner first.

**Blocked by:** None (can start immediately)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
