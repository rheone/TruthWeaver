# 32: Refresh the stale k3-conformance spec after followups 04-06

**What to build:** `.scratch/k3-conformance/spec.md` still describes the pre-followup design. Stories 13-16 say `NXOR` (now `PARITY`), stories 22-23 describe in-rule `Project` and an outermost `Collapse` (both are now `Decision` methods), and the header says "`Collapse` lives on `CompiledRule`", names `XorArityViolation` (now `InfixArityViolation`) and refers to "decisions 1-17". Update the spec so it matches ADR-0005 and the code. Do not touch `_superseded/`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Stories 13-16 and 22-23 describe `PARITY`, `Decision.Project` and `Decision.Collapse`
- [ ] The header names the current diagnostics and decision numbering
- [ ] ADR-0005 remains the authority and the spec points to it where they could diverge

Source: review of PR #4, Spec axis, missing or partial item on `spec.md`.
