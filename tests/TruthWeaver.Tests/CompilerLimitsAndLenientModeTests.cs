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

    /// <summary>
    /// Pins the DSL parser's own nesting guard (ticket 03) at its exact boundary, not only at a wide margin: the
    /// guard's single entry per NOT, plus the one entry ParseOrExpression makes around the whole top-level
    /// expression, means a NOT chain of <c>MaxDepth - 1</c> NOTs peaks at exactly <c>MaxDepth</c> nesting levels and
    /// still compiles, while one NOT more peaks at <c>MaxDepth + 1</c> and is rejected by the parser's guard, before
    /// RuleNodeCompiler.Build's own (looser, by exactly this one level) structural-depth check ever runs.
    /// </summary>
    [Fact]
    public void Compile_NotChainAtExactDepthBoundary_CompilesOneMoreNotFails_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry, new CompilerOptions(MaxDepth: 5));
        string atBoundary = string.Concat(Enumerable.Repeat("NOT ", 4)) + "a";
        string oneMore = string.Concat(Enumerable.Repeat("NOT ", 5)) + "a";

        CompilationResult<RuleTestContext> atBoundaryResult = compiler.Compile(atBoundary);
        CompilationResult<RuleTestContext> oneMoreResult = compiler.Compile(oneMore);

        Assert.True(atBoundaryResult.Succeeded, string.Join("; ", atBoundaryResult.Diagnostics.Select(d => d.Message)));
        Assert.False(oneMoreResult.Succeeded);
        Assert.Contains(oneMoreResult.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    /// <summary>
    /// A NOT chain thousands of levels deep, well beyond the default MaxDepth, reports MaxDepthExceeded instead of
    /// overflowing the call stack with an uncatchable StackOverflowException (ticket 03). ParseNotExpression
    /// recurses once per NOT with no guard of its own, so this exercises that guard directly.
    /// </summary>
    [Fact]
    public void Compile_NotChainThousandsOfLevelsDeep_ReportsMaxDepthInsteadOfCrashing_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        string deeplyNested = string.Concat(Enumerable.Repeat("NOT ", 5_000)) + "a";

        CompilationResult<RuleTestContext> result = compiler.Compile(deeplyNested);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    /// <summary>
    /// A term wrapped in thousands of redundant, nested parentheses reports MaxDepthExceeded instead of overflowing
    /// the call stack (ticket 03). Parentheses add no tree depth on their own (ParsePrimary discards them, keeping
    /// only the widest group extent for notes), so this exercises the parser's own recursion guard, not
    /// RuleNodeCompiler.Build's existing check on the built tree.
    /// </summary>
    [Fact]
    public void Compile_TermWrappedInThousandsOfNestedParentheses_ReportsMaxDepthInsteadOfCrashing_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        string deeplyNested = new string('(', 5_000) + "a" + new string(')', 5_000);

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

    /// <summary>
    /// Ticket 10: a quantifier's body used to compile against a brand new <see cref="CompilerOptions.MaxNodeCount"/>
    /// allowance instead of extending the outer compile's running count (ADR-0010 line 175: "a quantifier counts as
    /// one node plus its body, once"), so several quantifiers each within budget on their own could combine into a
    /// tree far over the limit with no diagnostic. Three quantifiers, each with a body that alone fits comfortably
    /// under a small test budget, combine to more nodes than that budget and are now rejected.
    /// </summary>
    [Fact]
    public void Limits_SeveralQuantifiersEachWithinBudgetAlone_CombineOverMaxNodeCountAndAreRejected_Test()
    {
        RuleCompiler<QuantifierBudgetContext> compiler = CreateQuantifierCompiler(new CompilerOptions(MaxNodeCount: 20));

        // One quantifier: 1 (the quantifier itself) + 9 (1 AND node + 8 "elementTrue" terms) = 10 nodes, comfortably
        // under the 20-node budget. Three of them, AND'ed together: 1 (the outer AND) + 3 x 10 = 31 nodes, over it.
        string body = string.Join(" AND ", Enumerable.Repeat("elementTrue", 8));
        string oneQuantifier = $"FORALL(x IN items, {body})";
        string threeQuantifiers = string.Join(" AND ", Enumerable.Repeat(oneQuantifier, 3));

        CompilationResult<QuantifierBudgetContext> result = compiler.Compile(threeQuantifiers);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
    }

    /// <summary>
    /// Ticket 10: threading the running node count across the collection-binding boundary must not make a single
    /// quantifier whose body legitimately fits the budget fail to compile.
    /// </summary>
    [Fact]
    public void Limits_SingleQuantifierWithinBudget_StillCompiles_Test()
    {
        RuleCompiler<QuantifierBudgetContext> compiler = CreateQuantifierCompiler(new CompilerOptions(MaxNodeCount: 20));
        string body = string.Join(" AND ", Enumerable.Repeat("elementTrue", 8));
        string oneQuantifier = $"FORALL(x IN items, {body})"; // 10 nodes total, well within the 20-node budget.

        CompilationResult<QuantifierBudgetContext> result = compiler.Compile(oneQuantifier);

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
    }

    /// <summary>
    /// Pins ticket 10's running-node-count threading at its exact boundary: two quantifiers ANDed together, each
    /// body 1 (quantifier) + 1 (AND) + 8 ("elementTrue" terms) = 10 nodes, plus the outer AND, total exactly
    /// 1 + 2*10 = 21 nodes. A budget of exactly 21 still compiles; a budget one node smaller, 20, reports
    /// <see cref="DiagnosticCodes.MaxNodeCountExceeded"/>, instead of each quantifier body silently starting over
    /// at a fresh allowance and never adding its count back to the outer total.
    /// </summary>
    [Fact]
    public void Limits_TwoQuantifiersCombiningToExactlyMaxNodeCount_CompileOneFewerFails_Test()
    {
        string body = string.Join(" AND ", Enumerable.Repeat("elementTrue", 8));
        string oneQuantifier = $"FORALL(x IN items, {body})";
        string twoQuantifiers = string.Join(" AND ", Enumerable.Repeat(oneQuantifier, 2));

        CompilationResult<QuantifierBudgetContext> atBudget = CreateQuantifierCompiler(new CompilerOptions(MaxNodeCount: 21))
            .Compile(twoQuantifiers);
        CompilationResult<QuantifierBudgetContext> oneOver = CreateQuantifierCompiler(new CompilerOptions(MaxNodeCount: 20))
            .Compile(twoQuantifiers);

        Assert.True(atBudget.Succeeded, string.Join("; ", atBudget.Diagnostics.Select(d => d.Message)));
        Assert.Equal(21, atBudget.CompiledRule?.Metrics.NodeCount);
        Assert.False(oneOver.Succeeded);
        Assert.Contains(oneOver.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
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

    private static RuleCompiler<QuantifierBudgetContext> CreateQuantifierCompiler(CompilerOptions options)
    {
        PredicateRegistryBuilder<QuantifierBudgetContext> builder = PredicateRegistry<QuantifierBudgetContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                _ => Array.Empty<TruthValue>(),
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<QuantifierBudgetContext>(builder.Build(), options);
    }

    /// <summary>A marker application context: the node-budget tests above only compile rules, never evaluate them.</summary>
#pragma warning disable S2094 // Intentionally empty: a marker TContext, not a stand-in for an interface.
    private sealed class QuantifierBudgetContext;
#pragma warning restore S2094
}
