# 83 - Every node and diagnostic from a library rule points at the reference, not into the library text

Status: ready-for-agent
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

## Decision (owner, 2026-11-01)

Keep the reference span in the caller's text, and add a library locator: the library rule's name and the node's offset inside the library text, in the diagnostic message and the trace entry. No new span type.

## Agent Brief

**Category:** enhancement
**Summary:** Make a diagnostic or trace entry for a node inside a library rule say which library rule and where, while its span stays at the `rule("x")` reference.

**Current behavior:** Every node and diagnostic from a library rule has the span of the reference. Two predicates in one library rule cannot be told apart.

**Desired behavior:** A diagnostic that originates in a library rule's node (an analyzer finding, a lint, a compile error from a nested node) carries the library rule's name and the node's offset (or line and column) inside the library text in its message. A trace entry for such a node carries the same locator in its text. The span stays the reference's span. No new public span type.

**Acceptance criteria:**
- [ ] A test fails before and passes after: a library rule with two predicates, one of which raises a diagnostic, gives a message that names the library rule and a position inside it.
- [ ] A trace entry for a node from a library rule names the library rule.
- [ ] A diagnostic from caller text is unchanged.
- [ ] Docs (`docs/rule-text.md` Library rules, `docs/strong-k3/specification/diagnostics.md`) state the locator.
- [ ] The validation list in CLAUDE.md passes, with a CHANGELOG entry.

**Out of scope:** Warnings from library rules (ticket 82, which needs this locator).
