# 07 - Decide the rule-shape reader's interface

Status: needs-triage
Category: enhancement
Severity: low
Blocked by: none
Effort: [architecture-deepening-4](../spec.md)
Type: grilling

**What to build:** A decision, recorded in the spec: what a rule-shape reader exposes (terms with predicate and arguments, operator counts, variable references) and how it treats `let` definitions. Five consumers (`RuleTermList`, `StubEvaluator`, `RuleStatistics`, `RewritePlayground`, and the palette, which the earlier review covers) read `PrintJson` output today, and their walks disagree about definitions. The grilling answers, for each consumer, whether it should see definitions expanded, as written or not at all.

**Acceptance criteria:**
- [ ] The decision table (consumer, what it needs, definition handling) is in the effort spec.
- [ ] Tickets 08 and 09 are updated to match, or closed with a recorded no if the deletion test fails.
