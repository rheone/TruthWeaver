# 11 - Decide shared node-text options for the renderers

Status: needs-triage
Category: enhancement
Severity: low
Blocked by: none
Effort: [architecture-deepening-4](../spec.md)
Type: grilling

**What to build:** A decision, recorded in the spec. `DotOptions`, `HtmlOptions` and `MermaidOptions` repeat `OperatorStyle`, `ShowArgumentValues` and `Constants`, and `RuleRenderTree.Build` takes the three at 8 call sites. The DOT renderer hard-codes its state colors where Mermaid takes a palette. The option types are public API, so a shared value is a public change. The grilling answers whether the gain (one value, one place for node text) is worth it for a library consumer, and whether the DOT palette difference is a bug.

**Acceptance criteria:**
- [ ] The decision and its reason are in the effort spec.
- [ ] The outcome is build tickets, filed under this effort, or a recorded no.
