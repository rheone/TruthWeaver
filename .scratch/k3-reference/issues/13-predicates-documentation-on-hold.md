# 13: Predicates documentation (on hold)

**What to build:** Hold. Document the predicates category once the predicates have been implemented: how a predicate returns a Strong K3 value, the null-selected-value rule, argument schemas, and one document per predicate (equality, numeric, string, collection, date/time, type tests) following the approved template. Do not write any predicate documentation before the predicates exist, so semantics are never invented. Owner decision 2026-10-03.

**Blocked by:** predicate implementations (the predicate catalog track) and 12; ticket 14 maintains it afterwards

**Status:** deferred

- [ ] Do not start until the predicate catalog implementation work has landed and the owner releases this hold
- [ ] When released, each predicate document passes the harness and conforms to the template

Source: [predicate gap list](../../predicate-catalog/k3-gap-list.md). See also [spec](../spec.md).

## Comments

- 2026-10-04: The owner released the hold. Numeric (14) and scalar (8) families are documented and off the hold list; the other families remain. Convention: one page per factory method, named `<kind>-<factory>.md`, and every NotX twin has its own page (the sync check requires one document per factory stem). Each page links its twin. Shared rules (null selected values, twins, argument kinds) are stated once in `docs/strong-k3/predicates/README.md`. The harness checks predicate pages for links only, so the Answers and Examples tables were verified against the engine with a temporary generated test that is not kept.
