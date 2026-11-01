# 05 - Derive the parser vocabulary from the operator table

Status: needs-triage
Category: refactor
Severity: low
Blocked by: 04
Effort: [architecture-deepening-3](../spec.md)

**What to build:** The DSL parser keywords, the suggestion keywords, the `IFF` and `XNOR` alias copies, and the reader's quantifier words read from the operator table and `QuantifierText`, inside `TruthWeaver`. Facts the table lacks (`LET`, `IN`, the constants and their letters) stay in one named place beside it. `ReservedWords` is source-linked into the netstandard2.0 source generator; either link the table into it or keep the copy behind the completeness test (decide in this ticket).

**Needs:** an Agent Brief once ticket 04 lands.
