# 05: Collection predicates

**What to build:** Collection predicates the catalog lacks become registerable: `IsEmpty` and `IsNotEmpty`, `Contains` (the collection contains a literal value, distinct from `StringPredicates.Contains`), `ContainsAny` (at least one element is in the candidate array), `ContainsAll` (every candidate is an element), `IsSubsetOf` (every element is in the candidate array), and the count family (`CountEqual`, `CountLessThan`, `CountGreaterThan`, `CountLessThanOrEqual`, `CountGreaterThanOrEqual`). `In` and `NotIn` are scalar-only membership (CONTEXT.md): a collection selector passed to `In` or `NotIn` is a compile error, and the three collection predicates above replace it. Every positive predicate here ships with its registered `NotX` twin, the K3 complement with `Unknown` staying `Unknown` (`IsNotEmpty` twins `IsEmpty`; `ContainsAny`, `ContainsAll`, `IsSubsetOf`, `Contains` and each count predicate get a `NotX` twin). A null collection follows the catalog rules: the count and comparison families return `Unknown` for null and honour `NullBehavior`, while the emptiness tests stay definite as `SetEquals` treats a null collection today. Each predicate has XML docs, a schema description and README coverage, and argument kinds use the existing `LiteralKind` set.

**Blocked by:** 10

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Each listed predicate is registerable and returns the documented value for a null, empty, one-item and many-item collection
- [ ] `ContainsAny`, `ContainsAll` and `IsSubsetOf` are registerable, each defined in one line in its XML docs, with tests that distinguish the three
- [ ] `In` and `NotIn` over a collection selector are a compile error
- [ ] Every positive predicate has a registered `NotX` twin that agrees with its K3 complement, including for `Unknown`
- [ ] Null handling matches the catalog rules and `NullBehavior`
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, Collection section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
