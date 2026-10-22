# 31 - NoMatch, AmbiguousMatch and non-scalar matches are classified as SourceError or TypeMismatch inconsistently between TryGetAsync/GetAsync, variables and the evaluator

Status: needs-triage
Severity: medium
Lens: F8 Predicate+DataSource concepts; L5 Least surprise
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.1: TryGetAsync/GetAsync classify an object, array or null match as SourceError; a variable classifies the same match as TypeMismatch

Location: `src/TruthWeaver/Building/DataSourceExtensions.cs:56` (PLAUSIBLE, medium)

TryGetAsync maps every DataQueryResult error kind, UnsupportedType included, to VariableFailureKind.SourceError. The evaluator maps UnsupportedType to TypeMismatch (Evaluator.cs:801-804). docs/data-sources.md:246-252 says GetAsync 'converts like a variable would' and lists TypeMismatch as a failure kind. So the eager read and the deferred read disagree on the same data.

### Failure scenario

JsonDataSource.Parse("{\"limits\":{\"age\":null}}").TryGetAsync<long>("$.limits.age") returns FailureKind = SourceError. A rule term ageAtLeast(min: from("cfg", "$.limits.age")) against the same document records a TypeMismatch fault. Host code that branches on FailureKind (for example, retry on SourceError) retries a data problem that can never succeed.

### Expected behavior

Map UnsupportedType to TypeMismatch in TryGetAsync, as Evaluator.QuerySourceUncachedAsync does, so both paths report one kind.

### Verification

not verified (medium or low)

## Finding L5.6: A data source that answers QueryAsync with NoMatch or AmbiguousMatch is reported as a source error, not missing/ambiguous

Location: `src/TruthWeaver/Evaluation/Evaluator.cs:801` (PLAUSIBLE, low)

`QuerySourceUncachedAsync` maps every `DataQueryErrorKind` other than `UnsupportedType` to `VariableFailureKind.SourceError` (Evaluator.cs:801-807), so a custom `IDataSource` that returns `DataQueryResult.Failure(DataQueryErrorKind.NoMatch, ...)` or `AmbiguousMatch` from `QueryAsync` (the API accepts any kind) gets the trace word "source error" and a `SourceError` fault instead of "missing"/"ambiguous". The enum doc (DataQueryErrorKind.cs:15-19) says those two kinds are for `ScopeAsync`, but nothing in `DataQueryResult.Failure` enforces it and docs/data-sources.md "Writing your own data source" does not say to return `Empty()` for no match.

### Failure scenario

A host writes a database-backed source that returns `Failure(NoMatch, "no row")` when a scalar query finds nothing. The trace reads `[from("db", "$.x"): source error]` and `Fault.Exception.Kind` is `SourceError`; an operator triages it as an outage rather than missing data, which is the documented `Missing` outcome for the same situation in the JSON source.

### Expected behavior

Either map `NoMatch` to `Missing` and `AmbiguousMatch` to `Ambiguous`, or document in `DataQueryResult.Failure` and the data-sources guide that a query source must answer no match with `Empty()` and that those two kinds are scope-only.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
