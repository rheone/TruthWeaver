# 43 - TryGetAsync does not catch a source exception, so its result-instead-of-throwing contract fails

Status: needs-triage
Severity: low
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.5: TryGetAsync does not catch a source exception, so its 'result instead of throwing' contract fails for a thrown backend failure

Location: `src/TruthWeaver/Building/DataSourceExtensions.cs:55` (PLAUSIBLE, low)

TryGetAsync awaits source.QueryAsync with no try/catch. ADR-0006 decision 9 and docs/data-sources.md:356-357 let a source throw, and the evaluator treats a throw as a SourceError (Evaluator.cs:767-784). The docs (lines 246-253) say GetAsync throws InvalidOperationException when the source fails, and that TryGetAsync returns a SourceError result instead of throwing.

### Failure scenario

An IDataSource over an HTTP config service throws HttpRequestException. TryGetAsync<long>("$.limits.age") propagates HttpRequestException instead of returning FailureKind = SourceError. GetAsync throws HttpRequestException, not the documented InvalidOperationException. The same source used as a rule variable gives Unknown plus a SourceError fault.

### Expected behavior

Catch non-cancellation exceptions (same filter as the evaluator) and return DataReadResult.Failure(SourceError, ...) with no data in the message.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
