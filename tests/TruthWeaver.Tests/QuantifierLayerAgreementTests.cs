namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing.Fuzzing;

/// <summary>
/// Layer agreement for <c>FORALL</c> and <c>EXISTS</c>. Each rule below has an expected value built only from
/// <see cref="K3Oracle"/>, and every layer (the evaluator, each rewrite, the printed forms and the decision renderers)
/// is held to it for every K3 value of the two terms and every element list of up to three elements. The generated-rule
/// tests cover the connectives and cardinality operators; this class covers the quantifiers, which that generator does
/// not produce.
/// </summary>
public sealed class QuantifierLayerAgreementTests
{
    private const int MaxElements = 3;
    private const int MaxReported = 12;

    private static readonly (string Text, Func<QuantifierContext, TruthValue> Expected)[] Rules =
    [
        ("FORALL(x IN items, elementTrue)", c => K3Oracle.And(c.Items)),
        ("EXISTS(x IN items, elementTrue)", c => K3Oracle.Or(c.Items)),
        ("NOT FORALL(x IN items, elementTrue)", c => K3Oracle.Not(K3Oracle.And(c.Items))),
        ("a AND EXISTS(x IN items, NOT elementTrue)", c => K3Oracle.And([c.A, K3Oracle.Or(c.Items.Select(K3Oracle.Not))])),
        ("FORALL(x IN items, elementTrue) XOR a", c => K3Oracle.Xor(K3Oracle.And(c.Items), c.A)),
        (
            "b IMPLIES FORALL(x IN items, IsUnknown(elementTrue))",
            c => K3Oracle.Implies(c.B, K3Oracle.And(c.Items.Select(K3Oracle.IsUnknown)))
        ),
        (
            "If(a, FORALL(x IN items, elementTrue), EXISTS(x IN items, elementTrue))",
            c => K3Oracle.If(c.A, K3Oracle.And(c.Items), K3Oracle.Or(c.Items))
        ),
        ("ANY(a, b, EXISTS(x IN items, elementTrue))", c => K3Oracle.Any([c.A, c.B, K3Oracle.Or(c.Items)])),
        (
            "AtLeast(2, a, b, FORALL(x IN items, elementTrue))",
            c => K3Oracle.Cardinality(count => count >= 2, [c.A, c.B, K3Oracle.And(c.Items)])
        ),
        ("COALESCE(FORALL(x IN items, elementTrue), a)", c => K3Oracle.Coalesce([K3Oracle.And(c.Items), c.A])),
        (
            "FORALL(x IN items, elementTrue) NAND EXISTS(x IN items, elementTrue)",
            c => K3Oracle.Nand(K3Oracle.And(c.Items), K3Oracle.Or(c.Items))
        ),
        ("a OR NOT EXISTS(x IN items, elementTrue)", c => K3Oracle.Or([c.A, K3Oracle.Not(K3Oracle.Or(c.Items))])),
    ];

    private static readonly (
        string Name,
        Func<CompiledRule<QuantifierContext>, CompilationResult<QuantifierContext>> Apply
    )[] Rewrites =
    [
        ("Simplify", r => r.Simplify()),
        ("Canonicalize", r => r.Canonicalize()),
        ("CompressToDerived", r => r.CompressToDerived()),
        ("ExpandToPrimitives", r => r.ExpandToPrimitives()),
        ("ExpandToNand", r => r.ExpandToNand()),
        ("ExpandToNor", r => r.ExpandToNor()),
        ("ToNnf", r => r.ToNnf()),
        ("ToCnf", r => r.ToCnf()),
        ("ToDnf", r => r.ToDnf()),
    ];

    /// <summary>Every quantifier rule evaluates to its oracle value for every term value and element list.</summary>
    [Fact]
    public async Task EvaluateAsync_QuantifierRules_MatchTheOracleForEveryContext_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        List<string> mismatches = [];

        foreach ((string text, Func<QuantifierContext, TruthValue> expected) in Rules)
        {
            CompiledRule<QuantifierContext> rule = Compile(compiler, text);
            await CollectMismatchesAsync(rule, text, "evaluate", expected, mismatches);
        }

