namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The expanding rewrites (<c>ExpandToPrimitives</c>, <c>ExpandToNand</c>, <c>ExpandToNor</c>) can produce a tree far
/// larger than the rule they started from, so each is capped by <see cref="CompilerOptions.MaxRewriteNodeCount"/> and
/// reports an over-cap result as a <c>TRE0016</c> diagnostic instead of exhausting memory or time.
/// </summary>
public sealed class RewriteResourceLimitsTests
{
    /// <summary>A rule whose expansion is exactly <paramref name="size"/> nodes is accepted when the cap equals that size.</summary>
    [Theory]
    [InlineData("primitives", "a XOR b", 9)]
    [InlineData("nand", "a AND b", 7)]
    [InlineData("nor", "a OR b", 7)]
    public void Expand_ResultExactlyAtTheCap_Succeeds_Test(string rewrite, string ruleText, int size)
    {
        // Arrange
        CompiledRule<RuleTestContext> rule = Compile(ruleText, 2);

        // Act
        CompilationResult<RuleTestContext> result = Expand(rule, rewrite, size);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>One node over the cap is the first refusal: no rule, and a single error diagnostic naming the cap.</summary>
    [Theory]
    [InlineData("primitives", "a XOR b", 9)]
    [InlineData("nand", "a AND b", 7)]
    [InlineData("nor", "a OR b", 7)]
    public void Expand_ResultOneNodeOverTheCap_FailsWithADiagnosticAndNoRule_Test(string rewrite, string ruleText, int size)
    {
        // Arrange
        CompiledRule<RuleTestContext> rule = Compile(ruleText, 2);

        // Act
        CompilationResult<RuleTestContext> result = Expand(rule, rewrite, size - 1);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.RewriteTooLarge, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains(
            (size - 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            diagnostic.Message,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// A wide threshold in gate-only form needs C(n, k) subsets; at the default cap it is refused up front (the subsets
    /// are never built), for both gates.
    /// </summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public void Expand_WideThresholdInGateForm_IsRefusedAtTheDefaultCap_Test(string gate)
    {
        // Arrange: C(14, 7) = 3432 subsets of 7 operands is far beyond the default cap.
        CompiledRule<RuleTestContext> rule = Compile("AtLeast(7, a, b, c, d, e, f, g, h, i, j, k, l, m, n)", 14);

        // Act
        CompilationResult<RuleTestContext> result = Expand(rule, gate, maxNodeCount: null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(DiagnosticCodes.RewriteTooLarge, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>Raising the cap lets a threshold a smaller cap refuses go through, and the result still evaluates equal.</summary>
    [Fact]
    public async Task ExpandToNand_WideThresholdWithARaisedCap_SucceedsAndEvaluatesEqual_Test()
    {
        // Arrange
        K3Rule original = K3Rule.TryCreate("AtLeast(3, a, b, c, d, e)", 5)!;
        Assert.False(original.Compiled.ExpandToNand(100).Succeeded);

        // Act
        K3Rule expanded = original.Rewrite(rule => rule.ExpandToNand(1_000_000).CompiledRule!);

        // Assert
        TruthValue[] assignment = [.. Enumerable.Repeat(TruthValue.True, 3), .. Enumerable.Repeat(TruthValue.False, 2)];
        Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
        Decision after = await expanded.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
        Assert.Equal(before.Result, after.Result);
    }

    /// <summary>
    /// A rule compiled with a raised <c>MaxRewriteNodeCount</c> expands past the default cap of 100,000 with no argument.
    /// </summary>
    [Fact]
    public void ExpandToPrimitives_RuleCompiledWithARaisedCap_ExpandsPastTheDefaultCapWithoutAnArgument_Test()
    {
        // Arrange: this nesting expands to well over 100,000 printed nodes.
        string text = "a";
        for (int level = 0; level < 14; level++)
        {
            text = $"({text}) XOR b";
        }

        PredicateRegistryBuilder<RuleTestContext> registry = PredicateRegistry<RuleTestContext>.CreateBuilder();
        registry.AddConstant("a", true);
        registry.AddConstant("b", true);
        RuleCompiler<RuleTestContext> compiler = new(
            registry.Build(),
            options: new CompilerOptions(MaxDepth: 64, MaxNodeCount: 4096, MaxRewriteNodeCount: 1_000_000)
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile(text).CompiledRule!;

        // Act
        CompilationResult<RuleTestContext> result = rule.ExpandToPrimitives();

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    /// Nested XORs repeat their operands at every level, so the expanded tree is exponential in the nesting depth; the
    /// default cap refuses it rather than walking an enormous tree.
    /// </summary>
    [Fact]
    public void ExpandToPrimitives_DeeplyNestedXor_IsRefusedAtTheDefaultCap_Test()
    {
        // Arrange: each level roughly doubles the printed size.
        string text = "a";
        for (int level = 0; level < 14; level++)
        {
            text = $"({text}) XOR b";
        }

        CompiledRule<RuleTestContext> rule = Compile(text, 2);

        // Act
        CompilationResult<RuleTestContext> result = rule.ExpandToPrimitives();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(DiagnosticCodes.RewriteTooLarge, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>An ordinary rule is expanded at the default cap exactly as before.</summary>
    [Fact]
    public void ExpandToPrimitives_OrdinaryRule_SucceedsAtTheDefaultCap_Test()
    {
        // Arrange
        CompiledRule<RuleTestContext> rule = Compile("a IMPLIES (b XOR c)", 3);

        // Act
        CompilationResult<RuleTestContext> result = rule.ExpandToPrimitives();

        // Assert
        Assert.True(result.Succeeded);
    }

    private static CompiledRule<RuleTestContext> Compile(string ruleText, int arity)
    {
        return K3Rule.TryCreate(ruleText, arity)!.Compiled;
    }

    private static CompilationResult<RuleTestContext> Expand(
        CompiledRule<RuleTestContext> rule,
        string rewrite,
        int? maxNodeCount
    )
    {
        return rewrite switch
        {
            "primitives" => rule.ExpandToPrimitives(maxNodeCount),
            "nand" => rule.ExpandToNand(maxNodeCount),
            _ => rule.ExpandToNor(maxNodeCount),
        };
    }
}
