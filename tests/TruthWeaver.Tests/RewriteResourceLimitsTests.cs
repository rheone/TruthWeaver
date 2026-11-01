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
        CompiledRule<RuleTestContext> rule = Compile("AtLeast(7, a, b, c, d, e, g, h, i, j, k, l, m, n, o)", 14);

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
    /// A threshold tree is small as written (one node per operand), but <c>NandNorExpander</c> checks a lower-bound
    /// cost estimate - the subset slots the threshold expansions alone would add - before attempting the (expensive)
    /// actual conversion, so a wide-enough threshold is refused up front even though the raw tree itself is nowhere
    /// near the cap.
    /// </summary>
    [Fact]
    public void ExpandToNand_WideThresholdUnderTheSizeCapButOverTheCostEstimate_IsRefusedWithoutBuildingTheExpansion_Test()
    {
        // Arrange: 15 nodes as written (one threshold plus 14 operands) is far under the cap of 50; the estimated
        // subset-slot cost, C(14, 7) * 7 = 24,024, is not.
        CompiledRule<RuleTestContext> rule = Compile("AtLeast(7, a, b, c, d, e, g, h, i, j, k, l, m, n, o)", 14);

        // Act
        CompilationResult<RuleTestContext> result = rule.ExpandToNand(50);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Equal(DiagnosticCodes.RewriteTooLarge, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>
    /// <c>Exactly</c>'s cost estimate sums two independent sides, each guarded so a vacuous side (no operand-count
    /// threshold of that shape exists) contributes nothing: <c>K == 0</c> makes the "at least K" side vacuous, and
    /// <c>K == n</c> (the operand count) makes the "at least K + 1" side vacuous. Both boundaries still expand to a
    /// rule equivalent to the original, proving neither guard misestimates the cost or breaks the expansion.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task ExpandToNand_ExactlyAtTheLowOrHighOperandCountBoundary_ExpandsToAnEquivalentRule_Test(int k)
    {
        // Arrange
        K3Rule original = K3Rule.TryCreate($"Exactly({k}, a, b, c, d)", 4)!;

        // Act
        K3Rule expanded = original.Rewrite(rule => rule.ExpandToNand().CompiledRule!);

        // Assert
        TruthValue[] assignment = [TruthValue.True, TruthValue.True, TruthValue.False, TruthValue.False];
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

    /// <summary>
    /// The two caps are independent: a rewrite result under <c>MaxRewriteNodeCount</c> can still be over
    /// <c>MaxNodeCount</c>, so its canonical text fails to compile until <c>MaxNodeCount</c> is raised.
    /// </summary>
    [Fact]
    public void ExpandToPrimitives_ResultOverMaxNodeCount_RecompilesOnlyWithARaisedMaxNodeCount_Test()
    {
        // Arrange: "a XOR b" is 3 nodes; its expansion is 9, which is under the rewrite cap but over a node cap of 8.
        PredicateRegistryBuilder<RuleTestContext> registry = PredicateRegistry<RuleTestContext>.CreateBuilder();
        registry.AddConstant("a", true);
        registry.AddConstant("b", true);
        PredicateRegistry<RuleTestContext> built = registry.Build();
        RuleCompiler<RuleTestContext> narrow = new(built, options: new CompilerOptions(MaxNodeCount: 8));
        RuleCompiler<RuleTestContext> raised = new(built, options: new CompilerOptions(MaxNodeCount: 9));
        string expandedText = narrow.Compile("a XOR b").CompiledRule!.ExpandToPrimitives().CompiledRule!.CanonicalText;

        // Act
        CompilationResult<RuleTestContext> overTheCap = narrow.Compile(expandedText);
        CompilationResult<RuleTestContext> withRaisedCap = raised.Compile(expandedText);

        // Assert
        Assert.False(overTheCap.Succeeded);
        Assert.Contains(overTheCap.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.MaxNodeCountExceeded);
        Assert.True(withRaisedCap.Succeeded);
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
