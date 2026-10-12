namespace TruthWeaver.Evaluation;

using System.Runtime.CompilerServices;

/// <summary>
/// Runs one compiled rule over a stream of contexts for <c>CompiledRule.EvaluateManyAsync</c>. It keeps a window of at
/// most the degree of parallelism of evaluations in flight, in input order: the oldest one is awaited and yielded first,
/// and a new one starts only when a slot is free. So the order is stable, the cap holds, and the memory held is bounded
/// by the cap, whatever the order in which the evaluations finish.
/// </summary>
internal static class BatchEvaluator
{
    /// <summary>Evaluates <paramref name="rule"/> for each context and yields the decisions in input order.</summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="contexts">The contexts, read one at a time as slots become free.</param>
    /// <param name="services">The services passed to every evaluation.</param>
    /// <param name="dataSources">The data sources passed to every evaluation.</param>
    /// <param name="options">The options passed to every evaluation, so a timeout or fault budget applies per context.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time; already validated as 1 or more.</param>
    /// <param name="cancellationToken">Stops the batch; it also reaches every evaluation in flight.</param>
    /// <returns>The decisions, one per context, in input order.</returns>
    public static async IAsyncEnumerable<Decision> EvaluateAsync<TContext>(
        CompiledRule<TContext> rule,
        IAsyncEnumerable<TContext> contexts,
        IServiceProvider? services,
        DataSources? dataSources,
        EvaluationOptions? options,
        int maxDegreeOfParallelism,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // A linked source, so stopping early (the consumer breaks or one evaluation throws) can cancel the
        // evaluations still in flight without cancelling the caller's own token.
        using CancellationTokenSource batchCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken token = batchCancellation.Token;
        Queue<Task<Decision>> inFlight = new(maxDegreeOfParallelism);
        IAsyncEnumerator<TContext> enumerator = contexts.GetAsyncEnumerator(token);
        try
        {
            bool hasMore = true;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                while (hasMore && inFlight.Count < maxDegreeOfParallelism)
                {
                    hasMore = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    if (hasMore)
                    {
                        inFlight.Enqueue(
                            StartAsync(rule, enumerator.Current, services, dataSources, options, maxDegreeOfParallelism, token)
                        );
                    }
                }

                if (inFlight.Count == 0)
                {
                    yield break;
                }

                yield return await inFlight.Dequeue().ConfigureAwait(false);
            }
        }
        finally
        {
            // Whatever ended the batch, nothing it started may keep running unobserved.
            await batchCancellation.CancelAsync().ConfigureAwait(false);
            await ((Task)Task.WhenAll(inFlight)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Task<Decision> StartAsync<TContext>(
        CompiledRule<TContext> rule,
        TContext context,
        IServiceProvider? services,
        DataSources? dataSources,
        EvaluationOptions? options,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken
    )
    {
        // With one slot there is nothing to overlap, so the evaluation runs on the caller's flow. With more, each
        // evaluation goes to the thread pool, so a predicate that completes synchronously still runs in parallel.
        return maxDegreeOfParallelism == 1
            ? rule.EvaluateAsync(context, services, dataSources, options, cancellationToken)
            : Task.Run(() => rule.EvaluateAsync(context, services, dataSources, options, cancellationToken), cancellationToken);
    }
}
