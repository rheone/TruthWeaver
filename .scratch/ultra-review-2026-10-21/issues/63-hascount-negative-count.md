# 63 - Collection HasCount predicates accept a negative count with no diagnostic

Status: needs-triage
Severity: low
Lens: F6 Predicates A
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a throwaway xUnit test confirmed `CollectionPredicates.HasCountLessThan`'s schema has `ArgumentValidator == null`, and evaluating it with `count: -1` against `["a"]` returns `False` (and `HasCountGreaterThanOrEqualTo` with `count: -1` returns `True`), with no compile or evaluation diagnostic either way.
Blocked by: none

## Finding F6.3: Collection HasCount predicates accept a negative count with no diagnostic

Location: `src/TruthWeaver.Predicates/CollectionPredicates.cs:614` (PLAUSIBLE, low)

The ten HasCount* factories (lines 534-862) build the schema through `Build`, which sets no `ArgumentValidator`. A negative `count` literal compiles cleanly. Because `Count` is never negative, the term is then a constant for every non-null collection. Range predicates catch a comparable authoring error (reversed bounds) with TRE0026, but this one passes silently. The docs do not say what a negative count means.

### Failure scenario

`tagsHasCountLessThan(count: -1)` compiles with no diagnostic and answers False for every non-null collection. `tagsHasCountGreaterThanOrEqualTo(count: -1)` answers True for every non-null collection, so a typo turns a gate into an always-pass rule, with no error at compile or evaluation time.

### Expected behavior

A negative literal count is reported at compile time through an ArgumentValidator (for example TRE0026, as for reversed bounds). A negative count from a data source is an argument fault. Otherwise the docs state the constant-answer behavior.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
