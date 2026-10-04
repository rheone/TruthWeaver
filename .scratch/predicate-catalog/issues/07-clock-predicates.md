# 07: Clock predicates

**What to build:** `AfterNow` and `BeforeNow` become registerable catalog predicates. The factory takes a `TimeProvider` at registration, with no ambient default, so the clock is a host-supplied dependency and tests can use a fake provider (catalog rule recorded in `CONTEXT.md`). Decide and document whether "now" is read once per evaluation, so every term in one `Evaluate` call sees the same instant, or once per term; the recommendation is once per evaluation if the predicate contract allows it without an engine change, and the ticket reports back if it does not. A null selection returns `Unknown` and honours `NullBehavior`. XML docs, schema description and README coverage as for the other predicates.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] The factory requires a `TimeProvider`, and tests drive it with a fake provider across the boundary instant
- [ ] The read-once versus read-per-term behavior is documented and tested
- [ ] No ambient clock is read anywhere in the catalog
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, DateTimeOffset section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
