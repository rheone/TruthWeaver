# 11 - The counterfactual search rejects a decision of a rule with a quantifier

Status: needs-triage
Category: bug
Severity: low
Blocked by: none
Effort: [architecture-deepening-3](../spec.md)

## Finding

Found while resolving ticket 01. `RuleCounterfactual.FindChanges(rule, decision)` pairs the trace tree with the rule through `ReadTermValues`, which treats a quantifier like an operator with one operand per trace child. A quantifier's trace has one child per element, so the pairing fails with `ArgumentException` ("does not match the rule at '$.operands[0]'") for any decision of a rule with a quantifier. A null collection and a `[True]` collection both fail. No test or document covers quantifiers in this overload.

## Open decision

Read the quantifier as an atom (take its value from the trace node and ignore its elements, as analysis does, ADR-0010 decision 8), or reject such rules up front with a clear message. Ticket 10 (one pairing module) is the natural place to settle it.
