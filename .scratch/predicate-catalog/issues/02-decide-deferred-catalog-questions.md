# 02: Decide the deferred predicate-catalog questions

**What to build:** The owner answers the four questions ticket 10 of k3-followups left deferred, and the answers are recorded so the implementation tickets can proceed: (2) whether each negated predicate (`NotX`) is a first-class registered predicate defined as the K3 complement, or is left to `NOT` in the rule; (4) the meaning of collection `In`/`NotIn` (subset, intersection or scalar membership); (7) whether `Between`/`Outside` bounds are inclusive or exclusive for numeric and date/time values, and the result for reversed bounds; (8) selector shapes (generic `Func<TContext, TValue?>` factories with per-kind overloads versus per-kind classes such as `NumericPredicates`, `DateTimePredicates` and `TypePredicates`). Use the recommendations in research findings section 7 as the starting point, one question at a time. This is an owner decision ticket, not agent-grabbable: it is worked in a grilling session with the owner, and an agent then records the outcome in `CONTEXT.md` (predicate catalog rules) and marks the questions resolved in the gap list.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] Questions 2, 4, 7 and 8 each have a recorded owner answer
- [ ] `CONTEXT.md` states the resulting rules beside the existing predicate catalog rules
- [ ] The gap list marks the four questions resolved and updates the per-predicate notes that referred to them
- [ ] Tickets 03 to 08 are re-read and amended where an answer changes their scope

Source: [gap list](../k3-gap-list.md), [research findings, section 7](../../k3-conformance/research-findings.md#7-predicate-catalog), k3-followups ticket 10.
