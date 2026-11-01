# 83 - Every node and diagnostic from a library rule points at the reference, not into the library text

Status: needs-triage
Category: enhancement
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

Ticket 11b-2 places every node and diagnostic that comes from a library rule at the span of the `rule("x")` reference in the caller's text. A rule with two predicates where one fails at analysis or evaluation then points both at the same span, so a caller cannot tell which part of the library rule is involved. The expanded body is also cached and shared across references, and the span rewrite copies only the top level of each shared node.

## Open question for the owner

Where should a trace entry or an analyzer diagnostic point for a node inside a library rule: the reference in the caller's text (today), the node's span in the library rule's own text (needs a way to name which rule), or both?

## Next step

Decide, then write an Agent Brief. Tickets 82 and 83 may merge, since both depend on how a library rule's diagnostics map back to text.
