namespace TruthWeaver.Metrics;

using System.Diagnostics;

/// <summary>
/// The library's <see cref="System.Diagnostics.ActivitySource"/> instrumentation, beside
/// <c>TruthWeaverMetrics</c>. A single static source named <see cref="SourceName"/> starts one span for each rule
/// evaluation and, only when <c>EvaluationOptions.TracePredicates</c> is set, one child span for each predicate
/// invocation. A host observes it like any other source (an <see cref="ActivityListener"/>, or OpenTelemetry's
/// <c>AddSource("TruthWeaver")</c>). With no listener, <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns <see langword="null"/> and the cost is one check. The library takes no dependency on an exporter.
/// </summary>
/// <remarks>
/// Tags carry only bounded, non-sensitive values: the decision value, the fault count and a registered predicate name.
/// Rule text, term arguments and resolved data are never tags (ADR-0013 decision 4).
/// </remarks>
internal static class TruthWeaverTracing
{
    /// <summary>The library's source name, for a host to subscribe to (for example OpenTelemetry's <c>AddSource</c>).</summary>
    public const string SourceName = "TruthWeaver";

    /// <summary>The name of the span that covers one rule evaluation.</summary>
    public const string EvaluationSpanName = "truthweaver.evaluate";

    /// <summary>The name of the child span that covers one predicate invocation.</summary>
    public const string PredicateSpanName = "truthweaver.predicate";

    /// <summary>The tag on an evaluation span that holds the decision value (<c>True</c>, <c>False</c> or <c>Unknown</c>).</summary>
    public const string DecisionTag = "truthweaver.decision";

    /// <summary>The tag on an evaluation span that holds the number of faults the decision carries.</summary>
    public const string FaultCountTag = "truthweaver.fault_count";

    /// <summary>The tag on a predicate span that holds the registered predicate name.</summary>
    public const string PredicateNameTag = "truthweaver.predicate.name";

    /// <summary>The tag on a predicate span that holds the value the predicate answered (<c>True</c>, <c>False</c> or <c>Unknown</c>).</summary>
    public const string PredicateResultTag = "truthweaver.predicate.result";

    /// <summary>The tag on a predicate span that is <see langword="true"/> when the invocation recorded a fault.</summary>
    public const string PredicateFaultedTag = "truthweaver.predicate.faulted";

    private static readonly ActivitySource Source = new(SourceName);

    /// <summary>Starts the span for one rule evaluation.</summary>
    /// <returns>The span, or <see langword="null"/> when no listener samples this source.</returns>
    public static Activity? StartEvaluation()
    {
        return Source.StartActivity(EvaluationSpanName);
    }

    /// <summary>Starts the child span for one predicate invocation.</summary>
    /// <param name="predicateName">The registered predicate name, recorded as a tag.</param>
    /// <returns>The span, or <see langword="null"/> when no listener samples this source.</returns>
    public static Activity? StartPredicate(string predicateName)
    {
        Activity? activity = Source.StartActivity(PredicateSpanName);
        activity?.SetTag(PredicateNameTag, predicateName);
        return activity;
    }
}
