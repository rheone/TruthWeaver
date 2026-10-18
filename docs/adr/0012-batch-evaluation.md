# ADR-0012: Batch evaluation and bounded parallelism

## Status

Accepted (2026-10-17). Records a feature that shipped without an ADR (`CompiledRule.EvaluateManyAsync` and its
`maxDegreeOfParallelism` parameter), as the owner decided on 2026-10-17 in
`.scratch/main-review-2026-10-17/issues/08-shipped-surface-without-adr.md`. It changes no code. It states how the
feature fits [ADR-0002](0002-evaluation-semantics.md), and amends that ADR in place with a dated note.

## Context

[ADR-0002](0002-evaluation-semantics.md) says two things that this feature touches:

- "The caller loops, rather than the engine offering a bulk/partial-evaluation API in v1." Evaluating one rule for many
  candidate contexts (every user in a list, every row of an export) is a common job, and every caller wrote the same
  loop.
- "Sibling operands are **not** evaluated concurrently in v1", because concurrency costs deterministic fault ordering,
  straightforward cancellation and a trace that reads in the written order.

A batch call raises two questions. Is it the bulk API that ADR-0002 deferred? And does running several contexts at once
break the "no implicit concurrency" rule?

## Decision

### 1. A batch is a loop over independent evaluations, not a new evaluation mode

`EvaluateManyAsync` takes a sequence of contexts (`IEnumerable<TContext>` or `IAsyncEnumerable<TContext>`) and yields
one `Decision` per context, in input order. Each context is exactly one `EvaluateAsync` call with the same services,
data sources and options. The batch has no semantics of its own: no shared memo, no shared fault list, no cross-context
short-circuit, no partial evaluation. It is the loop that ADR-0002 expects the caller to write, written once.

This is not the "bulk/partial-evaluation API" that ADR-0002 defers. Partial evaluation (fixing some terms and
simplifying the rule for the rest) remains deferred.

### 2. Concurrency is across contexts only, bounded, and opt-in

`maxDegreeOfParallelism` is the largest number of evaluations in flight at one time. It defaults to `1`, and a value
below `1` is rejected when the method is called. With `1` the evaluations run one after the other on the caller's flow,
so a caller that does not ask for concurrency gets none. With more, each evaluation starts on the thread pool.

ADR-0002's rule is about the operands of one rule inside one evaluation, and it still holds: one evaluation is
sequential, left to right, with one fault list and one trace in written order. The batch overlaps whole evaluations,
which share no state because the evaluator, its memo and its fault list are created per call. The two properties that
ADR-0002 protects, deterministic fault order and a trace in written order, are properties of one decision, and they are
unchanged.

The concurrency is not implicit. A caller opts in with a number, and the documentation states the cost: the
predicates, the service provider and the data sources are then called from several threads at once, so they must be
thread-safe. The engine does not check this.

### 3. Output order never depends on timing

The batch keeps a window of at most `maxDegreeOfParallelism` tasks, in input order. It awaits the oldest one first, so
a decision that finishes early waits for the decisions before it. The cap holds, the memory held is bounded by the
cap, and the sequence is read lazily, one context per free slot. A stream of any length can be evaluated.

### 4. Options, faults and cancellation are per context

- **Options.** `EvaluationOptions` apply to each context separately. `Timeout` is a timeout for one context, and a
  `FaultBudget` of 1 stops one context at its first fault; the next context starts with none.
- **Faults.** A predicate fault in one context makes that decision `Unknown` with a `Fault`
  ([ADR-0001](0001-kleene-failure-model.md)). The other contexts are evaluated as usual.
- **Cancellation.** The caller's token stops the batch and reaches every evaluation in flight, as for
  [ADR-0002](0002-evaluation-semantics.md): it throws `OperationCanceledException` and is not a fault. An expired
  per-context `Timeout` also throws and stops the batch, because a timeout is the same kind of stop as a cancelled
  token. When the consumer stops reading, or one evaluation throws, the batch cancels the evaluations still in flight
  and waits for them before it ends, so nothing it started keeps running unobserved.
- **Argument errors.** A `null` sequence, a `maxDegreeOfParallelism` below 1 or a `FaultBudget` below 1 throws when the
  method is called, not at the first read.

## Consequences

- A host that evaluates one rule for many contexts calls one method, and the cost with parallelism 1 is that of a
  `foreach` loop of `EvaluateAsync` calls (a benchmark compares the two).
- The documentation (`docs/batch-evaluation.md`) must keep the thread-safety condition next to the parallelism
  parameter, because the default is safe and a larger value is not safe for every host.
- A timeout that expires for one context stops the whole batch. A host that wants to skip a slow context and continue
  must call `EvaluateAsync` itself, or set the timeout inside its own predicate, where it is a fault.
- Streaming results as an `IAsyncEnumerable<Decision>` means a consumer that is slower than the evaluations holds back
  the window, not the whole input. That is deliberate back-pressure.
- Parallel evaluation of the operands of one rule is still not offered. If it is ever added, it needs its own decision,
  because it changes the fault order and the trace order that ADR-0002 protects.

## Related

- [ADR-0001: Kleene failure model](0001-kleene-failure-model.md)
- [ADR-0002: Evaluation semantics](0002-evaluation-semantics.md)
- `docs/batch-evaluation.md`
