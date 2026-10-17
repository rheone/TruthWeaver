namespace TruthWeaver.Evaluation;

/// <summary>
/// Runs a sequence of contexts through two versions of a rule and lists the contexts whose decision changed, each with
/// the decision and trace of both versions. Use it to check a rule change against recorded contexts before it ships.
/// </summary>
/// <remarks>
/// Each version evaluates every context with <c>CompiledRule&lt;TContext&gt;.EvaluateManyAsync</c>, so the same services,
/// data sources and options apply to both. The decisions always carry the flat trace
/// (<see cref="EvaluationOptions.IncludeTrace"/> is set for the replay). The replay keeps only the changed contexts,
/// so memory grows with the number of changes, not with the number of contexts.
/// </remarks>
public static class ShadowReplay
{
    // Contexts are replayed in blocks so a long or lazy sequence is never held whole in memory.
    private const int BlockSize = 64;

    /// <summary>Replays <paramref name="contexts"/> through <paramref name="baseline"/> and <paramref name="candidate"/>.</summary>
    /// <typeparam name="TContext">The context type of both rules.</typeparam>
    /// <param name="baseline">The rule version in use now.</param>
    /// <param name="candidate">The rule version under test.</param>
    /// <param name="contexts">The contexts to replay, read lazily.</param>
    /// <param name="services">The service provider for every evaluation, or <see langword="null"/>.</param>
    /// <param name="dataSources">The data sources for every evaluation, or <see langword="null"/>.</param>
    /// <param name="evaluationOptions">The evaluation options for both versions, or <see langword="null"/> for the defaults.</param>
    /// <param name="options">What counts as a change, or <see langword="null"/> for a changed <see cref="Decision.Result"/>.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time for each version. The default is 1.</param>
    /// <param name="cancellationToken">A token that stops the replay.</param>
    /// <returns>The number of contexts replayed and the contexts that changed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="baseline"/>, <paramref name="candidate"/> or <paramref name="contexts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is below 1, or <paramref name="evaluationOptions"/> has a fault budget below 1.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the replay, or a timeout expired for one evaluation.</exception>
    public static Task<ShadowReplayResult<TContext>> CompareAsync<TContext>(
        CompiledRule<TContext> baseline,
        CompiledRule<TContext> candidate,
        IEnumerable<TContext> contexts,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? evaluationOptions = null,
        ShadowReplayOptions? options = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contexts);
        return CompareAsync(
            baseline,
            candidate,
            contexts.ToAsyncEnumerable(),
            services,
            dataSources,
            evaluationOptions,
            options,
            maxDegreeOfParallelism,
            cancellationToken
        );
    }

    /// <summary>Replays an asynchronous stream of contexts through <paramref name="baseline"/> and <paramref name="candidate"/>.</summary>
    /// <typeparam name="TContext">The context type of both rules.</typeparam>
    /// <param name="baseline">The rule version in use now.</param>
    /// <param name="candidate">The rule version under test.</param>
    /// <param name="contexts">The contexts to replay, read lazily with <paramref name="cancellationToken"/>.</param>
    /// <param name="services">The service provider for every evaluation, or <see langword="null"/>.</param>
    /// <param name="dataSources">The data sources for every evaluation, or <see langword="null"/>.</param>
    /// <param name="evaluationOptions">The evaluation options for both versions, or <see langword="null"/> for the defaults.</param>
    /// <param name="options">What counts as a change, or <see langword="null"/> for a changed <see cref="Decision.Result"/>.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time for each version. The default is 1.</param>
    /// <param name="cancellationToken">A token that stops the replay.</param>
    /// <returns>The number of contexts replayed and the contexts that changed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="baseline"/>, <paramref name="candidate"/> or <paramref name="contexts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is below 1, or <paramref name="evaluationOptions"/> has a fault budget below 1.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the replay, or a timeout expired for one evaluation.</exception>
    public static async Task<ShadowReplayResult<TContext>> CompareAsync<TContext>(
        CompiledRule<TContext> baseline,
        CompiledRule<TContext> candidate,
        IAsyncEnumerable<TContext> contexts,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? evaluationOptions = null,
        ShadowReplayOptions? options = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);
        if (options is { Comparison: not (ShadowReplayComparison.Result or ShadowReplayComparison.IsSatisfied) })
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Comparison, "Unhandled comparison.");
        }

        ShadowReplayOptions effective = options ?? new ShadowReplayOptions();
        EvaluationOptions evaluation = (evaluationOptions ?? EvaluationOptions.Default) with { IncludeTrace = true };
        if (evaluation.FaultBudget is < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(evaluationOptions),
                evaluation.FaultBudget,
                "The fault budget must be 1 or more."
            );
        }

        List<ShadowReplayChange<TContext>> changes = [];
        List<TContext> block = [];
        int count = 0;

        await foreach (TContext context in contexts.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            block.Add(context);
            if (block.Count == BlockSize)
            {
                count = await ReplayBlockAsync(count).ConfigureAwait(false);
            }
        }

        if (block.Count > 0)
        {
            count = await ReplayBlockAsync(count).ConfigureAwait(false);
        }

        return new ShadowReplayResult<TContext>(count, changes);

        // Evaluates the current block with both versions, records the changed contexts and returns the new total.
        async Task<int> ReplayBlockAsync(int start)
        {
            Decision[] before = await baseline
                .EvaluateManyAsync(block, services, dataSources, evaluation, maxDegreeOfParallelism, cancellationToken)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            Decision[] after = await candidate
                .EvaluateManyAsync(block, services, dataSources, evaluation, maxDegreeOfParallelism, cancellationToken)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            for (int i = 0; i < block.Count; i++)
            {
                if (IsChange(before[i], after[i], effective))
                {
                    changes.Add(new ShadowReplayChange<TContext>(start + i, block[i], before[i], after[i]));
                }
            }

            int total = start + block.Count;
            block.Clear();
            return total;
        }
    }

    /// <summary>Compares the fault sets of two decisions. A fault equals another with the same term, exception type and message.</summary>
    /// <param name="first">The first decision.</param>
    /// <param name="second">The second decision.</param>
    /// <returns><see langword="true"/> when the two decisions record different faults.</returns>
    internal static bool FaultsDiffer(Decision first, Decision second)
    {
        return !first.Faults.ToHashSet().SetEquals(second.Faults);
    }

    private static bool IsChange(Decision before, Decision after, ShadowReplayOptions options)
    {
        bool decisionChanged = options.Comparison switch
        {
            ShadowReplayComparison.IsSatisfied => before.IsSatisfied != after.IsSatisfied,
            _ => before.Result != after.Result,
        };
        return decisionChanged || (options.IncludeFaultChanges && FaultsDiffer(before, after));
    }
}
