# 45 - The element projector and outer data sources run per distinct query, not as ADR-0010 and ADR-0006 decision 11 state

Status: needs-triage
Severity: low
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.8: The element projector runs once per distinct query, not once per element as ADR-0010 states

Location: `src/TruthWeaver/Registry/LazyElementDataSource.cs:19` (PLAUSIBLE, low)

LazyElementDataSource calls projector(item) on every QueryAsync and ScopeAsync. The ADR-0010 amendment says 'the delegate runs at the first query of that element'. The evaluator memoizes per (scope, reference), so each distinct query of one element builds a new source.

### Failure scenario

elementSource = item => JsonDataSource.Create(JsonSerializer.SerializeToNode(item)), and a body with three different from(line, ...) references over 10,000 lines. The code serializes 30,000 times instead of 10,000. A projector with side effects or non-deterministic output gives different terms of one element different views of the element.

### Expected behavior

Cache the projected source (or the projection exception) in LazyElementDataSource after the first call.

### Verification

not verified (medium or low)

## Finding F8.9: Outer data sources are queried again for each quantifier element, against ADR-0006 decision 11

Location: `src/TruthWeaver/Registry/CollectionBinding.cs:189` (PLAUSIBLE, low)

WithElementSource passes outer sources unchanged into each nested evaluator, but the answer memo key is (memoScope, reference) (Evaluator.cs:742), and memoScope differs per element. An outer from("cfg", "$.limit") inside a body is queried once per element. ADR-0006 decision 11 says each (source, query) pair is queried at most once within one evaluation.

### Failure scenario

FORALL(line IN lines, under(max: from("cfg", "$.limit"))) over 500 lines, with a live config source. That makes 500 backend calls. If the value changes during the evaluation, line 1 and line 400 are checked against different limits within one evaluation.

### Expected behavior

Memoize references to sources that the element binding does not shadow at the outer scope, or amend ADR-0006 decision 11 to state the per-element scope.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
