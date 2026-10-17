namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// <see cref="ShadowReplay"/>: the contexts whose decision differs between two versions of a rule. A context is a
/// string such as <c>"a=T,b=F"</c>: <c>T</c>, <c>F</c> and <c>U</c> are the answer of the predicate, <c>X</c> makes it
/// throw, and a missing name answers <c>Unknown</c>.
/// </summary>
public sealed class ShadowReplayTests
{
    /// <summary>Only the contexts whose result differs are listed, in input order, with the count of those replayed.</summary>
    [Fact]
    public async Task CompareAsync_ResultDiffers_ListsOnlyChangedContextsInOrder_Test()
    {
        CompiledRule<string> baseline = Compile("a AND b");
        CompiledRule<string> candidate = Compile("a OR b");

        ShadowReplayResult<string> result = await ShadowReplay.CompareAsync(
            baseline,
            candidate,
            ["a=T,b=T", "a=T,b=F", "a=F,b=F", "a=F,b=T"],
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal((4, 2), (result.ContextCount, result.UnchangedCount));
        Assert.Equal([1, 3], result.Changes.Select(change => change.Index));
        ShadowReplayChange<string> first = result.Changes[0];
        Assert.Equal(
            ("a=T,b=F", TruthValue.False, TruthValue.True),
            (first.Context, first.Baseline.Result, first.Candidate.Result)
        );
        Assert.True(first.ResultChanged);
        Assert.True(first.SatisfactionChanged);
    }

    /// <summary>Each changed context carries the trace and the trace tree of both versions.</summary>
    [Fact]
    public async Task CompareAsync_ChangedContext_CarriesBothTraces_Test()
    {
        ShadowReplayResult<string> result = await ShadowReplay.CompareAsync(
            Compile("a AND b"),
            Compile("a OR b"),
            ["a=T,b=F"],
            cancellationToken: TestContext.Current.CancellationToken
        );

        ShadowReplayChange<string> change = Assert.Single(result.Changes);
        Assert.NotNull(change.Baseline.Trace);
        Assert.NotNull(change.Candidate.Trace);
        Assert.NotNull(change.Baseline.TraceTree);
        Assert.NotNull(change.Candidate.TraceTree);
        Assert.NotEqual(change.Baseline.TraceTree, change.Candidate.TraceTree);
    }

    /// <summary>Two identical rules produce no changes.</summary>
    [Fact]
    public async Task CompareAsync_SameRule_ReportsNoChanges_Test()
    {
        ShadowReplayResult<string> result = await ShadowReplay.CompareAsync(
            Compile("a AND b"),
            Compile("a AND b"),
            ["a=T,b=T", "a=F", "a=T,b=U"],
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Empty(result.Changes);
        Assert.Equal(3, result.UnchangedCount);
    }

    /// <summary>With the satisfaction comparison, a change between False and Unknown is not reported but a change to True is.</summary>
    [Fact]
    public async Task CompareAsync_IsSatisfiedComparison_IgnoresFalseToUnknown_Test()
    {
        // Baseline: a AND b. Candidate: a OR b. Context "a=F,b=U": False -> Unknown. Context "a=F,b=T": False -> True.
        ShadowReplayResult<string> result = await ShadowReplay.CompareAsync(
            Compile("a AND b"),
            Compile("a OR b"),
            ["a=F,b=U", "a=F,b=T"],
            options: new ShadowReplayOptions(ShadowReplayComparison.IsSatisfied),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(["a=F,b=T"], result.Changes.Select(change => change.Context));
    }

    /// <summary>A different fault set is a change only when fault changes are requested.</summary>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task CompareAsync_FaultSetDiffers_IsReportedOnlyWhenRequested_Test(bool includeFaultChanges, int expected)
    {
        // Both give True for "b=X,d=T": the baseline faults on b, the candidate short-circuits before b.
        ShadowReplayResult<string> result = await ShadowReplay.CompareAsync(
            Compile("b OR d"),
            Compile("d OR b"),
            ["b=X,d=T"],
            options: new ShadowReplayOptions(IncludeFaultChanges: includeFaultChanges),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, result.Changes.Count);
    }

    /// <summary>An asynchronous stream of more contexts than one block is replayed whole and the indexes stay global.</summary>
    [Fact]
    public async Task CompareAsync_AsyncStreamLongerThanABlock_NumbersChangesAcrossBlocks_Test()
    {
        string[] contexts = [.. Enumerable.Range(0, 150).Select(i => i == 100 ? "a=T,b=F" : "a=T,b=T")];

        ShadowReplayResult<string> result = await ShadowReplay.CompareAsync(
            Compile("a AND b"),
            Compile("a"),
            contexts.ToAsyncEnumerable(),
            maxDegreeOfParallelism: 4,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(150, result.ContextCount);
        Assert.Equal(100, Assert.Single(result.Changes).Index);
    }

    /// <summary>A null rule or sequence, a parallelism below 1 or a fault budget below 1 is rejected.</summary>
    [Fact]
    public async Task CompareAsync_BadArguments_AreRejected_Test()
    {
        CompiledRule<string> rule = Compile("a");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ShadowReplay.CompareAsync(null!, rule, ["a=T"], cancellationToken: TestContext.Current.CancellationToken)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ShadowReplay.CompareAsync(
                rule,
                rule,
                (IEnumerable<string>)null!,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            ShadowReplay.CompareAsync(
                rule,
                rule,
                ["a=T"],
                maxDegreeOfParallelism: 0,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            ShadowReplay.CompareAsync(
                rule,
                rule,
                ["a=T"],
                evaluationOptions: new EvaluationOptions(FaultBudget: 0),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    private static CompiledRule<string> Compile(string text)
    {
        PredicateRegistryBuilder<string> builder = PredicateRegistry<string>.CreateBuilder();
        foreach (string name in new[] { "a", "b", "d" })
        {
            builder.Add(
                PredicateSchema.NoArguments(name, name, $"Reads '{name}' from the context text."),
                (context, _, _) => ValueTask.FromResult(Read(context, name))
            );
        }

        CompilationResult<string> result = new RuleCompiler<string>(builder.Build()).Compile(text);
        return result.CompiledRule ?? throw new InvalidOperationException(result.FormatDiagnostics(text));
    }

    private static TruthValue Read(string context, string name)
    {
        foreach (string pair in context.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=');
            if (parts[0] == name)
            {
                return parts[1] switch
                {
                    "T" => TruthValue.True,
                    "F" => TruthValue.False,
                    "U" => TruthValue.Unknown,
                    _ => throw new InvalidOperationException($"Predicate '{name}' failed."),
                };
            }
        }

        return TruthValue.Unknown;
    }
}
