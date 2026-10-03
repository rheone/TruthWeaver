# 05: Collection predicates

**What to build:** Collection predicates the catalog lacks become registerable: `IsEmpty` and `IsNotEmpty`, `Contains` (the collection contains a literal value, distinct from `StringPredicates.Contains`), `In` and `NotIn`, and the count family (`CountEqual`, `CountLessThan`, `CountGreaterThan`, `CountLessThanOrEqual`, `CountGreaterThanOrEqual`). The meaning of `In`/`NotIn` (subset, intersection or scalar membership) and the `NotX` form come from the owner's answers in ticket 02. A null collection follows the catalog rules: the count and comparison families return `Unknown` for null and honour `NullBehavior`, while the emptiness tests stay definite as `SetEquals` treats a null collection today. Each predicate has XML docs, a schema description and README coverage, and argument kinds use the existing `LiteralKind` set.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Each listed predicate is registerable and returns the documented value for a null, empty, one-item and many-item collection
- [ ] `In` and `NotIn` match the recorded owner definition, with tests that distinguish it from the other readings
- [ ] Null handling matches the catalog rules and `NullBehavior`
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, Collection section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
