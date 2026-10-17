# 07: Trace does not name the variable-resolution outcome

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** done

**Owner decision (2026-10-17):** Implement ADR-0006 decision 13: trace entries for variable terms name the reference and the outcome (resolved, missing, ambiguous).

**What to build:** Decide whether to implement ADR-0006 decision 13 (trace and faults name the reference and the outcome: resolved, missing, ambiguous) or amend the ADR.

**Evidence:** Evaluator.cs:488-511 shows only the term identity; outcomes appear in fault messages only, never for a resolved variable.

- [ ] The trace shows the outcome, or the ADR is amended
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
