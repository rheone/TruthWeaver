# 04: TRE0016 reused although ADR-0005 retired it

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

**What to build:** Decide: amend ADR-0005 decision 14 to allow reuse, or move the rewrite-size code to a new TRE number.

**Evidence:** ADR-0005 decision 14 says TRE0016 NestedCollapse is retired and not reused. DiagnosticCodes.cs (~line 96) defines RewriteTooLarge = TRE0016 and CompilerOptions.cs:14 uses it. Changing a published code is a breaking change, so the ADR amendment may be the cheaper path.

- [ ] The code table, the ADR and docs/diagnostics agree
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
