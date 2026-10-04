# 02: Full resolution rules: cardinality, conversion, failures, memoization

**What to build:** Every variable resolves or fails exactly as ADR-0006 decisions 6, 7, 9 and 11 specify. Scalar arguments need exactly one match, so zero or several matches are faults and the term is `Unknown`. Array arguments collect all matches, and zero matches give an empty array. All `LiteralKind` values work, with the agreed conversions and no wider ones. Errors returned as data and thrown exceptions, timeouts and cancellation become `Unknown` plus a `Fault`. Distinct fault kinds exist for missing, ambiguous, type mismatch, source error and unsupplied source. Each `(source, query)` pair, and each fault, is resolved once per evaluation.

**Blocked by:** 01

**Status:** done

- [x] Tests cover zero, one and many matches for scalar and array kinds, and every conversion allowed or refused in decision 7
- [x] A repeated reference queries its source once, including when the query faults
- [x] A source that throws, times out or is cancelled produces `Unknown` plus a fault, and evaluation continues where the logic allows (`Unknown OR True` is `True`)
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- No new public API beyond ticket 01. The behavior lives in `Evaluator` and the internal `VariableConversion`; the tests are `VariableResolutionTests` in `TruthWeaver.Testing.Tests`.
- Fault kinds are `VariableFailureKind.Missing`, `Ambiguous`, `TypeMismatch`, `SourceError` and `UnsuppliedSource`, carried by `VariableResolutionException` in `Fault.Exception`.
- A node a source cannot convert (`DataQueryErrorKind.UnsupportedType`) is reported as a type mismatch; a malformed query returned as data is a source error.
- A term with several failing variables records one fault per failing variable. A repeated failing reference is not re-queried; each term that uses it still records its own fault.
- "Cancelled": a source that throws its own `OperationCanceledException` or `TimeoutException` while the evaluation's token is untouched is a source-error fault. Cancellation of the evaluation (caller token or `EvaluationOptions.Timeout`) propagates as `OperationCanceledException`, the same behavior as for a predicate (ADR-0002).
