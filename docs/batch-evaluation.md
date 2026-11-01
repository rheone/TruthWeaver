# Batch evaluation

`CompiledRule.EvaluateManyAsync` evaluates one compiled rule against many contexts and returns one `Decision` for each context, in the order of the contexts. Back to the [README](../README.md).

## Evaluate a sequence of contexts

Pass the contexts as an `IEnumerable<TContext>` or an `IAsyncEnumerable<TContext>`. The method returns an `IAsyncEnumerable<Decision>`:

```csharp
await foreach (Decision decision in rule.EvaluateManyAsync(customers, serviceProvider, cancellationToken: ct))
{
    // One decision for each customer, in the order of customers.
}
```

To collect every decision in an array, use `ToArrayAsync` from `System.Linq`:

```csharp
Decision[] decisions = await rule.EvaluateManyAsync(customers, serviceProvider, cancellationToken: ct).ToArrayAsync(ct);
```

## Parameters

| Parameter | Default | Meaning |
| --- | --- | --- |
| `contexts` | Required | The contexts. The method reads them lazily, one context for each free slot. |
| `services` | `null` | The service provider for every evaluation, as for `EvaluateAsync`. |
| `dataSources` | `null` | The data sources for every evaluation. The evaluations only read them. |
| `options` | `null` | The `EvaluationOptions` for every evaluation. |
| `maxDegreeOfParallelism` | `1` | The largest number of evaluations in flight at one time. |
| `cancellationToken` | `default` | Stops the batch. Each evaluation in flight gets the token too. |

## Behavior

- **Order.** The decisions come in the order of the contexts. A decision that finishes early waits for the decisions before it, so the order never depends on timing.
- **Parallelism.** No more than `maxDegreeOfParallelism` evaluations are in flight at one time. The default of `1` evaluates one context after the other. A value above `1` calls the predicates and the service provider from several threads at once, so use it only when they are thread-safe.
- **Options per context.** Each context is one `EvaluateAsync` call with the same options. `Timeout` and `FaultBudget` apply to each context separately: a fault budget of 1 stops the evaluation of one context at its first fault, and the next context starts with no faults.
- **Faults.** A predicate fault in one context makes that decision `Unknown` with a `Fault`. The other contexts are evaluated as usual. The [evaluation rules](strong-k3/specification/evaluation.md) define faults.
- **Cancellation.** A cancelled token stops the batch and throws `OperationCanceledException`, and the evaluations in flight stop. An expired `Timeout` for one context also throws `OperationCanceledException` and stops the batch. Neither is a fault.
- **Early stop.** When the consumer stops reading, or one evaluation throws, the method cancels the evaluations still in flight and waits for them before the enumeration ends.
- **Argument errors.** A `null` sequence, a `maxDegreeOfParallelism` below 1 or a `FaultBudget` or `MaxElementEvaluations` below 1, or an unusable `Timeout` (negative other than `Timeout.InfiniteTimeSpan`, or over 4,294,967,294 ms), throws `ArgumentOutOfRangeException` when the method is called, before the method reads a context.

## Cost

With one evaluation at a time, `EvaluateManyAsync` costs about the same as a `foreach` loop of `EvaluateAsync` calls. The `BatchEvaluationBenchmarks` class in [Benchmarks](benchmarks.md) compares the two.
