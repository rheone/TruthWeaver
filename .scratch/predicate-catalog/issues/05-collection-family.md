# 05: Collection predicates

**What to build:** Collection predicates the catalog lacks become registerable: `IsEmpty` and `IsNotEmpty`, `Contains` (the collection contains a literal value, distinct from `StringPredicates.Contains`), `ContainsAny` (at least one element is in the candidate array), `ContainsAll` (every candidate is an element), `IsSubsetOf` (every element is in the candidate array), and the count family (`CountEqual`, `CountLessThan`, `CountGreaterThan`, `CountLessThanOrEqual`, `CountGreaterThanOrEqual`). `In` and `NotIn` are scalar-only membership (CONTEXT.md): a collection selector passed to `In` or `NotIn` is a compile error, and the three collection predicates above replace it. Every positive predicate here ships with its registered `NotX` twin, the K3 complement with `Unknown` staying `Unknown` (`IsNotEmpty` twins `IsEmpty`; `ContainsAny`, `ContainsAll`, `IsSubsetOf`, `Contains` and each count predicate get a `NotX` twin). A null collection follows the catalog rules: the count and comparison families return `Unknown` for null and honour `NullBehavior`, while the emptiness tests stay definite as `SetEquals` treats a null collection today. Each predicate has XML docs, a schema description and README coverage, and argument kinds use the existing `LiteralKind` set.

**Blocked by:** 10

**Status:** done

- [ ] The failing test run is shown before the implementation Note 2026-10-10: the failing-first run is not recorded in the repo, so it cannot be confirmed.
- [x] Each listed predicate is registerable and returns the documented value for a null, empty, one-item and many-item collection
- [x] `ContainsAny`, `ContainsAll` and `IsSubsetOf` are registerable, each defined in one line in its XML docs, with tests that distinguish the three
- [x] `In` and `NotIn` over a collection selector are a compile error
- [x] Every positive predicate has a registered `NotX` twin that agrees with its K3 complement, including for `Unknown`
- [x] Null handling matches the catalog rules and `NullBehavior`
- [x] README and the gap list show the predicates as present
- [x] The full validation from CLAUDE.md passes

Source: [gap list, Collection section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

- 2026-10-04: Done. `CollectionPredicates` gains `IsEmpty`, `Contains`, `ContainsAny`, `ContainsAll`, `IsSubsetOf`, scalar-`string` `In` and the five `Count*` predicates, each with a `NotX` twin (`IsNotEmpty`, `NotContains`, `NotContainsAny`, `NotContainsAll`, `IsNotSubsetOf`, `NotIn`, `NotCount*`). `In`/`NotIn` take a `string?` selector, so a collection selector fails to compile (no runtime diagnostic is needed). Emptiness is definite (null is empty, no `nullBehavior`); the other families default to `NullBehavior.Unknown` and the twin is the K3 complement of the configured null answer. Element type is `string` only. Tests: `CollectionFamilyPredicatesTests`. Docs: `docs/predicates.md`.
- 2026-10-04 bookkeeping: the boxes were ticked from what this comment records. No comment shows a red run before the implementation, so that box stays open. The full validation passed on the integrated branch (restore --locked-mode, build, test, csharpier, format, roslynator).
