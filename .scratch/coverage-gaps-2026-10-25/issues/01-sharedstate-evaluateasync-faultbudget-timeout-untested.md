# 01 - CompiledRule's shared-state EvaluateAsync overload has no test for its Timeout branch

Status: needs-triage
Severity: high
Slice: Evaluation (src/TruthWeaver/Evaluation/CompiledRule.Evaluation.cs)
Lens: fault-path
Signal: line, branch

## Gap

`CompiledRule<TContext>.EvaluateAsync(TContext, EvaluationState, ...)` at
`src/TruthWeaver/Evaluation/CompiledRule.Evaluation.cs:189-232` is the `internal` overload used by
`TruthWeaver.Policies` (via `InternalsVisibleTo`) so several rules can share one `EvaluationState` and term
memo when evaluated against the same context (feature backlog 08). Its `FaultBudget` guard (line 203) and
its no-timeout branch (lines 221-231) are covered, but the `EvaluationOptions.Timeout`-present branch
(lines 206-218) is entirely uncovered: nothing creates the linked, auto-cancelling
`CancellationTokenSource` (line 206-207) or constructs the timed `Evaluator<TContext>` (lines 208-217) via
this specific overload. Branch 204 (`if (effectiveOptions.Timeout is { } timeout)`) is only 50% covered -
only the "no timeout" side is exercised through this overload.

ADR-0001/0002's timeout semantics are tested for the public per-rule overload (which delegates to
`BatchEvaluator`), but not for the shared-state path `TruthWeaver.Policies` actually calls.

## Missing scenario

A test that calls the shared-state overload (reachable via `TruthWeaver.Policies`' `InternalsVisibleTo`, or
a same-assembly test) with `EvaluationOptions.Timeout` set short enough to expire before a deliberately
slow predicate completes, asserting `OperationCanceledException` and that no fault is recorded for it
(ADR-0001/0002), through this overload specifically.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line/branch coverage signal clears for `CompiledRule.Evaluation.cs:206-218` and branch `204`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
