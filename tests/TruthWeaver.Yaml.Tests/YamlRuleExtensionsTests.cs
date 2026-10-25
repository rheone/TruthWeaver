namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;
using YamlDotNet.RepresentationModel;

public sealed class YamlRuleExtensionsTests
{
    /// <summary>CompileYaml compiles a single term.</summary>
    [Fact]
    public void CompileYaml_SingleTerm_CompilesRule_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: isManager");

        Assert.True(result.Succeeded);
        Assert.Equal("isManager", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A YAML <c>const</c> written as the letter T, F or U (either case) compiles to the same tree as the DSL text.</summary>
    [Theory]
    [InlineData("T", "T")]
    [InlineData("f", "F")]
    [InlineData("U", "U")]
    [InlineData("\"u\"", "U")]
    public void CompileYaml_ConstantLetter_EqualsDslConstant_Test(string letter, string dslText)
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());

        CompilationResult<YamlTestContext> fromYaml = compiler.CompileYaml($"const: {letter}");
        CompilationResult<YamlTestContext> fromDsl = compiler.Compile(dslText);

        Assert.True(fromYaml.Succeeded);
        Assert.Equal(fromDsl.CompiledRule!.CanonicalText, fromYaml.CompiledRule!.CanonicalText);
    }

    /// <summary>TryCompileYaml returns the rule for valid YAML text and false with diagnostics for an unknown predicate.</summary>
    [Fact]
    public void TryCompileYaml_ValidAndUnknownPredicate_ReturnsRuleOrFailure_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        Assert.True(compiler.TryCompileYaml("predicate: isManager", out CompiledRule<YamlTestContext>? rule, out _));
        Assert.Equal("isManager", rule.CanonicalText);
        Assert.False(
            compiler.TryCompileYaml(
                "predicate: neverRegistered",
                out CompiledRule<YamlTestContext>? none,
                out EquatableArray<Diagnostic> diagnostics
            )
        );
        Assert.Null(none);
        Assert.NotEmpty(diagnostics);
    }

    /// <summary>TryCompileYaml accepts a YamlNode extracted from a larger document.</summary>
    [Fact]
    public void TryCompileYaml_YamlNode_ReturnsRule_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );
        YamlStream stream = [];
        using StringReader reader = new("predicate: isManager");
        stream.Load(reader);

        bool ok = compiler.TryCompileYaml(stream.Documents[0].RootNode, out CompiledRule<YamlTestContext>? rule, out _);

        Assert.True(ok);
        Assert.NotNull(rule);
        Assert.Equal("isManager", rule.CanonicalText);
    }

    /// <summary>PrintYaml and CompileYaml round trip a compiled rule.</summary>
    [Fact]
    public void PrintYaml_CompiledRule_RoundTripsThroughCompileYaml_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddStringArgPredicate("hasRole", "role", "Y")
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>PrintYaml to a writer writes exactly the YAML that the string form returns, and a failing writer's exception reaches the caller unchanged.</summary>
    [Fact]
    public void PrintYaml_ToWriter_WritesTheStringFormAndPropagatesWriterFailure_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddStringArgPredicate("hasRole", "role", "Y")
                .Build()
        );
        CompiledRule<YamlTestContext> rule = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        using StringWriter writer = new();
        IOException failure = new("disk full");
        using ThrowingWriter failing = new(failure);

        rule.PrintYaml(writer);
        IOException thrown = Assert.Throws<IOException>(() => rule.PrintYaml(failing));

        Assert.Equal(rule.PrintYaml(), writer.ToString());
        Assert.Same(failure, thrown);
        Assert.Throws<ArgumentNullException>(() => rule.PrintYaml(null!));
    }

    /// <summary>CompileYaml reports a diagnostic rather than throwing for an unknown predicate.</summary>
    [Fact]
    public void CompileYaml_UnknownPredicate_ReportsDiagnostic_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: neverRegistered");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    /// <summary>
    /// An unquoted <c>null</c>, <c>~</c> or empty value as a predicate argument is rejected with the diagnostic JSON gives
    /// for <c>null</c>: the same code, expectation and path. It does not become the string "null".
    /// </summary>
    [Theory]
    [InlineData("null")]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("~")]
    [InlineData("")]
    public void CompileYaml_UnquotedNullArgument_IsRejectedLikeJsonNull_Test(string value)
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<YamlTestContext> yaml = compiler.CompileYaml($"predicate: hasRole\nargs:\n  role: {value}\n");
        CompilationResult<YamlTestContext> json = compiler.CompileJson("""{"predicate":"hasRole","args":{"role":null}}""");

        Assert.False(yaml.Succeeded);
        Diagnostic expected = Assert.Single(json.Diagnostics);
        Diagnostic actual = Assert.Single(yaml.Diagnostics);
        Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.Expected, actual.Expected);
        Assert.Equal(expected.Found, actual.Found);
        Assert.Equal(expected.Path, actual.Path);
    }

    /// <summary>A quoted <c>"null"</c> is the author's explicit string, so it still compiles as the text.</summary>
    [Fact]
    public void CompileYaml_QuotedNullArgument_IsTheString_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "null").Build()
        );

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: hasRole\nargs:\n  role: \"null\"\n");

        Assert.True(result.Succeeded);
        Assert.Equal("hasRole(role: \"null\")", result.CompiledRule!.CanonicalText);
    }

    /// <summary>
    /// A self-referencing YAML alias (an anchor aliased inside its own body, so YamlDotNet's loaded node graph is
    /// cyclic rather than a tree) reports MalformedTree instead of recursing forever and overflowing the call stack
    /// with an uncatchable StackOverflowException (ticket 03). Mirrors the <c>a: &amp;x [*x]</c> pattern already
    /// pinned for <see cref="YamlDataSource"/> in <c>YamlDataSourceTests</c>, through the rule-compiling path instead
    /// of the data-source path.
    /// </summary>
    [Fact]
    public void CompileYaml_SelfReferencingAlias_ReportsMalformedTreeInsteadOfHanging_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("&r\nop: not\noperands: [*r]\n");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>
    /// A YAML tree nested well beyond the default MaxDepth reports MaxDepthExceeded instead of overflowing the call
    /// stack while TreeFormatReader.ReadNode walks it (ticket 03), which recurses once per level with no guard of
    /// its own. The depth here is deliberately a few hundred, not thousands: YamlDotNet's own recursive-descent
    /// loader (YamlStream.Load, outside this fix's reach) has its own, lower stack-overflow ceiling for flow-style
    /// nesting, so a test that is "deep enough to prove the guard trips" must stay well under that ceiling too.
    /// </summary>
    [Fact]
    public void CompileYaml_NestedWellBeyondMaxDepth_ReportsMaxDepthInsteadOfCrashing_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());
        const int levels = 150;
        string deeplyNested =
            string.Concat(Enumerable.Repeat("{op: not, operands: [", levels))
            + "{const: true}"
            + string.Concat(Enumerable.Repeat("]}", levels));

        CompilationResult<YamlTestContext> result = compiler.CompileYaml(deeplyNested);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    /// <summary>
    /// A YAML tree only 6 anchor/alias levels deep (well within the default MaxDepth of 32), where each level is an
    /// <c>and</c> of 6 references to the one below (one of them the anchor's own definition, the rest aliases),
    /// expands to tens of thousands of nodes if fully materialized (roughly 6^6). YamlNodeCursor.ElementsOf walks an
    /// aliased node again in full on every reference with no count limit of its own, so this reports
    /// MaxNodeCountExceeded quickly instead of hanging or exhausting memory while TreeFormatReader reads it
    /// (ticket 05).
    /// </summary>
    [Fact]
    public void CompileYaml_ShallowHighFanoutAliasChain_ReportsMaxNodeCountInsteadOfExponentialBlowup_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().Build(),
            new CompilerOptions(MaxNodeCount: 50)
        );
        string yaml = BuildAliasFanoutYaml(levels: 6, fanout: 6);

        CompilationResult<YamlTestContext> result = compiler.CompileYaml(yaml);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
    }

    /// <summary>
    /// Pins the YAML alias node-count guard added for ticket 05 at its exact boundary: a 3-level, fanout-3 alias
    /// chain (<c>BuildAliasFanoutYaml(3, 3)</c>) visits exactly 40 nodes while reading (V(0)=1,
    /// V(k)=1+3*V(k-1)), computed independently of <c>YamlNodeCursor</c>. Unlike a <c>let</c>-definition chain,
    /// a YAML alias resolves straight to the aliased node with no extra indirection layer, so the visit count here
    /// equals the final compiled tree's node count. A budget of exactly 40 still compiles; a budget one node
    /// smaller, 39, reports <see cref="DiagnosticCodes.MaxNodeCountExceeded"/>.
    /// </summary>
    [Fact]
    public void CompileYaml_AliasChainExpandingToExactlyItsVisitBudget_CompilesButOneFewerFails_Test()
    {
        PredicateRegistry<YamlTestContext> registry = PredicateRegistry<YamlTestContext>.CreateBuilder().Build();
        string yaml = BuildAliasFanoutYaml(levels: 3, fanout: 3);

        CompilationResult<YamlTestContext> atBudget = new RuleCompiler<YamlTestContext>(
            registry,
            new CompilerOptions(MaxNodeCount: 40)
        ).CompileYaml(yaml);
        CompilationResult<YamlTestContext> oneOver = new RuleCompiler<YamlTestContext>(
            registry,
            new CompilerOptions(MaxNodeCount: 39)
        ).CompileYaml(yaml);

        Assert.True(atBudget.Succeeded, string.Join("; ", atBudget.Diagnostics.Select(d => d.Message)));
        Assert.Equal(40, atBudget.CompiledRule?.Metrics.NodeCount);
        Assert.False(oneOver.Succeeded);
        Assert.Contains(oneOver.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
    }

    /// <summary>
    /// Builds YAML tree-format text for a chain of <paramref name="levels"/> anchored <c>and</c> nodes, each
    /// referencing the one below it <paramref name="fanout"/> times (once as the anchor's own definition, the rest
    /// as aliases), so the fully expanded tree has roughly <paramref name="fanout"/>^<paramref name="levels"/> nodes
    /// even though the YAML nests only <paramref name="levels"/> levels deep.
    /// </summary>
    private static string BuildAliasFanoutYaml(int levels, int fanout)
    {
        string level = "&a0 {const: true}";
        for (int i = 1; i <= levels; i++)
        {
            string previousAnchor = $"a{i - 1}";
            List<string> operands = [level];
            operands.AddRange(Enumerable.Repeat($"*{previousAnchor}", fanout - 1));
            level = $"&a{i} {{op: and, operands: [{string.Join(", ", operands)}]}}";
        }

        return level;
    }

    /// <summary>A writer whose every write fails with a preset exception.</summary>
    private sealed class ThrowingWriter(Exception failure) : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(char value)
        {
            throw failure;
        }

        public override void Write(string? value)
        {
            throw failure;
        }
    }
}
