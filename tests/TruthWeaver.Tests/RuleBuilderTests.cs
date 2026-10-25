namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>The fluent <see cref="RuleBuilder"/> API for assembling a rule without hand-written DSL/JSON/YAML text.</summary>
public sealed class RuleBuilderTests
{
    /// <summary>The predicate builder compiles and evaluates like the equivalent DSL text.</summary>
    [Fact]
    public async Task Predicate_Builder_CompilesLikeDslText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("isManager").Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The AND, OR and NOT builders produce the same compiled rule as equivalent DSL text.</summary>
    [Fact]
    public void Builders_AndOrNot_MatchDslCompiledRule_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddConstant("isSuspended", false)
                .Build()
        );

        CompiledRule<RuleTestContext> viaBuilder = RuleBuilder
            .And(RuleBuilder.Predicate("isManager"), RuleBuilder.Not(RuleBuilder.Predicate("isSuspended")))
            .Compile(compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaDsl = compiler.Compile("isManager AND NOT isSuspended").CompiledRule!;

        Assert.Equal(viaDsl.CanonicalText, viaBuilder.CanonicalText);
    }

    /// <summary>The predicate builder with a named string argument compiles successfully.</summary>
    [Fact]
    public void Predicate_BuilderNamedStringArgument_CompilesSuccessfully_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasRole", ("role", "Y")).Compile(compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("hasRole(role: \"Y\")", result.CompiledRule!.CanonicalText);
    }

    /// <summary>The threshold builder compiles to the same canonical text as DSL text.</summary>
    [Fact]
    public void Threshold_Builder_CompilesToDslCanonicalText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompiledRule<RuleTestContext> viaBuilder = RuleBuilder
            .AtLeast(2, RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"))
            .Compile(compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaDsl = compiler.Compile("AtLeast(2, a, b, c)").CompiledRule!;

        Assert.Equal(viaDsl.CanonicalText, viaBuilder.CanonicalText);
    }

    /// <summary>The XOR and XNOR builders compile to the same canonical text as DSL text.</summary>
    [Fact]
    public void Xor_Builders_CompileToDslCanonicalText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        CompiledRule<RuleTestContext> viaXorBuilder = RuleBuilder
            .Xor(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaXnorBuilder = RuleBuilder
            .Xnor(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(compiler)
            .CompiledRule!;

        Assert.Equal("(a XOR b)", viaXorBuilder.CanonicalText);
        Assert.Equal("(a EQUIVALENT b)", viaXnorBuilder.CanonicalText);
    }

    /// <summary>An unknown predicate from the builder is a compile error, not an exception.</summary>
    [Fact]
    public void Predicate_Unknown_IsCompileErrorNotException_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("neverRegistered").Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownPredicate);
    }

    /// <summary>An out of range threshold from the builder is a compile error, not an exception.</summary>
    [Fact]
    public void Threshold_OutOfRange_IsCompileErrorNotException_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .AtLeast(0, RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidThresholdValue);
    }

    /// <summary>
    /// A diagnostic raised while compiling a builder tree (ticket 24) is located by a path into that tree, the
    /// same <c>$.operands[n]...</c> shape a hand-written JSON rule gets, and never by a <see cref="SourceSpan"/>
    /// into JSON the builder generated and the caller never sees.
    /// </summary>
    [Fact]
    public void Compile_UnknownPredicateNestedInAnAndBuilder_LocatesTheDiagnosticByABuilderPathWithNoSpan_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("neverRegistered"))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownPredicate);
        Assert.Equal("$.operands[1].predicate", diagnostic.Path);
        Assert.Equal(SourceSpan.None, diagnostic.Span);
    }

    /// <summary>
    /// A builder tree whose depth sits exactly at <see cref="CompilerOptions.MaxDepth"/>'s default (32) compiles
    /// through <see cref="RuleBuilder.Compile{TContext}(RuleCompiler{TContext})"/>'s JSON round trip (ticket 14),
    /// instead of its internal <c>JsonDocument.Parse(this.ToJson())</c> throwing at <see cref="JsonDocument"/>'s own,
    /// unrelated default nesting cap of 64.
    /// </summary>
    [Fact]
    public void Compile_NotChainAtDefaultMaxDepth_RoundTripsThroughJsonWithoutThrowing_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );
        RuleBuilder rule = RuleBuilder.Predicate("isManager");

        // 31 NOTs over a predicate term = tree depth 32, the default MaxDepth.
        for (int i = 0; i < 31; i++)
        {
            rule = RuleBuilder.Not(rule);
        }

        CompilationResult<RuleTestContext> result = rule.Compile(compiler);

        Assert.True(result.Succeeded);
    }

    /// <summary>
    /// A builder tree one level past <see cref="CompilerOptions.MaxDepth"/> reports
    /// <see cref="DiagnosticCodes.MaxDepthExceeded"/> through the same round trip, not a thrown
    /// <see cref="JsonException"/> (ticket 14): <see cref="RuleBuilder.Compile{TContext}(RuleCompiler{TContext})"/>
    /// never throws for an authoring error, per the pipeline's own contract.
    /// </summary>
    [Fact]
    public void Compile_NotChainOneLevelPastMaxDepth_ReportsMaxDepthExceededInsteadOfThrowing_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );
        RuleBuilder rule = RuleBuilder.Predicate("isManager");

        // 32 NOTs over a predicate term = tree depth 33, one past the default MaxDepth.
        for (int i = 0; i < 32; i++)
        {
            rule = RuleBuilder.Not(rule);
        }

        CompilationResult<RuleTestContext> result = rule.Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    /// <summary>The constant builder round trips through the same pipeline as DSL text.</summary>
    [Fact]
    public void Constant_Builder_RoundTripsThroughDslPipeline_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompiledRule<RuleTestContext> rule = RuleBuilder.Constant(true).Compile(compiler).CompiledRule!;

        Assert.Equal("True", rule.CanonicalText);
    }

    /// <summary>The OR builder evaluates true with at least one true operand.</summary>
    [Theory]
    [InlineData(true, false, TruthValue.True)]
    [InlineData(false, false, TruthValue.False)]
    public Task Or_Builder_EvaluatesTrueWithOneTrueOperand_Test(bool a, bool b, TruthValue expected)
    {
        return EvaluateAsync([a, b], operands => RuleBuilder.Or(operands), expected);
    }

    /// <summary>The ExactlyOne builder evaluates true if and only if exactly one operand is true.</summary>
    [Theory]
    [InlineData(false, false, false, TruthValue.False)]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public Task ExactlyOne_Builder_EvaluatesTrueIffExactlyOneTrue_Test(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.ExactlyOne(operands), expected);
    }

    /// <summary>The AtMost builder evaluates correctly near the boundary.</summary>
    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public Task AtMost_Builder_EvaluatesCorrectlyNearBoundary_Test(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.AtMost(1, operands), expected);
    }

    /// <summary>The GreaterThan builder evaluates correctly near the boundary.</summary>
    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public Task GreaterThan_Builder_EvaluatesCorrectlyNearBoundary_Test(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.GreaterThan(1, operands), expected);
    }

    /// <summary>The LessThan builder evaluates correctly near the boundary.</summary>
    [Theory]
    [InlineData(false, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public Task LessThan_Builder_EvaluatesCorrectlyNearBoundary_Test(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.LessThan(2, operands), expected);
    }

    /// <summary>The Exactly builder evaluates correctly near the boundary.</summary>
    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, true, true, TruthValue.False)]
    public Task Exactly_Builder_EvaluatesCorrectlyNearBoundary_Test(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.Exactly(2, operands), expected);
    }

    /// <summary>ToJson round trips to a rule that compiles and evaluates identically.</summary>
    [Fact]
    public void ToJson_Builder_RoundTripsToIdenticalRule_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddConstant("isSuspended", false)
                .Build()
        );
        RuleBuilder original = RuleBuilder.And(
            RuleBuilder.Predicate("isManager"),
            RuleBuilder.Not(RuleBuilder.Predicate("isSuspended"))
        );

        string json = original.ToJson();
        CompilationResult<RuleTestContext> reparsed = compiler.CompileJson(json);
        CompiledRule<RuleTestContext> viaOriginal = original.Compile(compiler).CompiledRule!;

        Assert.True(reparsed.Succeeded);
        Assert.Equal(viaOriginal.CanonicalText, reparsed.CompiledRule!.CanonicalText);
    }

    /// <summary>The predicate builder with a bool argument value compiles and evaluates correctly.</summary>
    [Fact]
    public async Task Predicate_BuilderBoolArgument_CompilesAndEvaluates_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddBooleanArgPredicate("hasFlag", "flag", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasFlag", ("flag", true)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The predicate builder with an int argument value compiles and evaluates correctly.</summary>
    [Fact]
    public async Task Predicate_BuilderIntArgument_CompilesAndEvaluates_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasCode",
                        "hasCode",
                        "True iff 'code' equals 7.",
                        [new PredicateArgumentSchema("code", "The code to compare against 7.", LiteralKind.Int64)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetInt64("code") == 7 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasCode", ("code", 7)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The predicate builder with a long argument value compiles and evaluates correctly.</summary>
    [Fact]
    public async Task Predicate_BuilderLongArgument_CompilesAndEvaluates_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasCode",
                        "hasCode",
                        "True iff 'code' equals 7.",
                        [new PredicateArgumentSchema("code", "The code to compare against 7.", LiteralKind.Int64)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetInt64("code") == 7 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasCode", ("code", 7L)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The predicate builder with a double argument value compiles and evaluates correctly.</summary>
    [Fact]
    public async Task Predicate_BuilderDoubleArgument_CompilesAndEvaluates_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDecimalArgPredicate("exceedsThreshold", "threshold", 12.5m)
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("exceedsThreshold", ("threshold", 12.5d))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The predicate builder with a decimal argument value compiles and evaluates correctly.</summary>
    [Fact]
    public async Task Predicate_BuilderDecimalArgument_CompilesAndEvaluates_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDecimalArgPredicate("exceedsThreshold", "threshold", 12.5m)
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("exceedsThreshold", ("threshold", 12.5m))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The predicate builder with a DateTimeOffset argument value compiles and evaluates correctly.</summary>
    [Fact]
    public async Task Predicate_BuilderDateTimeOffsetArgument_CompilesAndEvaluates_Test()
    {
        DateTimeOffset when = new(2024, 6, 1, 12, 30, 0, TimeSpan.Zero);
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDateTimeOffsetArgPredicate("occurredAt", "when", when).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("occurredAt", ("when", when)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The predicate builder with an array argument value compiles and evaluates correctly through ArrayToNode.</summary>
    [Fact]
    public async Task Predicate_BuilderArrayArgument_CompilesAndEvaluatesViaArrayToNode_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyCode",
                        "hasAnyCode",
                        "True iff any of 'codes' matches.",
                        [new PredicateArgumentSchema("codes", "The codes to check for.", LiteralKind.Int64Array)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetInt64Array("codes").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("hasAnyCode", ("codes", new object[] { 1, 2, 3 }))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>An unsupported argument value type throws ArgumentException naming the type.</summary>
    [Fact]
    public void Predicate_UnsupportedArgumentType_ThrowsNamingType_Test()
    {
        object unsupported = new StringBuilder("not a supported literal type");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            RuleBuilder.Predicate("anything", ("arg", unsupported)).ToJson()
        );

        Assert.Contains(unsupported.GetType().ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal("value", exception.ParamName);
    }

    private static async Task EvaluateAsync(bool[] operandValues, Func<RuleBuilder[], RuleBuilder> build, TruthValue expected)
    {
        PredicateRegistryBuilder<RuleTestContext> registryBuilder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        RuleBuilder[] operands = new RuleBuilder[operandValues.Length];
        for (int i = 0; i < operandValues.Length; i++)
        {
            string name = ((char)('a' + i)).ToString();
            registryBuilder = registryBuilder.AddConstant(name, operandValues[i]);
            operands[i] = RuleBuilder.Predicate(name);
        }

        RuleCompiler<RuleTestContext> compiler = new(registryBuilder.Build());
        CompiledRule<RuleTestContext> rule = build(operands).Compile(compiler).CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, decision.Result);
    }
}
