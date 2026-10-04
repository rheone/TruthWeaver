# 10: Amend predicate tickets 03-08 for the NotX twins and decided rules

**What to build:** Each family ticket carries the decided rules so an agent working it needs no further decision: every predicate it adds ships with its `NotX` twin, ticket 05 (collections) lists `ContainsAny`, `ContainsAll` and `IsSubsetOf` and makes `In` over a collection a compile error, tickets 04 and 06 use inclusive bounds with reversed bounds as an error, new families use the per-kind static classes, and ticket 07 (clock predicates) is checked for whether twins apply and says so.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Tickets 03, 04, 05, 06, 07 and 08 each state the twin requirement or why it does not apply
- [ ] Ticket 05 lists the three collection predicates and the `In` compile error
- [ ] Tickets 04 and 06 state inclusive bounds and the reversed-bounds error
- [ ] No ticket still says it is blocked on ticket 02

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
