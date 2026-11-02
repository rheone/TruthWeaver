# 10 - Decide whether the read-side document reader earns a seam

Status: needs-triage
Category: enhancement
Severity: low
Blocked by: none
Effort: [architecture-deepening-4](../spec.md)
Type: grilling

**What to build:** A decision, recorded in the spec. About nine Tool.Core modules (`TreeValue`, `DocumentOutline`, `SourceNodeIndex`, `EmbeddedRuleReader`, `PredicateSchemaFile`, `AssignmentVariables`, `RuleTestCaseText`, `DocumentRewrite`) build their own parser for JSON or YAML and switch on `RuleFormat`. The question is whether one reader with source offsets serves them, or whether they need different outputs (spans, plain trees, round-trip) so a shared seam would only move the complexity. The deletion test was unclear. A seam needs two adapters: list which readers would be the second.

**Acceptance criteria:**
- [ ] A table of readers (output needed: spans, plain tree, round-trip) is in the effort spec.
- [ ] The outcome is build tickets, filed under this effort, or a recorded no with the reason.
