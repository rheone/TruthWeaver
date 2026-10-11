# 32: Refresh the stale k3-conformance spec after followups 04-06

**What to build:** `.scratch/k3-conformance/spec.md` still describes the pre-followup design. Stories 13-16 say `NXOR` (now `PARITY`), stories 22-23 describe in-rule `Project` and an outermost `Collapse` (both are now `Decision` methods), and the header says "`Collapse` lives on `CompiledRule`", names `XorArityViolation` (now `InfixArityViolation`) and refers to "decisions 1-17". Update the spec so it matches ADR-0005 and the code. Do not touch `_superseded/`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Stories 13-16 and 22-23 describe `PARITY`, `Decision.Project` and `Decision.Collapse`
- [ ] The header names the current diagnostics and decision numbering Note 2026-10-10: header numbering was not re-checked.
- [x] ADR-0005 remains the authority and the spec points to it where they could diverge

Source: review of PR #4, Spec axis, missing or partial item on `spec.md`.

## Comments

- 2026-10-03 (implementation): `.scratch/k3-conformance/spec.md` header now names `InfixArityViolation` (BRE0006), decisions 1-22 plus the decision 4, 12 and 14 amendments, and says ADR-0005 wins on divergence; stories 13, 14, 16 use `PARITY`; stories 22 and 23 describe `Decision.Project(bool unknownAs)` and `Decision.Collapse(CollapsePolicy)`; story 40, the derived-operator list, the Boundaries bullet, the Solution text and the slice-table note were aligned (slice names kept for traceability). Verified against `Decision.cs`, `DiagnosticCodes.cs` and ADR-0005. `_superseded/` untouched. Docs only; no source changed.
