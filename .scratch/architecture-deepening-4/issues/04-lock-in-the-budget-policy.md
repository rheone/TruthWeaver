# 04 - Lock in the analysis budget policy

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: refactor
Severity: low
Blocked by: 02, 03
Effort: [architecture-deepening-4](../spec.md)

**What to build:** An architecture test fails when a `BddNodeBudgetExceededException` catch or the "configured cap" reason text appears in a verdict type instead of the analysis budget module. A new verdict cannot forget the policy, because the build says where it lives.

**Constraints:** The allowlist names the files that keep their own handling on purpose: the module itself, `Analyzer` and `BddManager` (they raise and wrap the exception), `RuleSetAnalysis` (pairwise fallback), `TruthTablePrinter` (swallows it) and `RewritePlayground` if it still holds its own cap. Add the check to `TruthWeaver.Architecture.Tests`, in the style of its existing rules.

**Acceptance criteria:**
- [ ] The test is shown red by adding a stray catch to one verdict type, then green after removing it.
- [ ] Each allowlist entry carries a one-line reason.
- [ ] The validation list in CLAUDE.md passes.
