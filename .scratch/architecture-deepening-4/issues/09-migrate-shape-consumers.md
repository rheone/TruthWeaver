# 09 - Migrate the remaining rule-shape consumers

Status: needs-triage
Category: refactor
Severity: low
Blocked by: 08
Effort: [architecture-deepening-4](../spec.md)

**What to build:** `StubEvaluator` (its shape visitor), `RuleStatistics` (its walk) and `RewritePlayground` use the rule-shape reader. Each keeps a small projection for its own needs.

**Constraints:** Where the walks treated definitions differently, the ticket 07 decision rules. A change in `RuleStatistics` output is user-visible: add a `CHANGELOG.md` entry and update the page that describes the report.

**Acceptance criteria:**
- [ ] No Tool.Core consumer outside the reader reads the JSON keys `predicate`, `args`, `op`, `operands` or `from` from `PrintJson` output.
- [ ] Existing stub evaluator, statistics and rewrite playground tests pass unchanged, or a changed expectation is explained by the 07 decision.
- [ ] The validation list in CLAUDE.md passes.
