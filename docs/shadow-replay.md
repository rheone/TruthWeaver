# Shadow replay

`ShadowReplay.CompareAsync` runs a sequence of contexts through two versions of a rule and lists the contexts whose decision changed. Each change holds the decision and the trace of both versions. Back to the [README](../README.md).

Use it to check a rule change against recorded contexts before the change ships.

## Compare two versions

Pass the rule in use now (the baseline), the rule under test (the candidate) and the contexts. The contexts can be an `IEnumerable<TContext>` or an `IAsyncEnumerable<TContext>`:

```csharp
ShadowReplayResult<Order> result = await ShadowReplay.CompareAsync(
    currentRule,
    newRule,
    recordedOrders,
    serviceProvider,
    cancellationToken: ct);

foreach (ShadowReplayChange<Order> change in result.Changes)
{
    // change.Baseline and change.Candidate are the two decisions, each with its trace.
}
```

Both rules must use the same context type. Each version evaluates every context with [`EvaluateManyAsync`](batch-evaluation.md), so both get the same services, data sources and evaluation options.

## Parameters

| Parameter | Default | Meaning |
| --- | --- | --- |
| `baseline`, `candidate` | Required | The two rule versions. |
| `contexts` | Required | The contexts. The method reads them lazily, in blocks. |
| `services` | `null` | The service provider for every evaluation. |
| `dataSources` | `null` | The data sources for every evaluation. |
| `evaluationOptions` | `null` | The `EvaluationOptions` for both versions. `IncludeTrace` is always on for the replay. |
| `options` | `null` | The `ShadowReplayOptions` that say what counts as a change. |
| `maxDegreeOfParallelism` | `1` | The largest number of evaluations in flight at one time for each version. |
| `cancellationToken` | `default` | Stops the replay. |

## What counts as a change

A context is a change when `Decision.Result` differs between the two versions. `ShadowReplayOptions` changes this:

| Option | Default | Effect |
| --- | --- | --- |
| `Comparison` | `ShadowReplayComparison.Result` | `Result` reports any change among `True`, `False` and `Unknown`. `IsSatisfied` reports only a change to or from `True`, so a change between `False` and `Unknown` is not a change. |
| `IncludeFaultChanges` | `false` | When `true`, a context whose set of faults differs is also a change, even if the comparison finds no difference. |

Two faults are the same when they have the same term, exception type and message.

## The result

| Member | Content |
| --- | --- |
| `ContextCount` | The number of contexts replayed. |
| `UnchangedCount` | The number of contexts that did not change. |
| `Changes` | One `ShadowReplayChange<TContext>` for each changed context, in the order of the contexts. |

`ShadowReplayChange<TContext>` has `Index` (the position of the context), `Context`, `Baseline` and `Candidate` (the two `Decision` values, with `Trace` and `TraceTree`), and the flags `ResultChanged`, `SatisfactionChanged` and `FaultsChanged`.

The replay keeps only the changed contexts. Its memory grows with the number of changes, not with the number of contexts.

## Errors

A `null` rule or sequence, a `maxDegreeOfParallelism` below 1 or a `FaultBudget` or `MaxElementEvaluations` below 1, or an unusable `Timeout`, throws `ArgumentNullException` or `ArgumentOutOfRangeException`. A cancelled token, or an expired `Timeout` for one evaluation, throws `OperationCanceledException`. A predicate fault is not an error: it makes that decision `Unknown` with a `Fault`, as in [batch evaluation](batch-evaluation.md).
