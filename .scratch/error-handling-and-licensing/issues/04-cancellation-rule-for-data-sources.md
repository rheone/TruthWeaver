# 04: Pin the cancellation rule for data sources

**What to build:** State one rule and test it: cancellation is decided by the evaluation's own token. If the evaluation token (the caller's, or the one linked to `EvaluationOptions.Timeout`) is cancelled, `OperationCanceledException` propagates. If a data source (or predicate) throws its own `OperationCanceledException` or `TimeoutException` while that token is live, the result is `Unknown` plus a fault. No custom exception type is added.

**Blocked by:** None (can start immediately)

**Status:** done

Conflict to resolve first: the project instructions and ADR-0001/0002 say predicate exceptions, timeouts and cancellation become `Unknown` plus a fault, while `Evaluator` rethrows `OperationCanceledException` when the token is cancelled (see the `catch (Exception ex) when (ex is not OperationCanceledException || !this.cancellationToken.IsCancellationRequested)` filters). Read both, decide which is intended for the evaluation-timeout case specifically, and align code and docs. If the documented behaviour changes, amend the ADR in place and mark it.

- [ ] The intended behaviour for caller cancellation, evaluation timeout, and a source's own cancellation/timeout is written once (ADR-0002 amendment or `CONTEXT.md`) and the docs agree
- [ ] One test per case, for a data source and for a predicate
- [ ] Faults and messages never contain resolved values
- [ ] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 4).
