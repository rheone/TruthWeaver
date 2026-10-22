# 62 - The predicates index says a null element has no effect outside HasCount, but IsSubsetOf and SetEquals answer False

Status: needs-triage
Severity: low
Lens: F6 Predicates A
Verdict: PLAUSIBLE
Blocked by: none

## Finding F6.2: Predicates index says a null element has no effect outside HasCount, but IsSubsetOf and SetEquals answer False because of one

Location: `docs/strong-k3/predicates/README.md:196` (PLAUSIBLE, low)

The index says a repeated element and a `null` element 'have no effect on the other predicates'. For IsSubsetOf, IsNotSubsetOf, SetEquals and NotSetEquals, a null element changes the answer. The code (CollectionPredicates.cs:473 `selected.All(set.Contains)` and :983-987 `HashSet.SetEquals`) and those predicates' own pages (collection-issubsetof.md:61/69, collection-setequals.md:62/73) agree on this. The index sentence is the wrong side.

### Failure scenario

A reader trusts the index and expects `tagsIsSubsetOf(values: ["admin"])` with `tags = ["admin", null]` to be True, because the null element supposedly has no effect. The engine answers False, as does `tagsSetEquals(values: ["admin"])` for the same input.

### Expected behavior

The index sentence limits 'no effect' to Contains/ContainsAny/ContainsAll and their twins. It states that a null element is an extra member for IsSubsetOf and SetEquals and their twins.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
