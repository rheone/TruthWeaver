namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 09: compiler resource limits and <see cref="CompilationMode.Lenient"/>.</summary>
public sealed class CompilerLimitsAndLenientModeTests
{
    /// <summary>The default options are the documented limits, in strict mode.</summary>
    [Fact]
    public void Defaults_Options_AreLimitsAndStrictMode_Test()
    {
        CompilerOptions defaults = CompilerOptions.Default;

        Assert.Equal(32, defaults.MaxDepth);
        Assert.Equal(512, defaults.MaxNodeCount);
        Assert.Equal(20, defaults.MaxAnalysisTerms);
        Assert.Equal(CompilationMode.Strict, defaults.Mode);
    }

    /// <summary>A rule that exceeds the maximum depth is a compile error.</summary>
    [Fact]
    public void Limits_ExceedingMaxDepth_IsCompileError_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry, new CompilerOptions(MaxDepth: 5));
        string deeplyNested = string.Concat(Enumerable.Repeat("NOT ", 10)) + "a";

        CompilationResult<RuleTestContext> result = compiler.Compile(deeplyNested);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    /// <summary>A rule that exceeds the maximum node count is a compile error.</summary>
    [Fact]
    public void Limits_ExceedingMaxNodeCount_IsCompileError_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < 60; i++)
        {
            builder = builder.AddConstant($"p{i}", true);
        }

        RuleCompiler<RuleTestContext> compiler = new(builder.Build(), new CompilerOptions(MaxNodeCount: 10));
        string manyTerms = string.Join(" AND ", Enumerable.Range(0, 60).Select(i => $"p{i}"));

        CompilationResult<RuleTestContext> result = compiler.Compile(manyTerms);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
    }

    /// <summary>A rule with more distinct terms than the analysis cap compiles and reports an information skip.</summary>
    [Fact]
    public void Limits_MoreTermsThanAnalysisCap_CompilesAndReportsInfo_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < 5; i++)
        {
            builder = builder.AddConstant($"p{i}", true);
        }

        RuleCompiler<RuleTestContext> compiler = new(builder.Build(), new CompilerOptions(MaxAnalysisTerms: 3));
        string rule = string.Join(" OR ", Enumerable.Range(0, 5).Select(i => $"p{i}"));

        CompilationResult<RuleTestContext> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.AnalysisSkippedTooManyTerms, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    /// <summary>Lenient mode compiles an unregistered predicate to a permanently unknown term.</summary>
    [Fact]
    public void Lenient_UnregisteredPredicate_CompilesToUnknownTerm_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>An unknown term in lenient mode evaluates to unknown at runtime.</summary>
    [Fact]
    public async Task Lenient_UnknownTerm_EvaluatesToUnknown_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("noSuchPredicate").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>
    /// An unregistered predicate in lenient mode records one fault on its term, even when the rule repeats the term, so
    /// the decision says why the result is Unknown (ADR-0002).
    /// </summary>
    [Fact]
    public async Task Lenient_UnknownTerm_RecordsOneFaultOnTheTerm_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("noSuchPredicate OR noSuchPredicate").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("noSuchPredicate", fault.Term.PredicateName);
        Assert.Contains("noSuchPredicate", fault.Exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Lenient mode compiles a heterogeneous array literal against an unregistered predicate without error.</summary>
    [Fact]
    public void Lenient_HeterogeneousArray_CompilesWithoutError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate(values: [1, \"two\", 3])");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>A heterogeneous array literal term in lenient mode evaluates to unknown at runtime.</summary>
    [Fact]
    public async Task Lenient_HeterogeneousArray_EvaluatesToUnknown_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("noSuchPredicate(values: [1, \"two\", 3])").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>
    /// A node that fails validation is replaced by an Unknown constant (never False), so a failure
    /// can never look like a negative answer even to code that inspects the intermediate tree.
    /// </summary>
    [Fact]
    public void Placeholder_WhenSubstitutingAFailedNode_IsAnUnknownConstant_Test()
    {
        // Arrange / Act
        Ast.Expression placeholder = FailedNode.Placeholder;

        // Assert
        Ast.ConstantExpression constant = Assert.IsType<Ast.ConstantExpression>(placeholder);
        Assert.Equal(TruthValue.Unknown, constant.Value);
    }

    /// <summary>Strict mode rejects the same rule text that lenient mode accepts.</summary>
    [Fact]
    public void Strict_IdenticalRuleText_IsRejectedWhereLenientAccepts_Test()
    {
        RuleCompiler<RuleTestContext> strictCompiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = strictCompiler.Compile("noSuchPredicate");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>Lenient mode reports a reserved word used as a term as a diagnostic that names the word, and never throws.</summary>
    /// <param name="source">The rule text.</param>
    /// <param name="word">The reserved word.</param>
    [Theory]
    [InlineData("a AND OR", "OR")]
    [InlineData("a AND AND", "AND")]
    public void Lenient_ReservedWordAsTerm_IsDiagnosticNamingTheWord_Test(string source, string word)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile(source);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Message.Contains(word));
    }

    /// <summary>Lenient mode reports a JSON term named with a reserved word as an UnknownPredicate error.</summary>
    /// <param name="word">The reserved word.</param>
    [Theory]
    [InlineData("T")]
    [InlineData("F")]
    [InlineData("U")]
    [InlineData("OR")]
    public void Lenient_ReservedWordAsJsonTerm_IsUnknownPredicateError_Test(string word)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );

        CompilationResult<RuleTestContext> result = compiler.CompileJson($"{{\"predicate\": \"{word}\"}}");

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, error.Code);
        Assert.Contains($"'{word}'", error.Message);
    }
}
