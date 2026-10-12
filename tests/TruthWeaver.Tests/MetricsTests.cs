namespace TruthWeaver.Tests;

using System.Diagnostics.Metrics;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Metrics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Meter-based counters for evaluation, faults, and compile diagnostics. The library's <c>Meter</c>
/// is process-wide static state (same as any <c>System.Diagnostics.Metrics</c> producer), so these
/// assertions tolerate measurements from other tests running concurrently in the same process —
/// "at least one matching measurement was recorded", never an exact count.
/// </summary>
public sealed class MetricsTests
{
    /// <summary>Evaluating a rule increments the evaluations counter.</summary>
    [Fact]
    public async Task Evaluate_Rule_IncrementsEvaluationsCounter_Test()
    {
        using MeasurementCollector collector = new("truthweaver.evaluations");
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("p", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("p").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(1L, collector.Measurements);
    }

    /// <summary>A faulting predicate increments the faults counter.</summary>
    [Fact]
    public async Task Evaluate_FaultingPredicate_IncrementsFaultsCounter_Test()
    {
        using MeasurementCollector collector = new("truthweaver.faults");
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("flaky").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("flaky").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(1L, collector.Measurements);
    }

    /// <summary>Compiling a rule with a diagnostic increments the compile diagnostics counter, tagged by severity.</summary>
    [Fact]
    public void Compile_DiagnosticRule_IncrementsCounterTaggedBySeverity_Test()
    {
        using MeasurementCollector collector = new("truthweaver.compile_diagnostics");
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        compiler.Compile("noSuchPredicate");

        Assert.Contains(collector.Tags, t => t.GetValueOrDefault("severity") == nameof(DiagnosticSeverity.Error));
    }

    /// <summary>Subscribes to one named counter on the library's meter and records every measurement raised while it is alive.</summary>
    private sealed class MeasurementCollector : IDisposable
    {
        private readonly MeterListener listener;

        public MeasurementCollector(string instrumentName)
        {
            this.listener = new MeterListener
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == TruthWeaverMetrics.MeterName && instrument.Name == instrumentName)
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                },
            };
            this.listener.SetMeasurementEventCallback<long>(
                (_, measurement, tags, _) =>
                {
                    this.Measurements.Add(measurement);
                    this.Tags.Add(tags.ToArray().ToDictionary(t => t.Key, t => t.Value?.ToString()));
                }
            );
            this.listener.Start();
        }

        public ConcurrentBag<long> Measurements { get; } = [];

        public ConcurrentBag<Dictionary<string, string?>> Tags { get; } = [];

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }
}
