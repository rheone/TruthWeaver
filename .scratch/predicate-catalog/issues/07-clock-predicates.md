# 07: Clock predicates

**What to build:** `AfterNow` and `BeforeNow` become registerable catalog predicates. The factory takes a `TimeProvider` at registration, with no ambient default, so the clock is a host-supplied dependency and tests can use a fake provider (catalog rule recorded in `CONTEXT.md`). Decide and document whether "now" is read once per evaluation, so every term in one `Evaluate` call sees the same instant, or once per term; the recommendation is once per evaluation if the predicate contract allows it without an engine change, and the ticket reports back if it does not. The `NotX` twin rule applies: `AfterNow` and `BeforeNow` are not complements of each other (at the exact instant of "now" both are `False`), so each ships with its own registered twin, `NotAfterNow` and `NotBeforeNow`, defined as the K3 complement with `Unknown` staying `Unknown`. The twins take the same `TimeProvider` at registration. The members live in `DateTimePredicates`. A null selection returns `Unknown` and honours `NullBehavior`. XML docs, schema description and README coverage as for the other predicates.

**Blocked by:** 06

**Status:** done

- [x] The failing test run is shown before the implementation
- [x] The factory requires a `TimeProvider`, and tests drive it with a fake provider across the boundary instant
- [x] `NotAfterNow` and `NotBeforeNow` are registered with a required `TimeProvider` and agree with the K3 complement of their positive form, including at the boundary instant and for `Unknown`
- [x] The read-once versus read-per-term behavior is documented and tested
- [x] No ambient clock is read anywhere in the catalog
- [x] README and the gap list show the predicates as present
- [x] The full validation from CLAUDE.md passes

Source: [gap list, DateTimeOffset section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

- 2026-10-04: Done. `DateTimePredicates` adds `AfterNow`, `BeforeNow`, `NotAfterNow` and `NotBeforeNow`: `(name, selector, TimeProvider timeProvider, label, nullBehavior)`, no rule-text arguments, a null `timeProvider` throws `ArgumentNullException` at registration, and no ambient clock is read. Read-once versus read-per-term: the predicate delegate has no per-evaluation state, so the clock is read once per predicate evaluation, and the engine memoizes per term identity, so a repeated term sees one instant but different terms can differ. A shared snapshot needs an engine change; a host can supply a `TimeProvider` that returns a fixed instant per evaluation. Documented in the XML docs and `docs/predicates.md`, and tested. The compile-time diagnostic for reversed bounds is not part of this ticket (no bounds here). Tests: `ClockPredicatesTests` with a counting manual `TimeProvider` (no fake-time package is referenced).
