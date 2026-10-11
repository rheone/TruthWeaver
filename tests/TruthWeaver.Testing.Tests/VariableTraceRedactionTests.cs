namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// Trace and fault redaction for variable references (ADR-0006 decision 13, data-sources ticket 07): by default neither
/// shows a resolved value, and <see cref="EvaluationOptions.IncludeResolvedValues"/> adds values to the trace only.
/// </summary>
public sealed class VariableTraceRedactionTests
{
    private const string Secret = "hunter2";

    /// <summary>By default the trace tree and flat trace carry the reference text and no resolved value.</summary>
    [Fact]
    public async Task EvaluateAsync_DefaultOptions_TraceContainsNoResolvedValue_Test()
    {
        Decision decision = await EvaluateAsync(new FakeDataSource().With("$.secret", Secret), EvaluationOptions.Default);

        Assert.Equal("takesString(v: from(\"user\", \"$.secret\"))", decision.TraceTree!.Text);
        Assert.All(decision.Trace!.Entries, e => Assert.DoesNotContain(Secret, e.Text, StringComparison.Ordinal));
    }

    /// <summary>Every failure kind's fault message names the reference and never a value the source held.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task EvaluateAsync_FailedResolution_FaultMessageContainsNoResolvedValue_Test(bool ambiguous, bool mismatch)
    {
        // Ambiguous: two string matches; mismatch: the argument is Int64 but the match is a string.
        FakeDataSource source = ambiguous
            ? new FakeDataSource().With("$.secret", [Secret, Secret + "x"])
            : new FakeDataSource().With("$.secret", Secret);
        string rule = mismatch ? "takesInt64(v: from(\"user\", \"$.secret\"))" : "takesString(v: from(\"user\", \"$.secret\"))";

        Decision decision = await EvaluateAsync(source, new EvaluationOptions(IncludeResolvedValues: true), rule);

        Fault fault = Assert.Single(decision.Faults);
        Assert.DoesNotContain(Secret, fault.Exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, decision.TraceTree!.Text, StringComparison.Ordinal);
    }

    /// <summary>With the opt-in the trace shows the resolved value next to the reference.</summary>
    [Fact]
    public async Task EvaluateAsync_IncludeResolvedValues_TraceShowsTheValue_Test()
    {
        Decision decision = await EvaluateAsync(
            new FakeDataSource().With("$.secret", Secret),
            new EvaluationOptions(IncludeResolvedValues: true)
        );

        const string expected = "takesString(v: from(\"user\", \"$.secret\") = \"hunter2\")";
        Assert.Equal(expected, decision.TraceTree!.Text);
        Assert.Equal(expected, decision.Trace!.Entries[0].Text);
    }

    /// <summary>With the opt-in a successful evaluation still records no fault, so values never reach faults.</summary>
    [Fact]
    public async Task EvaluateAsync_IncludeResolvedValues_RecordsNoFaultAndMemoizedRepeatKeepsTheValue_Test()
    {
        Decision decision = await EvaluateAsync(
            new FakeDataSource().With("$.secret", Secret),
            new EvaluationOptions(IncludeResolvedValues: true),
            "takesString(v: from(\"user\", \"$.secret\")) AND takesString(v: from(\"user\", \"$.secret\"))"
        );

        decision.Should().HaveNoFaults();
        Assert.All(
            decision.Trace!.Entries.Where(e => e.Text.StartsWith("takes", StringComparison.Ordinal)),
            e => Assert.Contains("= \"hunter2\"", e.Text, StringComparison.Ordinal)
        );
    }

    /// <summary>A term that failed to resolve has no value to show, so the opt-in trace keeps the reference text.</summary>
    [Fact]
    public async Task EvaluateAsync_IncludeResolvedValuesWithMissingValue_TraceKeepsTheReferenceText_Test()
    {
        Decision decision = await EvaluateAsync(new FakeDataSource(), new EvaluationOptions(IncludeResolvedValues: true));

        Assert.Equal("takesString(v: from(\"user\", \"$.secret\"))", decision.TraceTree!.Text);
    }

    /// <summary>An array query that matches no node stays an empty array without a fault, and the trace names the query.</summary>
    [Fact]
    public async Task EvaluateAsync_ArrayQueryMatchingNothing_TraceNotesTheZeroMatchAndRecordsNoFault_Test()
    {
        const string Rule = "takesStrings(v: from(\"user\", \"$.rolez[*]\"))";
        Decision decision = await EvaluateAsync(
            new FakeDataSource().With("$.roles[*]", ["admin"]),
            EvaluationOptions.Default,
            Rule
        );

        decision.Should().HaveNoFaults();
        Assert.Equal(TruthValue.True, decision.Result);
        const string expected = Rule + " [no match for from(\"user\", \"$.rolez[*]\")]";
        Assert.Equal(expected, decision.TraceTree!.Text);
        Assert.Equal(expected, decision.Trace!.Entries[0].Text);
    }

    /// <summary>An array query that matches a node adds no note to the trace.</summary>
    [Fact]
    public async Task EvaluateAsync_ArrayQueryMatchingANode_TraceCarriesNoNote_Test()
    {
        const string Rule = "takesStrings(v: from(\"user\", \"$.roles[*]\"))";
        Decision decision = await EvaluateAsync(
            new FakeDataSource().With("$.roles[*]", ["admin"]),
            EvaluationOptions.Default,
            Rule
        );

        Assert.Equal(Rule, decision.TraceTree!.Text);
    }

    private static Task<Decision> EvaluateAsync(
        FakeDataSource source,
        EvaluationOptions options,
        string rule = "takesString(v: from(\"user\", \"$.secret\"))"
    )
    {
        VariableHarness harness = new();
        return VariableHarness.EvaluateAsync(
            harness.Compile(rule, "user"),
            new DataSources { ["user"] = source },
            options,
            TestContext.Current.CancellationToken
        );
    }
}
