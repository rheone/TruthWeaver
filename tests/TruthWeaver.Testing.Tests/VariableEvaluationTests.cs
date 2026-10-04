namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// Evaluating a rule that uses <c>from("source", "query")</c> against a <see cref="FakeDataSource"/> (ADR-0006,
/// data-sources ticket 01): the happy path, the unsupplied-source fault, and that rules without variables behave as before.
/// </summary>
public sealed class VariableEvaluationTests
{
    /// <summary>A scalar variable resolves from the source and is passed to the predicate as an ordinary argument.</summary>
    [Fact]
    public async Task EvaluateAsync_Int64Variable_PassesTheResolvedValueToThePredicate_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.minAge\"))", "user");
        DataSources sources = new() { ["user"] = new FakeDataSource().With("$.minAge", 18L) };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied().HaveNoFaults();
        Assert.Equal(LiteralValue.OfInt64(18), Assert.Single(harness.Received));
    }

    /// <summary>A string variable resolves the same way.</summary>
    [Fact]
    public async Task EvaluateAsync_StringVariable_PassesTheResolvedValueToThePredicate_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesString(v: from(\"request\", \"$.role\"))", "request");
        DataSources sources = new() { ["request"] = new FakeDataSource().With("$.role", "admin") };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied().HaveNoFaults();
        Assert.Equal(LiteralValue.OfString("admin"), Assert.Single(harness.Received));
    }

    /// <summary>The same compiled rule gives different answers for different sources, because resolution happens at evaluation time.</summary>
    [Fact]
    public async Task EvaluateAsync_SameRuleDifferentSources_ResolvesEachEvaluationAfresh_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.minAge\"))", "user");

        await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = new FakeDataSource().With("$.minAge", 18L) },
            cancellationToken: TestContext.Current.CancellationToken
        );
        await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = new FakeDataSource().With("$.minAge", 21L) },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal([LiteralValue.OfInt64(18), LiteralValue.OfInt64(21)], harness.Received);
    }

    /// <summary>A declared source that the evaluation was not given is Unknown plus an unsupplied-source fault, and the predicate never runs.</summary>
    [Fact]
    public async Task EvaluateAsync_DeclaredSourceNotSupplied_IsUnknownWithAnUnsuppliedSourceFault_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.minAge\"))", "user");

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            new DataSources(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().NotBeSatisfied().HaveResult(TruthValue.Unknown);
        Fault fault = Assert.Single(decision.Faults);
        VariableResolutionException exception = Assert.IsType<VariableResolutionException>(fault.Exception);
        Assert.Equal(VariableFailureKind.UnsuppliedSource, exception.Kind);
        Assert.Equal(new VariableReference("user", "$.minAge"), exception.Reference);
        Assert.Empty(harness.Received);
    }

    /// <summary>Passing no <see cref="DataSources"/> at all behaves like an empty set for a rule that needs one.</summary>
    [Fact]
    public async Task EvaluateAsync_NoDataSourcesPassed_IsUnknownWithAnUnsuppliedSourceFault_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.minAge\"))", "user");

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().HaveResult(TruthValue.Unknown);
        Assert.Equal(
            VariableFailureKind.UnsuppliedSource,
            Assert.IsType<VariableResolutionException>(Assert.Single(decision.Faults).Exception).Kind
        );
    }

    /// <summary>A missing value is Unknown plus a Missing fault, and Strong Kleene lets the rest of the rule still decide.</summary>
    [Fact]
    public async Task EvaluateAsync_MissingValueInOrWithTrue_StillEvaluatesToTrue_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.minAge\")) OR yes", "user");
        DataSources sources = new() { ["user"] = new FakeDataSource() };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied();
        Assert.Equal(
            VariableFailureKind.Missing,
            Assert.IsType<VariableResolutionException>(Assert.Single(decision.Faults).Exception).Kind
        );
    }

    /// <summary>The trace names the reference but never the resolved value.</summary>
    [Fact]
    public async Task EvaluateAsync_ResolvedVariable_TraceNamesTheReferenceNotTheValue_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesString(v: from(\"user\", \"$.secret\"))", "user");
        DataSources sources = new() { ["user"] = new FakeDataSource().With("$.secret", "hunter2") };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        TraceNode tree = decision.TraceTree!;
        Assert.Equal("takesString(v: from(\"user\", \"$.secret\"))", tree.Text);
        Assert.DoesNotContain("hunter2", tree.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", decision.Trace!.Entries[0].Text, StringComparison.Ordinal);
    }

    /// <summary>A rule with no variables evaluates identically with or without sources, and never touches them.</summary>
    [Fact]
    public async Task EvaluateAsync_RuleWithoutVariables_IgnoresSuppliedSources_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("yes AND NOT no");
        FakeDataSource source = new();

        Decision withSources = await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = source },
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision without = await VariableHarness.EvaluateAsync(
            rule,
            null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, withSources.Result);
        Assert.Equal(TruthValue.True, without.Result);
        Assert.Empty(source.Queries);
    }

    /// <summary>The original three-argument overload still evaluates a rule with no variables.</summary>
    [Fact]
    public async Task EvaluateAsync_ExistingOverloadWithoutSources_StillEvaluates_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("yes");

        Decision decision = await rule.EvaluateAsync(
            null,
            new VariableHarnessNoServices(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied();
    }

    private sealed class VariableHarnessNoServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