        Assert.Empty(mismatches);
    }

    /// <summary>Each rewrite that the engine documents as value-preserving returns a rule that still matches the oracle.</summary>
    [Theory]
    [InlineData("Simplify")]
    [InlineData("Canonicalize")]
    [InlineData("CompressToDerived")]
    [InlineData("ExpandToPrimitives")]
    [InlineData("ToNnf")]
    [InlineData("ToCnf")]
    [InlineData("ToDnf")]
    public Task Rewrite_QuantifierRules_KeepTheOracleValueForEveryContext_Test(string rewrite)
    {
        return AssertRewriteKeepsValueAsync(rewrite);
    }

    /// <summary>
    /// <c>ExpandToNand</c> and <c>ExpandToNor</c> keep a quantifier whole, as <c>ExpandToPrimitives</c> does, and return
    /// a rule that still matches the oracle. Ultra review ticket 07: today they throw on any quantifier.
    /// </summary>
    [Theory]
    [InlineData("ExpandToNand")]
    [InlineData("ExpandToNor")]
    public Task Rewrite_QuantifierRulesUnderAUniversalGate_KeepTheOracleValueForEveryContext_Test(string rewrite)
    {
        return AssertRewriteKeepsValueAsync(rewrite);
    }

    /// <summary>A rule printed as canonical text or as JSON and compiled again evaluates to the same oracle value.</summary>
    [Theory]
    [InlineData("canonical text")]
    [InlineData("json")]
    public async Task Compile_PrintedQuantifierRules_MatchTheOracleForEveryContext_Test(string form)
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        List<string> mismatches = [];

        foreach ((string text, Func<QuantifierContext, TruthValue> expected) in Rules)
        {
            CompiledRule<QuantifierContext> original = Compile(compiler, text);
            CompilationResult<QuantifierContext> again =
                form == "json" ? compiler.CompileJson(original.PrintJson()) : compiler.Compile(original.CanonicalText);
            if (again.CompiledRule is not { } reprinted)
            {
                mismatches.Add(
                    $"{text}: the {form} form does not compile: {string.Join("; ", again.Diagnostics.Select(d => d.Message))}"
                );
                continue;
            }

            await CollectMismatchesAsync(reprinted, text, form, expected, mismatches);
        }

        Assert.Empty(mismatches);
    }

    /// <summary>
    /// Rendering an evaluated decision of a quantifier rule never throws, whatever the element count. Ultra review
    /// ticket 08: today the renderers throw when a quantifier was evaluated.
    /// </summary>
    [Fact]
    public async Task Print_DecisionOfAQuantifierRule_DoesNotThrowForAnyElementCount_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        foreach ((string text, _) in Rules)
        {
            CompiledRule<QuantifierContext> rule = Compile(compiler, text);
            foreach (QuantifierContext context in Contexts().Where(c => c.A == TruthValue.Unknown && c.B == TruthValue.True))
            {
                Decision decision = await rule.EvaluateAsync(context, cancellationToken: TestContext.Current.CancellationToken);
                Assert.NotEmpty(rule.PrintPlainText(decision));
                Assert.NotEmpty(rule.PrintHtml(decision));
                Assert.NotEmpty(rule.PrintMermaid(decision));
                Assert.NotEmpty(rule.PrintDot(decision));
            }
        }
    }

    private static async Task AssertRewriteKeepsValueAsync(string rewrite)
    {
        Func<CompiledRule<QuantifierContext>, CompilationResult<QuantifierContext>> apply = Rewrites
            .Single(r => r.Name == rewrite)
            .Apply;
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        List<string> mismatches = [];

        foreach ((string text, Func<QuantifierContext, TruthValue> expected) in Rules)
        {
            CompiledRule<QuantifierContext> original = Compile(compiler, text);
            CompilationResult<QuantifierContext> result = apply(original);
            if (result.CompiledRule is not { } rewritten)
            {
                mismatches.Add($"{text}: {rewrite} failed: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");
                continue;
            }

            await CollectMismatchesAsync(rewritten, text, rewrite, expected, mismatches);
        }

        Assert.Empty(mismatches);
    }

    private static async Task CollectMismatchesAsync(
        CompiledRule<QuantifierContext> rule,
        string text,
        string layer,
        Func<QuantifierContext, TruthValue> expected,
        List<string> mismatches
    )
    {
        foreach (QuantifierContext context in Contexts())
        {
            if (mismatches.Count >= MaxReported)
            {
                return;
            }

            Decision decision = await rule.EvaluateAsync(context, cancellationToken: TestContext.Current.CancellationToken);
            TruthValue want = expected(context);
            if (decision.Result != want)
            {
                mismatches.Add(
                    $"{text} [{layer}] a={context.A} b={context.B} items=[{string.Join(",", context.Items)}]: got {decision.Result}, oracle {want}"
                );
            }
        }
    }

    // Every K3 value of the two terms, with every element list of up to MaxElements elements (including none).
    private static IEnumerable<QuantifierContext> Contexts()
    {
        foreach (TruthValue a in K3Oracle.Values)
        {
            foreach (TruthValue b in K3Oracle.Values)
            {
                for (int count = 0; count <= MaxElements; count++)
                {
                    foreach (TruthValue[] items in K3Oracle.Assignments(count))
                    {
                        yield return new QuantifierContext
                        {
                            A = a,
                            B = b,
                            Items = items,
                        };
                    }
                }
            }
        }
    }

    private static CompiledRule<QuantifierContext> Compile(RuleCompiler<QuantifierContext> compiler, string text)
    {
        CompilationResult<QuantifierContext> result = compiler.Compile(text);
        Assert.True(result.Succeeded, $"{text}: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");
        return result.CompiledRule!;
    }

    private static RuleCompiler<QuantifierContext> CreateCompiler()
    {
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
            .CreateBuilder()
            .Add(PredicateSchema.NoArguments("a", "a", "The A input."), (context, _, _) => ValueTask.FromResult(context.A))
            .Add(PredicateSchema.NoArguments("b", "b", "The B input."), (context, _, _) => ValueTask.FromResult(context.B))
            .AddCollection(
                "items",
                context => context.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "The element itself, as a K3 value."),
                        (element, _, _) => ValueTask.FromResult(element.Item)
                    )
            );
        return new RuleCompiler<QuantifierContext>(builder.Build());
    }

    /// <summary>The two K3 inputs and the element list that the rules read.</summary>
    private sealed class QuantifierContext
    {
        public TruthValue A { get; init; }

        public TruthValue B { get; init; }

        public IReadOnlyList<TruthValue> Items { get; init; } = [];
    }
}
