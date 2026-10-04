# 02: Full resolution rules: cardinality, conversion, failures, memoization

**What to build:** Every variable resolves or fails exactly as ADR-0006 decisions 6, 7, 9 and 11 specify. Scalar arguments need exactly one match, so zero or several matches are faults and the term is `Unknown`. Array arguments collect all matches, and zero matches give an empty array. All `LiteralKind` values work, with the agreed conversions and no wider ones. Errors returned as data and thrown exceptions, timeouts and cancellation become `Unknown` plus a `Fault`. Distinct fault kinds exist for missing, ambiguous, type mismatch, source error and unsupplied source. Each `(source, query)` pair, and each fault, is resolved once per evaluation.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Tests cover zero, one and many matches for scalar and array kinds, and every conversion allowed or refused in decision 7
- [ ] A repeated reference queries its source once, including when the query faults
- [ ] A source that throws, times out or is cancelled produces `Unknown` plus a fault, and evaluation continues where the logic allows (`Unknown OR True` is `True`)
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
