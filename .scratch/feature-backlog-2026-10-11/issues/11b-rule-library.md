# 11b: Rule library: named rules referenced from other rules

**Group:** C author and manage rules

**What to build:** A rule library: named, compiled rules stored in a registry and referenced from other rules (for example `rule("adult-customer") AND hasCoupon`), expanded with the same compile-time expansion as ticket 11. Needs a store, versioning (a reference names a version or a hash), cross-rule cycle detection, and a decision on how a referenced rule's own registry and data-source declarations merge. Builds on 11 and links to the rule store (12). Grill the owner first.

**Blocked by:** 11

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages and dependencies)
- [ ] The pages that describe the behavior are written or updated in the same change (rule text, JSON and YAML formats, builder, diagnostics table, glossary), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
