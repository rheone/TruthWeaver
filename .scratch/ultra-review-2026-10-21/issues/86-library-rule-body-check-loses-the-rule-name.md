# 86 - A shadowed variable or depth error inside a library rule in a quantifier body is reported without the library rule's name

Status: needs-triage
Category: bug
Severity: low
Lens: Follow-up to ticket 79
Blocked by: none

## Finding

Ticket 79 makes a library rule used inside a `FORALL` or `EXISTS` body resolve against that body's element registry. The agent noted that the check build of such a library rule runs at quantifier depth 0 with no inherited variables. A shadowed-variable or quantifier-depth error inside that library rule is reported by the caller's build without the library rule's name (the locator from ticket 83 may not apply to it).

## Open question for the owner

Should the check build inherit the enclosing quantifier depth and bound variables, so `ShadowedVariable` and `MaxQuantifierDepth` apply to the library rule as if its text were written in place, and the message names the library rule?

## Next step

Decide, then write an Agent Brief.
