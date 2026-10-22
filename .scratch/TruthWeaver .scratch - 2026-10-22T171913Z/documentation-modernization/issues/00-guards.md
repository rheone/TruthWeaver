# 00: Guards (phase 0)

**What to build:** The rule "do not cite ADRs, tickets, PRDs or `.scratch` paths in code comments, XML docs, tests or user-facing documents; describe the behavior or reason instead" is written into `CLAUDE.md` (Development rules and Documentation). A guard test fails on `ADR-nnnn`, `ticket nn`, `PRD` and `.scratch` in `src`, `tests` and in-scope documents. Exempt: `docs/adr/`, `.scratch/`, `docs/agents/`, `.agents/`, `CLAUDE.md`. A shrink-only baseline lists the files that offend today, following the `DocumentationLintBaseline` pattern.

**Blocked by:** None

**Status:** on-hold (do not start or dispatch until the owner triggers the effort) (the owner may approve this ticket alone before the trigger)

- [ ] `CLAUDE.md` states the rule
- [ ] The guard test fails on a new reference in a file not on the baseline, and a test fails if a baseline entry no longer offends
- [ ] The exempt locations are documented in the test
- [ ] The full validation set from `CLAUDE.md` passes
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
