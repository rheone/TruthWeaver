# 07 - A compile-scope value crosses quantifier bodies and library rules

Status: needs-triage
Category: refactor
Severity: medium
Blocked by: 02, 06
Effort: [architecture-deepening-3](../spec.md)

**What to build:** One compile-scope value carries depth, quantifier depth, bound variables, the node budget with its reported flag, and the library scope across `Compile`, `CompileBody` and the library rule check, replacing the four scope parameters that `Compile` and `CompileBody` both repeat. The library check passes the enclosing scope (this is ticket 86, which may land first or inside this ticket). The seven duplicate-diagnostic mechanisms stay; consolidating them is out of scope unless the scope value makes one redundant.

**Constraints:** ADR-0008, ADR-0010 and ADR-0019 (expand before build); the per-node work in Build allocates nothing; ticket 22 relies on separate node counts in the reader and the expander.

**Needs:** an Agent Brief once tickets 02 and 06 land.
