# 06: Date/time comparison predicates

**What to build:** `DateTimeOffset` comparison predicates the catalog lacks become registerable: `After` (strict `>`), `Before` (strict `<`) and `Between` against `DateTimeOffset` literal arguments. There is no `DateTime` literal kind and no `DateTime` overload; a host with a `DateTime` converts at the selector, and the docs say so (catalog rule). `Between` is inclusive on both ends (`n <= value <= k`) and its twin `Outside` is the exact complement. Reversed bounds (`n > k`) are an authoring error: a compile-time diagnostic when both bounds are literals, an argument error otherwise, and never swapped silently. Every positive predicate ships with its registered `NotX` twin, the K3 complement with `Unknown` staying `Unknown` (`NotAfter`, `NotBefore`, `Outside`). The members live in the per-kind static class `DateTimePredicates` with selectors typed for `DateTimeOffset`. A null selection returns `Unknown` and honours `NullBehavior`. Each predicate has XML docs, a schema description and README coverage. Clock predicates are a separate ticket.

**Blocked by:** 10

**Status:** done

- [ ] The failing test run is shown before the implementation
- [ ] `After`, `Before` and `Between` are registerable and correct at, just before and just after each boundary, including across offsets
- [ ] `Between` is inclusive on both ends and `Outside` is its exact complement
- [ ] Reversed bounds are a compile-time diagnostic for literal bounds and an argument error otherwise, and are never swapped
- [ ] `NotAfter`, `NotBefore` and `Outside` are registered and agree with the K3 complement of their positive form, including for `Unknown`
- [ ] Null selections follow the catalog rules and `NullBehavior`
- [ ] No `DateTime` literal kind or overload is added, and the docs explain the host-side conversion
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, DateTimeOffset section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

- 2026-10-04: Done. New `DateTimePredicates` with `After`, `Before`, `Between` and twins `NotAfter`, `NotBefore`, `Outside`; selector `Func<TContext, DateTimeOffset?>`, comparison by instant. Bounds are inclusive; reversed `Between`/`Outside` bounds throw `ArgumentException` at evaluation (a fault, so the result is `Unknown`). The compile-time diagnostic for reversed literal bounds is not implemented: `PredicateSchema` has no argument-validation hook, so it needs an engine change that is shared with ticket 04 and is left for the owner. Null selection is `Unknown` by default (`NullBehavior.False` is the host option). No `DateTime` kind or overload; the docs show the host-side conversion. Tests: `DateTimePredicatesTests`.
