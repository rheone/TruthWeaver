# 10 - One module pairs a trace tree with the rule

Status: needs-triage
Category: refactor
Severity: low
Blocked by: 01, 09
Effort: [architecture-deepening-3](../spec.md)

**What to build:** `RuleCoverageAccumulator.Pair` and `RuleCounterfactual.ReadTermValues` pair a trace tree with the rule by position and check the text with two different term rules (`StartsWith(name)` and `StartsWith(name + "(")`). One internal module does the pairing for both, with one term rule, and keeps the text as the guard. The quantifier description is built in three places (`Evaluator`, `CollectionBinding`, `TraceText`); they use the one builder. No change to the public `TraceNode` or the decision-record schema (the owner kept the tree path internal).

**Constraints:** ADR-0014 section 5 (a mismatch is rejected with `ArgumentException` and no predicate runs); about 44 `.Text` assertions in 30 test files depend on the text form and must not change.

**Needs:** an Agent Brief once tickets 01 and 09 land.
