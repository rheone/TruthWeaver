namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 05 (and its later extension): XOR / ExactlyOne / the AtLeast-AtMost-GreaterThan-LessThan-Exactly threshold family.</summary>
public sealed class XorExactlyOneThresholdTests
{
    public static TheoryData<bool?, bool?, TruthValue> XorTruthTable =>
        new()
        {
            { true, true, TruthValue.False },
            { true, false, TruthValue.True },
            { true, null, TruthValue.Unknown },
            { false, true, TruthValue.True },
            { false, false, TruthValue.False },
            { false, null, TruthValue.Unknown },
            { null, true, TruthValue.Unknown },
            { null, false, TruthValue.Unknown },
            { null, null, TruthValue.Unknown },
        };

    public static TheoryData<bool?, bool?, TruthValue> XnorTruthTable =>
        new()
        {
            { true, true, TruthValue.True },
            { true, false, TruthValue.False },
            { true, null, TruthValue.Unknown },
            { false, true, TruthValue.False },
            { false, false, TruthValue.True },
            { false, null, TruthValue.Unknown },
            { null, true, TruthValue.Unknown },
            { null, false, TruthValue.Unknown },
            { null, null, TruthValue.Unknown },
        };

    /// <summary>XOR matches the Kleene truth table.</summary>
    [Theory]
    [MemberData(nameof(XorTruthTable))]
    public async Task Xor_TruthTable_MatchesKleeneTable_Test(bool? left, bool? right, TruthValue expected)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = left is { } l ? builder.AddConstant("a", l) : builder.AddThrowing("a");
        builder = right is { } r ? builder.AddConstant("b", r) : builder.AddThrowing("b");
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        Decision decision = await compiler
            .Compile("a XOR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>XOR or XNOR with three operands, given through the JSON op, is a compile error.</summary>
    [Theory]
    [InlineData("xor")]
    [InlineData("xnor")]
    public void Xor_ThreeOperandsViaJsonOp_IsCompileError_Test(string op)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.CompileJson(
            $$"""{"op":"{{op}}","operands":[{"predicate":"a"},{"predicate":"b"},{"predicate":"c"}]}"""
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InfixArityViolation);
    }

    /// <summary>Mixing XOR or XNOR with AND or OR without parentheses is a compile error.</summary>
    [Theory]
    [InlineData("a AND b XOR c")]
    [InlineData("a XOR b AND c")]
    [InlineData("a OR b XOR c")]
    [InlineData("a AND b XNOR c")]
    [InlineData("a XNOR b AND c")]
    [InlineData("a OR b XNOR c")]
    [InlineData("a XOR b XNOR c")]
    public void Mixing_XorXnorWithAndOr_IsCompileError_Test(string dsl)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile(dsl);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>XOR inside parentheses, combined with AND, compiles successfully.</summary>
    [Fact]
    public void Xor_InsideParentheses_CompilesWithAnd_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND (b XOR c)");

