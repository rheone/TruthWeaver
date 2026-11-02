# 03 - Migrate RuleCounterfactual to the analysis budget module

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: refactor
Severity: low
Blocked by: 01
Effort: [architecture-deepening-4](../spec.md)

**What to build:** `RuleCounterfactual`'s search entry (the code that checks the term cap and runs `ChangeSetSearch`) uses `Run` from ticket 01 with one root. Its cap check and its budget catch go.

**Constraints:**
- The one-root reason text ("The rule has N distinct terms, which exceeds the configured cap of C.") is unchanged.
- A bad fixed term still throws `ArgumentException` before the cap is checked, as today (the fixed terms resolve first).
- Out of scope, by the grilling decision: `RuleSetAnalysis` (it falls back to pairwise on a budget error), `Analyzer` and `TruthTablePrinter` (they catch the exception and carry on), and `RewritePlayground`.

**Acceptance criteria:**
- [ ] `RuleCounterfactual` has no `BddNodeBudgetExceededException` catch and no cap message of its own.
- [ ] Existing counterfactual tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.
