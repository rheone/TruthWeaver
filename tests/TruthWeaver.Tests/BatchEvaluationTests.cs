namespace TruthWeaver.Tests;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary><see cref="CompiledRule{TContext}.EvaluateManyAsync(IEnumerable{TContext}, IServiceProvider?, DataSources?, EvaluationOptions?, int, CancellationToken)"/>: one rule over many contexts.</summary>
public sealed class BatchEvaluationTests
{
    /// <summary>The decisions come back in the order of the contexts, even when later contexts finish first.</summary>
    [Fact]
    public async Task EvaluateManyAsync_LaterContextsFinishFirst_YieldsDecisionsInInputOrder_Test()
    {
        CompiledRule<BatchItem> rule = CompileProbe(
            async (item, ct) =>
            {
                // The first context is the slowest, so a result in completion order would be reversed.
                await Task.Delay(TimeSpan.FromMilliseconds((6 - item.Id) * 15), ct).ConfigureAwait(false);
                return ValueOf(item.Id);
            }
        );
        BatchItem[] items = [.. Enumerable.Range(0, 6).Select(id => new BatchItem(id))];

        Decision[] decisions = await rule.EvaluateManyAsync(
                items,
                maxDegreeOfParallelism: 6,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(items.Select(item => ValueOf(item.Id)), decisions.Select(decision => decision.Result));
    }

    /// <summary>No more evaluations than the cap run at the same time, and the cap is reached when there is enough work.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task EvaluateManyAsync_ParallelismCap_NeverRunsMoreEvaluationsThanTheCap_Test(int cap)
    {
        ConcurrencyProbe probe = new(cap);
        CompiledRule<BatchItem> rule = CompileProbe((_, ct) => probe.RunAsync(ct));
        BatchItem[] items = [.. Enumerable.Range(0, 12).Select(id => new BatchItem(id))];

        Decision[] decisions = await rule.EvaluateManyAsync(
                items,
                maxDegreeOfParallelism: cap,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(12, decisions.Length);
        Assert.Equal(cap, probe.MaxInFlight);
    }

    /// <summary>With no parallelism argument the contexts are evaluated one at a time.</summary>
    [Fact]
    public async Task EvaluateManyAsync_DefaultParallelism_EvaluatesOneContextAtATime_Test()
    {
        ConcurrencyProbe probe = new(1);
        CompiledRule<BatchItem> rule = CompileProbe((_, ct) => probe.RunAsync(ct));
        BatchItem[] items = [.. Enumerable.Range(0, 5).Select(id => new BatchItem(id))];

        await rule.EvaluateManyAsync(items, cancellationToken: TestContext.Current.CancellationToken)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, probe.MaxInFlight);
    }

    /// <summary>A predicate that throws for one context gives that context Unknown and a fault; the other contexts are decided normally.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task EvaluateManyAsync_OneContextFaults_OtherContextsAreStillDecided_Test(int cap)
    {
        CompiledRule<BatchItem> rule = CompileProbe(
            (item, _) =>
                item.Id == 2 ? throw new InvalidOperationException("Context 2 faulted.") : ValueTask.FromResult(TruthValue.True)
        );
        BatchItem[] items = [.. Enumerable.Range(0, 5).Select(id => new BatchItem(id))];

        Decision[] decisions = await rule.EvaluateManyAsync(
                items,
                maxDegreeOfParallelism: cap,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [TruthValue.True, TruthValue.True, TruthValue.Unknown, TruthValue.True, TruthValue.True],
            decisions.Select(decision => decision.Result)
        );
        Assert.Single(decisions[2].Faults);
        Assert.All(decisions.Where((_, index) => index != 2), decision => Assert.Empty(decision.Faults));
    }

    /// <summary>Cancelling the token while evaluations wait on slow predicates stops the batch at once with <see cref="OperationCanceledException"/>.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task EvaluateManyAsync_TokenCancelledMidBatch_ThrowsOperationCanceledPromptly_Test(int cap)
    {
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        CompiledRule<BatchItem> rule = CompileProbe(
            async (item, ct) =>
            {
                if (item.Id == 0)
                {
                    return TruthValue.True;
                }

                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                return TruthValue.True;
            }
        );
        BatchItem[] items = [.. Enumerable.Range(0, 8).Select(id => new BatchItem(id))];
        Stopwatch stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (
                Decision decision in rule.EvaluateManyAsync(
                    items,
                    maxDegreeOfParallelism: cap,
                    cancellationToken: cancellation.Token
                )
            )
            {
                Assert.Equal(TruthValue.True, decision.Result);
                await cancellation.CancelAsync();
            }
        });

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"The batch took {stopwatch.Elapsed} to stop.");
    }

    /// <summary>A token that is already cancelled stops the batch before any context is evaluated.</summary>
    [Fact]
    public async Task EvaluateManyAsync_TokenAlreadyCancelled_ThrowsWithoutEvaluating_Test()
    {
        int calls = 0;
        CompiledRule<BatchItem> rule = CompileProbe(
            (_, _) =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult(TruthValue.True);
            }
        );

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await rule.EvaluateManyAsync([new BatchItem(0)], cancellationToken: new CancellationToken(canceled: true))
                .ToArrayAsync(TestContext.Current.CancellationToken)
        );

        Assert.Equal(0, calls);
    }

    /// <summary>When the consumer stops reading, the evaluations still in flight are cancelled.</summary>
    [Fact]
    public async Task EvaluateManyAsync_ConsumerStopsEarly_CancelsTheEvaluationsInFlight_Test()
    {
        int cancelled = 0;
        CompiledRule<BatchItem> rule = CompileProbe(
            async (item, ct) =>
            {
                if (item.Id == 0)
                {
                    return TruthValue.True;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelled);
                    throw;
                }

                return TruthValue.True;
            }
        );
        BatchItem[] items = [.. Enumerable.Range(0, 4).Select(id => new BatchItem(id))];

        IAsyncEnumerator<Decision> reader = rule.EvaluateManyAsync(
                items,
                maxDegreeOfParallelism: 4,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await reader.MoveNextAsync());
        await reader.DisposeAsync();

        Assert.Equal(3, Volatile.Read(ref cancelled));
    }

    /// <summary>The timeout of the options applies to each context, so a batch longer than the timeout succeeds when every context is within it.</summary>
    [Fact]
    public async Task EvaluateManyAsync_TimeoutOption_AppliesToEachContextSeparately_Test()
    {
        CompiledRule<BatchItem> rule = CompileProbe(
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(60), ct).ConfigureAwait(false);
                return TruthValue.True;
            }
        );
        BatchItem[] items = [.. Enumerable.Range(0, 5).Select(id => new BatchItem(id))];

        Decision[] decisions = await rule.EvaluateManyAsync(
                items,
                options: new EvaluationOptions(Timeout: TimeSpan.FromSeconds(1)),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.All(decisions, decision => Assert.Equal(TruthValue.True, decision.Result));
    }

    /// <summary>The fault budget applies to each context: every context stops at its own first fault, and the batch goes on.</summary>
    [Fact]
    public async Task EvaluateManyAsync_FaultBudgetOption_AppliesToEachContextSeparately_Test()
    {
        RuleCompiler<BatchItem> compiler = new(
            PredicateRegistry<BatchItem>
                .CreateBuilder()
                .Add(
                    PredicateSchema.NoArguments("a", "a", "Always faults."),
                    (_, _, _) => throw new InvalidOperationException("a faulted.")
                )
                .Add(
                    PredicateSchema.NoArguments("b", "b", "Always faults."),
                    (_, _, _) => throw new InvalidOperationException("b faulted.")
                )
                .Build()
        );
        CompiledRule<BatchItem> rule = compiler.Compile("a XOR b").CompiledRule!;
        BatchItem[] items = [.. Enumerable.Range(0, 3).Select(id => new BatchItem(id))];

        Decision[] decisions = await rule.EvaluateManyAsync(
                items,
                options: new EvaluationOptions(FaultBudget: 1),
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, decisions.Length);
        Assert.All(decisions, decision => Assert.Single(decision.Faults));
    }

    /// <summary>An asynchronous stream of contexts gives the decisions in the order the stream produced the contexts.</summary>
    [Fact]
    public async Task EvaluateManyAsync_AsyncStreamOfContexts_YieldsDecisionsInStreamOrder_Test()
    {
        CompiledRule<BatchItem> rule = CompileProbe(
            async (item, ct) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds((5 - item.Id) * 10), ct).ConfigureAwait(false);
                return ValueOf(item.Id);
            }
        );

        Decision[] decisions = await rule.EvaluateManyAsync(
                StreamAsync(5, TestContext.Current.CancellationToken),
                maxDegreeOfParallelism: 3,
                cancellationToken: TestContext.Current.CancellationToken
            )
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Enumerable.Range(0, 5).Select(ValueOf), decisions.Select(decision => decision.Result));
    }

    /// <summary>An empty input gives no decisions.</summary>
    [Fact]
    public async Task EvaluateManyAsync_NoContexts_YieldsNothing_Test()
    {
        CompiledRule<BatchItem> rule = CompileProbe((_, _) => ValueTask.FromResult(TruthValue.True));

        Decision[] decisions = await rule.EvaluateManyAsync([], cancellationToken: TestContext.Current.CancellationToken)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Empty(decisions);
    }

    /// <summary>A parallelism below 1 is rejected when the method is called, before anything is enumerated.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void EvaluateManyAsync_ParallelismBelowOne_ThrowsArgumentOutOfRange_Test(int cap)
    {
        CompiledRule<BatchItem> rule = CompileProbe((_, _) => ValueTask.FromResult(TruthValue.True));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            rule.EvaluateManyAsync(
                [new BatchItem(0)],
                maxDegreeOfParallelism: cap,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>A fault budget below 1 is rejected when the method is called, as for a single evaluation.</summary>
    [Fact]
    public void EvaluateManyAsync_FaultBudgetBelowOne_ThrowsArgumentOutOfRange_Test()
    {
        CompiledRule<BatchItem> rule = CompileProbe((_, _) => ValueTask.FromResult(TruthValue.True));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            rule.EvaluateManyAsync(
                [new BatchItem(0)],
                options: new EvaluationOptions(FaultBudget: 0),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>A null sequence of contexts is rejected when the method is called.</summary>
    [Fact]
    public void EvaluateManyAsync_NullContexts_ThrowsArgumentNull_Test()
    {
        CompiledRule<BatchItem> rule = CompileProbe((_, _) => ValueTask.FromResult(TruthValue.True));

        Assert.Throws<ArgumentNullException>(() =>
            rule.EvaluateManyAsync((IEnumerable<BatchItem>)null!, cancellationToken: TestContext.Current.CancellationToken)
        );
        Assert.Throws<ArgumentNullException>(() =>
            rule.EvaluateManyAsync((IAsyncEnumerable<BatchItem>)null!, cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    private static TruthValue ValueOf(int id)
    {
        return (id % 3) switch
        {
            0 => TruthValue.True,
            1 => TruthValue.False,
            _ => TruthValue.Unknown,
        };
    }

    private static CompiledRule<BatchItem> CompileProbe(Func<BatchItem, CancellationToken, ValueTask<TruthValue>> probe)
    {
        RuleCompiler<BatchItem> compiler = new(
            PredicateRegistry<BatchItem>
                .CreateBuilder()
                .Add(
                    PredicateSchema.NoArguments("probe", "Probe", "A test predicate controlled by the test."),
                    (item, _, ct) => probe(item, ct)
                )
                .Build()
        );
        return compiler.Compile("probe").CompiledRule!;
    }

    private static async IAsyncEnumerable<BatchItem> StreamAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        for (int id = 0; id < count; id++)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new BatchItem(id);
        }
    }

    /// <summary>
    /// Counts the predicate calls in flight. Each call waits until the expected number of calls is in flight (or a short
    /// time passes), so the peak count reaches the cap deterministically when the batch allows it.
    /// </summary>
    private sealed class ConcurrencyProbe(int expectedPeak)
    {
        private int inFlight;
        private int maxInFlight;

        public int MaxInFlight => Volatile.Read(ref this.maxInFlight);

        public async ValueTask<TruthValue> RunAsync(CancellationToken cancellationToken)
        {
            int now = Interlocked.Increment(ref this.inFlight);
            int seen = Volatile.Read(ref this.maxInFlight);
            while (now > seen)
            {
                int previous = Interlocked.CompareExchange(ref this.maxInFlight, now, seen);
                if (previous == seen)
                {
                    break;
                }

                seen = previous;
            }

            Stopwatch waited = Stopwatch.StartNew();
            while (Volatile.Read(ref this.inFlight) < expectedPeak && waited.Elapsed < TimeSpan.FromMilliseconds(500))
            {
                await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            }

            // Hold the slot a little longer, so a batch that ignored the cap would push the count above it.
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            Interlocked.Decrement(ref this.inFlight);
            return TruthValue.True;
        }
    }
}
