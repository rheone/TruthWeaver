# 02 - Migrate RuleImplication and RuleOverlap to the analysis budget module

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: refactor
Severity: low
Blocked by: 01
Effort: [architecture-deepening-4](../spec.md)

**What to build:** `RuleImplication` (`Implies` and `Compare`) and `RuleOverlap` (`Compare`) use `Run` and `Printed` from ticket 01. The two private `Printed` helpers and `RuleImplication.CapReason` are deleted, because the module owns that work.

**Constraints:**
- `RuleImplication.Compare` returns `RuleRelationResult` and `Implies` returns `RuleImplicationResult`; each maps the attempt to its own Undecided result.
- An overlap with no joint outcome keeps its empty list when undecided.
- Reason texts are unchanged (several roots).

**Acceptance criteria:**
- [ ] Neither type contains a `BddNodeBudgetExceededException` catch, a cap message, `CapReason` or a `Printed` helper.
- [ ] Existing implication and overlap tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.
