# 07: Policy sets with combining algorithms

**Group:** B run at scale

**What to build:** Many rules with targets, priorities and combining algorithms (deny-overrides, permit-overrides, first-applicable) and obligations, as in XACML, Cedar and OPA. Likely a new package. Natural next step after the ASP.NET adapter (18), which will show which combining behavior hosts want.

**Blocked by:** 18 (to learn what hosts need)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
