namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 03: AND / OR / NOT with Kleene semantics, precedence, and short-circuiting.</summary>
public sealed class KleeneOperatorTests
{
    public static TheoryData<bool?, bool?, TruthValue> AndTruthTable =>
        new()
        {
            { true, true, TruthValue.True },
            { true, false, TruthValue.False },
            { true, null, TruthValue.Unknown },
            { false, true, TruthValue.False },
            { false, false, TruthValue.False },
            { false, null, TruthValue.False },
            { null, true, TruthValue.Unknown },
            { null, false, TruthValue.False },
            { null, null, TruthValue.Unknown },
        };

    public static TheoryData<bool?, bool?, TruthValue> OrTruthTable =>
        new()
        {
            { true, true, TruthValue.True },
            { true, false, TruthValue.True },
            { true, null, TruthValue.True },
            { false, true, TruthValue.True },
            { false, false, TruthValue.False },
            { false, null, TruthValue.Unknown },
            { null, true, TruthValue.True },
            { null, false, TruthValue.Unknown },
            { null, null, TruthValue.Unknown },
        };

    /// <summary>AND matches the Kleene truth table.</summary>
    [Theory]
    [MemberData(nameof(AndTruthTable))]
    public async Task And_TruthTable_MatchesKleeneTable_Test(bool? left, bool? right, TruthValue expected)
    {
        Decision decision = await EvaluateBinaryAsync("a AND b", left, right);
        Assert.Equal(expected, decision.Result);
    }

    /// <summary>OR matches the Kleene truth table.</summary>
    [Theory]
    [MemberData(nameof(OrTruthTable))]
    public async Task Or_TruthTable_MatchesKleeneTable_Test(bool? left, bool? right, TruthValue expected)
    {
        Decision decision = await EvaluateBinaryAsync("a OR b", left, right);
        Assert.Equal(expected, decision.Result);
    }

    /// <summary>NOT matches the Kleene truth table.</summary>
    [Theory]
    [InlineData(true, TruthValue.False)]
    [InlineData(false, TruthValue.True)]
    public async Task Not_TruthTable_MatchesKleeneTable_Test(bool value, TruthValue expected)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", value)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("NOT a")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>NOT of a faulting term is unknown, not true.</summary>
    [Fact]
    public async Task Not_FaultingTerm_IsUnknownNotTrue_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("a")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("NOT a")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>NOT binds tighter than AND, which binds tighter than OR.</summary>
    [Fact]
    public async Task Precedence_Operators_NotBindsTighterThanAndThanOr_Test()
    {
        // "NOT a AND b" must parse as "(NOT a) AND b": a=true -> NOT a=false -> AND anything=false.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("NOT a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>OR with a faulting operand and a true operand is true.</summary>
    [Fact]
    public async Task Or_FaultingOperandAndTrue_IsTrue_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("a")
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a OR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>AND with a faulting operand and a false operand is false.</summary>
    [Fact]
    public async Task And_FaultingOperandAndFalse_IsFalse_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("a")
            .AddConstant("b", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>AND short circuits after the first false and records the skipped operands as not evaluated.</summary>
    [Fact]
    public async Task And_FirstFalse_ShortCircuitsAndRecordsNotEvaluated_Test()
    {
        List<string> invocationLog = [];
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddCountingConstant("a", false, invocationLog)
            .AddCountingConstant("b", true, invocationLog)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                options: new EvaluationOptions(IncludeTrace: true),
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(["a"], invocationLog);
        Assert.NotNull(decision.Trace);
        Assert.Contains(decision.Trace.Entries, e => e.Text == "b" && e.NotEvaluated);
    }

    /// <summary>OR short circuits after the first true.</summary>
    [Fact]
    public async Task Or_FirstTrue_ShortCircuits_Test()
    {
        List<string> invocationLog = [];
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddCountingConstant("a", true, invocationLog)
            .AddCountingConstant("b", false, invocationLog)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a OR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(["a"], invocationLog);
    }

    /// <summary>Sibling operands are evaluated in written order.</summary>
    [Fact]
    public async Task Evaluation_SiblingOperands_RunInWrittenOrder_Test()
    {
        List<string> invocationLog = [];
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddCountingConstant("a", true, invocationLog)
            .AddCountingConstant("b", true, invocationLog)
            .AddCountingConstant("c", true, invocationLog)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        await compiler
            .Compile("a AND b AND c")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(["a", "b", "c"], invocationLog);
    }

    private static Task<Decision> EvaluateBinaryAsync(string dsl, bool? left, bool? right)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = left is { } l ? builder.AddConstant("a", l) : builder.AddThrowing("a");
        builder = right is { } r ? builder.AddConstant("b", r) : builder.AddThrowing("b");
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        return compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
    }
}
