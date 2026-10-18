namespace TruthWeaver.Tests;

using System.Diagnostics;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Metrics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// ActivitySource spans for evaluation and, opt-in, for each predicate. The source is process-wide static state, so
/// every test runs its evaluation under its own parent activity and keeps only the spans that descend from it,
/// which makes the assertions independent of evaluations running concurrently in other tests.
/// </summary>
public sealed class TracingTests
{
    private static readonly ActivitySource TestSource = new("TruthWeaver.Tests.Tracing");

    /// <summary>An evaluation starts one span tagged with the decision value and the fault count.</summary>
    [Fact]
    public async Task EvaluateAsync_Rule_StartsSpanTaggedWithDecisionAndFaultCount_Test()
    {
        using SpanCollector collector = new();
        CompiledRule<RuleTestContext> rule = Compile("p AND flaky", b => b.AddConstant("p", true).AddThrowing("flaky"));

        using Activity? parent = TestSource.StartActivity("parent");
        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Activity span = Assert.Single(
            collector.Descendants(parent!),
            a => a.OperationName == TruthWeaverTracing.EvaluationSpanName
        );

        Assert.Equal("Unknown", span.GetTagItem(TruthWeaverTracing.DecisionTag));
        Assert.Equal(1, span.GetTagItem(TruthWeaverTracing.FaultCountTag));
    }

    /// <summary>By default no predicate span is started.</summary>
    [Fact]
    public async Task EvaluateAsync_DefaultOptions_StartsNoPredicateSpans_Test()
    {
        using SpanCollector collector = new();
        CompiledRule<RuleTestContext> rule = Compile("p", b => b.AddConstant("p", true));

        using Activity? parent = TestSource.StartActivity("parent");
        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.DoesNotContain(collector.Descendants(parent!), a => a.OperationName == TruthWeaverTracing.PredicateSpanName);
    }

    /// <summary>With TracePredicates set, each predicate invocation is a child span of the evaluation span carrying its name, result and fault flag.</summary>
    [Fact]
    public async Task EvaluateAsync_TracePredicates_StartsChildSpanPerPredicate_Test()
    {
        using SpanCollector collector = new();
        CompiledRule<RuleTestContext> rule = Compile(
            "okTrace AND flakyTrace",
            b => b.AddConstant("okTrace", true).AddThrowing("flakyTrace")
        );

        using Activity? parent = TestSource.StartActivity("parent");
        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(TracePredicates: true),
            cancellationToken: TestContext.Current.CancellationToken
        );
        List<Activity> spans = [.. collector.Descendants(parent!)];
        Activity evaluation = Assert.Single(spans, a => a.OperationName == TruthWeaverTracing.EvaluationSpanName);
        List<Activity> predicates = [.. spans.Where(a => a.OperationName == TruthWeaverTracing.PredicateSpanName)];

        Assert.Equal(2, predicates.Count);
        Assert.All(predicates, p => Assert.Equal(evaluation.Id, p.ParentId));
        Activity ok = Assert.Single(predicates, p => (string?)p.GetTagItem(TruthWeaverTracing.PredicateNameTag) == "okTrace");
        Assert.Equal("True", ok.GetTagItem(TruthWeaverTracing.PredicateResultTag));
        Assert.False((bool)ok.GetTagItem(TruthWeaverTracing.PredicateFaultedTag)!);
        Activity flaky = Assert.Single(
            predicates,
            p => (string?)p.GetTagItem(TruthWeaverTracing.PredicateNameTag) == "flakyTrace"
        );
        Assert.Equal("Unknown", flaky.GetTagItem(TruthWeaverTracing.PredicateResultTag));
        Assert.True((bool)flaky.GetTagItem(TruthWeaverTracing.PredicateFaultedTag)!);
    }

    private static CompiledRule<RuleTestContext> Compile(
        string text,
        Func<PredicateRegistryBuilder<RuleTestContext>, PredicateRegistryBuilder<RuleTestContext>> register
    )
    {
        RuleCompiler<RuleTestContext> compiler = new(register(PredicateRegistry<RuleTestContext>.CreateBuilder()).Build());
        return compiler.Compile(text).CompiledRule!;
    }

    /// <summary>Listens to the library's source and the test source, and records every span that stops while it is alive.</summary>
    private sealed class SpanCollector : IDisposable
    {
        private readonly ActivityListener listener;
        private readonly ConcurrentBag<Activity> stopped = [];

        public SpanCollector()
        {
            this.listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name is TruthWeaverTracing.SourceName or "TruthWeaver.Tests.Tracing",
                Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = this.stopped.Add,
            };
            ActivitySource.AddActivityListener(this.listener);
        }

        /// <summary>Returns the stopped spans that share <paramref name="parent"/>'s trace, excluding the parent.</summary>
        public IEnumerable<Activity> Descendants(Activity parent)
        {
            return this.stopped.Where(a => a.TraceId == parent.TraceId && a.Id != parent.Id);
        }

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }
}
