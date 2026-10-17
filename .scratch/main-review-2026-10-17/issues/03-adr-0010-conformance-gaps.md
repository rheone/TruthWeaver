# 03: ADR-0010 conformance gaps

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** needs-triage

**What to build:** Confirm each item against ADR-0010, then split into tickets or amend the ADR: (1) ToCnf/ToDnf put only the outer tree in normal form and do not normalize the quantifier body (decision 9); (2) no truth-table oracle over collection sizes 0 to 4 plus null for quantifier rewrites (decision 9); (3) RuleCoverageAccumulator skips body terms and counts bodies per element instead of 'reached in at least one element', and an empty collection records nothing for the body; (4) Decided stops at the body root; (5) CheckValues trusts QuantifierExpression => true, so a mismatched quantifier trace is never detected; (6) body diagnostics may not use the body's own path; (7) CONTEXT.md and the ADR-0005 connective list do not name FORALL/EXISTS; (8) ADR-0010 is not amended with the diagnostic-code allocation or the chosen elementSource registration shape.

**Evidence:** Spec review of commit cadfc81 and of main at ef26bd5. The reviewer did not build or run tests, so treat each item as a lead.

- [ ] Each item is confirmed and fixed, or recorded as intended with the ADR amended
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
