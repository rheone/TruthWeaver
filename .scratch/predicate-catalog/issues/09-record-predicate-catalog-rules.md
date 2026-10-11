# 09: Record the predicate-catalog rules the owner decided

**What to build:** The four deferred catalog questions have recorded owner answers, so the family tickets can proceed. The glossary's predicate catalog rules state: (2) every positive predicate has a registered first-class `NotX` twin defined as the Strong Kleene complement (Unknown stays Unknown); (4) `In` and `NotIn` are scalar-only membership and a collection selector is a compile error, with `ContainsAny`, `ContainsAll` and `IsSubsetOf` as the collection predicates, each defined in one line and each with a twin; (7) `Between` is inclusive on both ends, `Outside` is its exact complement, and reversed bounds are an authoring error (a compile-time diagnostic when the bounds are literals, an argument error otherwise) and are never swapped silently; (8) new families are per-kind static classes (`NumericPredicates`, `DateTimePredicates`, `TypePredicates`) with selectors typed for their kind. The gap list marks the four questions resolved and updates the per-predicate notes that referred to them, and ticket 02 is closed.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] The glossary states the four rules beside the existing predicate catalog rules Note 2026-10-10: the glossary text was not re-checked.
- [x] The gap list marks questions 2, 4, 7 and 8 resolved and its per-predicate notes agree
- [x] Ticket 02 is marked resolved, pointing at this ticket
- [x] The doctest harness still passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).

## Comments

- 2026-10-04: The four rules are in `CONTEXT.md` under Predicate catalog rules. The gap list marks questions 2, 4, 7 and 8 resolved and its per-predicate notes agree. Ticket 02 points here.