        Assert.True(result.Succeeded);
    }

    /// <summary>XNOR matches the negated XOR truth table.</summary>
    [Theory]
    [MemberData(nameof(XnorTruthTable))]
    public async Task Xnor_TruthTable_MatchesNegatedXor_Test(bool? left, bool? right, TruthValue expected)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = left is { } l ? builder.AddConstant("a", l) : builder.AddThrowing("a");
        builder = right is { } r ? builder.AddConstant("b", r) : builder.AddThrowing("b");
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        Decision decision = await compiler
            .Compile("a XNOR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>XNOR inside parentheses, combined with AND, compiles successfully.</summary>
    [Fact]
    public void Xnor_InsideParentheses_CompilesWithAnd_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND (b XNOR c)");

        Assert.True(result.Succeeded);
    }

    /// <summary>XNOR round trips through JSON.</summary>
    [Fact]
    public void Xnor_RoundTrip_ThroughJson_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        CompilationResult<RuleTestContext> compiled = compiler.Compile("a XNOR b");
        Assert.True(compiled.Succeeded);

        string json = compiled.CompiledRule!.PrintJson();
        CompilationResult<RuleTestContext> reparsed = compiler.CompileJson(json);

        Assert.True(reparsed.Succeeded);
        Assert.Equal("(a EQUIVALENT b)", reparsed.CompiledRule!.CanonicalText);
    }

    /// <summary>ExactlyOne evaluates to true if and only if exactly one operand is true.</summary>
    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    [InlineData(false, false, false, TruthValue.False)]
    public async Task ExactlyOne_Evaluation_TrueIffExactlyOneTrue_Test(bool a, bool b, bool c, TruthValue expected)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", a)
                .AddConstant("b", b)
                .AddConstant("c", c)
                .Build()
        );

        Decision decision = await compiler
            .Compile("ExactlyOne(a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>ExactlyOne with two unknowns and the rest false is unknown.</summary>
    [Fact]
    public async Task ExactlyOne_TwoUnknownsRestFalse_IsUnknown_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", false).Build()
        );

        Decision decision = await compiler
            .Compile("ExactlyOne(a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>AtLeast evaluates to true if and only if at least k operands are true.</summary>
    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public async Task AtLeast_Evaluation_TrueIffAtLeastKTrue_Test(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("AtLeast(2, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>AtLeast with unsettled confirmed counts is unknown.</summary>
    [Fact]
    public async Task AtLeast_UnsettledCounts_IsUnknown_Test()
    {
        // One true, one unknown ("b" faults), one false: confirmed true count (1) < k (2), but
        // true+unknown (2) >= k -> Unknown.
        Decision decision = await EvaluateThresholdWithFaultingBAsync(
            "AtLeast(2, a, b, c)",
            trueOperand: true,
            falseOperand: false
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>AtLeast with k out of range is a compile error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void AtLeast_KOutOfRange_IsCompileError_Test(int k)
    {
        AssertThresholdOutOfRange($"AtLeast({k}, a, b, c)");
    }

    /// <summary>AtMost evaluates to true if and only if at most k operands are true.</summary>
    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public async Task AtMost_Evaluation_TrueIffAtMostKTrue_Test(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("AtMost(1, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>AtMost with unsettled confirmed counts is unknown.</summary>
    [Fact]
    public async Task AtMost_UnsettledCounts_IsUnknown_Test()
    {
        // Zero confirmed true, one unknown ("b" faults), one false: at-most-0 could still hold
        // (unknown resolves false) or fail (unknown resolves true) -> Unknown.
        Decision decision = await EvaluateThresholdWithFaultingBAsync(
            "AtMost(0, a, b, c)",
            trueOperand: false,
            falseOperand: false
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>AtMost with k out of range is a compile error.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AtMost_KOutOfRange_IsCompileError_Test(int k)
    {
        AssertThresholdOutOfRange($"AtMost({k}, a, b, c)");
    }

    /// <summary>GreaterThan evaluates to true if and only if more than k operands are true.</summary>
    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public async Task GreaterThan_Evaluation_TrueIffMoreThanKTrue_Test(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("GreaterThan(1, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>GreaterThan with k out of range is a compile error.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GreaterThan_KOutOfRange_IsCompileError_Test(int k)
    {
        AssertThresholdOutOfRange($"GreaterThan({k}, a, b, c)");
    }

    /// <summary>LessThan evaluates to true if and only if fewer than k operands are true.</summary>
    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public async Task LessThan_Evaluation_TrueIffFewerThanKTrue_Test(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("LessThan(2, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>LessThan with k out of range is a compile error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void LessThan_KOutOfRange_IsCompileError_Test(int k)
    {
        AssertThresholdOutOfRange($"LessThan({k}, a, b, c)");
    }

    /// <summary>Exactly evaluates to true if and only if the true count matches k exactly.</summary>
    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public async Task Exactly_Evaluation_TrueIffCountMatchesK_Test(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("Exactly(2, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>Exactly with k reachable but not certain is unknown.</summary>
    [Fact]
    public async Task Exactly_KReachableNotCertain_IsUnknown_Test()
    {
        // Zero confirmed true, two unknowns, one false: k=1 is reachable (one unknown resolves true)
        // but not certain (both could resolve false, or both true) -> Unknown.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", false).Build()
        );

        Decision decision = await compiler
            .Compile("Exactly(1, a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>Exactly with k out of range is a compile error.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Exactly_KOutOfRange_IsCompileError_Test(int k)
    {
        AssertThresholdOutOfRange($"Exactly({k}, a, b, c)");
    }

    private static void AssertThresholdOutOfRange(string dsl)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile(dsl);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidThresholdValue);
    }

    private static Task<Decision> EvaluateThresholdAsync(string dsl, bool a, bool b, bool c)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", a)
                .AddConstant("b", b)
                .AddConstant("c", c)
                .Build()
        );

        return compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
    }

    private static Task<Decision> EvaluateThresholdWithFaultingBAsync(string dsl, bool trueOperand, bool falseOperand)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", trueOperand)
                .AddThrowing("b")
                .AddConstant("c", falseOperand)
                .Build()
        );

        return compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
    }
}
