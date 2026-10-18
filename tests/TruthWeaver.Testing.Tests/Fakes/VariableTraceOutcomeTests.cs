namespace TruthWeaver.Testing.Tests.Fakes;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// The trace names the outcome of each variable reference (ADR-0006 decision 13): resolved, missing, ambiguous,
/// type mismatch, source error or unsupplied source.
/// </summary>
public sealed class VariableTraceOutcomeTests
{
    private const string Reference = "from(\"user\", \"$.v\")";

    /// <summary>Each failure kind shows as its own outcome word next to the reference, and the term is Unknown with a fault.</summary>
    [Theory]
    [InlineData("missing", "takesString")]
    [InlineData("ambiguous", "takesString")]
    [InlineData("type mismatch", "takesInt64")]
    [InlineData("source error", "takesString")]
    [InlineData("unsupplied source", "takesString")]
    public async Task EvaluateAsync_FailedReference_TraceNamesTheOutcome_Test(string outcome, string predicate)
    {
        DataSources sources = new() { ["user"] = SourceFor(outcome) };
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile($"{predicate}(v: {Reference})", "user", "other");

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            outcome == "unsupplied source" ? new DataSources { ["other"] = new FakeDataSource() } : sources,
            new EvaluationOptions(IncludeTrace: true),
            TestContext.Current.CancellationToken
        );

        string expected = $"{predicate}(v: {Reference}) [{Reference}: {outcome}]";
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(expected, decision.TraceTree!.Text);
        Assert.Equal(expected, decision.Trace!.Entries[0].Text);
    }

    /// <summary>A repeat of a term answered from memoization shows the same outcome as its first occurrence.</summary>
    [Fact]
    public async Task EvaluateAsync_RepeatedTerm_TraceKeepsTheOutcome_Test()
    {
        VariableHarness harness = new();
        string term = $"takesString(v: {Reference})";
        CompiledRule<object?> rule = harness.Compile($"{term} AND {term}", "user");

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = new FakeDataSource().With("$.v", "x") },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.All(
            decision.TraceTree!.Children,
            child => Assert.EndsWith($"[{Reference}: resolved]", child.Text, StringComparison.Ordinal)
        );
    }

    private static FakeDataSource SourceFor(string outcome)
    {
        return outcome switch
        {
            "ambiguous" => new FakeDataSource().With("$.v", ["a", "b"]),
            "type mismatch" => new FakeDataSource().With("$.v", "text"),
            "source error" => new FakeDataSource().Failing("$.v", "offline"),
            _ => new FakeDataSource(),
        };
    }
}
