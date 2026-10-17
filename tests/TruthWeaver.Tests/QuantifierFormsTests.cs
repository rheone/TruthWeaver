namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// <c>FORALL</c>/<c>EXISTS</c> (ADR-0010) in rule text, JSON, YAML and <see cref="RuleBuilder"/> (ticket 22b-3):
/// the four forms compile to the same tree, every printer writes the node without throwing, and the printed
/// forms round-trip back to an equal tree.
/// </summary>
public sealed class QuantifierFormsTests
{
    /// <summary>Rule text, JSON, YAML and <see cref="RuleBuilder"/> all compile <c>FORALL(x IN items, elementTrue)</c> to the same tree.</summary>
    [Fact]
    public void Compile_ForAllAcrossRuleTextJsonYamlAndBuilder_ProducesTheSameTree_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        Expression fromText = Require(compiler.Compile("FORALL(x IN items, elementTrue)"));
        Expression fromJson = Require(
            compiler.CompileJson("""{"op":"forall","var":"x","in":"items","operands":[{"predicate":"elementTrue"}]}""")
        );
        Expression fromYaml = Require(
            compiler.CompileYaml(
                """
                op: forall
                var: x
                in: items
                operands:
                  - predicate: elementTrue
                """
            )
        );
        Expression fromBuilder = Require(
            RuleBuilder.ForAll("x", "items", RuleBuilder.Predicate("elementTrue")).Compile(compiler)
        );

        Assert.Equal(fromText, fromJson);
        Assert.Equal(fromText, fromYaml);
        Assert.Equal(fromText, fromBuilder);
    }

    /// <summary>Rule text, JSON, YAML and <see cref="RuleBuilder"/> all compile <c>EXISTS(x IN items, elementTrue)</c> to the same tree.</summary>
    [Fact]
    public void Compile_ExistsAcrossRuleTextJsonYamlAndBuilder_ProducesTheSameTree_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        Expression fromText = Require(compiler.Compile("EXISTS(x IN items, elementTrue)"));
        Expression fromJson = Require(
            compiler.CompileJson("""{"op":"exists","var":"x","in":"items","operands":[{"predicate":"elementTrue"}]}""")
        );
        Expression fromYaml = Require(
            compiler.CompileYaml(
                """
                op: exists
                var: x
                in: items
                operands:
                  - predicate: elementTrue
                """
            )
        );
        Expression fromBuilder = Require(
            RuleBuilder.Exists("x", "items", RuleBuilder.Predicate("elementTrue")).Compile(compiler)
        );

        Assert.Equal(fromText, fromJson);
        Assert.Equal(fromText, fromYaml);
        Assert.Equal(fromText, fromBuilder);
    }

    /// <summary>
    /// A quantifier nested inside <c>NOT</c>, and a quantifier head that repeats the contextual <c>IN</c> keyword twice
    /// (once per nesting level), both parse with no syntax error. The inner <c>FORALL</c> still fails to compile
    /// (<c>items</c> is not registered on the outer quantifier's own element registry - ADR-0010 decision 3's "a body
    /// term resolves only in the element registry ... of its own quantifier"), which is a semantic diagnostic, not a
    /// parse failure, so it is the one this test expects.
    /// </summary>
    [Fact]
    public void Compile_NestedQuantifierRuleText_ParsesWithNoSyntaxError_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();

        CompilationResult<QuantifierContext> result = compiler.Compile(
            "NOT EXISTS(x IN items, FORALL(y IN items, elementTrue))"
        );

        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownCollection);
    }

    /// <summary><c>CanonicalText</c> writes a quantifier as <c>FORALL(x IN items, elementTrue)</c> (ADR-0010 decision 2).</summary>
    [Fact]
    public void CanonicalText_ForAll_WritesVariableInCollectionAndBody_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll();

        Assert.Equal("FORALL(x IN items, elementTrue)", rule.CanonicalText);
    }

    /// <summary><c>PrintJson</c> writes the quantifier's <c>var</c>/<c>in</c> keys, and the result compiles back to an equal tree.</summary>
    [Fact]
    public void PrintJson_ForAll_RoundTripsToAnEqualTree_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        CompiledRule<QuantifierContext> rule = CompileForAll();

        string json = rule.PrintJson();
        Expression roundTripped = Require(compiler.CompileJson(json));

        Assert.Contains("\"var\"", json, StringComparison.Ordinal);
        Assert.Contains("\"in\"", json, StringComparison.Ordinal);
        Assert.Equal(rule.Root, roundTripped);
    }

    /// <summary><c>PrintYaml</c> writes the quantifier's <c>var</c>/<c>in</c> keys, and the result compiles back to an equal tree.</summary>
    [Fact]
    public void PrintYaml_Exists_RoundTripsToAnEqualTree_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        CompiledRule<QuantifierContext> rule = CompileExists();

        string yaml = rule.PrintYaml();
        Expression roundTripped = Require(compiler.CompileYaml(yaml));

        Assert.Equal(rule.Root, roundTripped);
    }

    /// <summary><c>PrintRuleText</c> round-trips through the DSL parser to an equal tree.</summary>
    [Fact]
    public void PrintRuleText_ForAll_RoundTripsToAnEqualTree_Test()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        CompiledRule<QuantifierContext> rule = CompileForAll();

        Expression roundTripped = Require(compiler.Compile(rule.PrintRuleText()));

        Assert.Equal(rule.Root, roundTripped);
    }

    /// <summary>Every tree-shaped printer writes a quantifier node without throwing (plain text, Mermaid, DOT, HTML).</summary>
    [Fact]
    public void Printers_ForAll_WriteTheQuantifierNodeWithoutThrowing_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll();

        string plain = rule.PrintPlainText();
        string mermaid = rule.PrintMermaid();
        string dot = rule.PrintDot();
        string html = rule.PrintHtml();

        Assert.Contains("FORALL x IN items", plain, StringComparison.Ordinal);
        Assert.Contains("FORALL x IN items", mermaid, StringComparison.Ordinal);
        Assert.Contains("FORALL x IN items", dot, StringComparison.Ordinal);
        Assert.Contains("FORALL x IN items", html, StringComparison.Ordinal);
    }

    /// <summary>The symbolic tree style renders a quantifier's head with <c>&#8704;</c>/<c>&#8712;</c> instead of the words.</summary>
    [Fact]
    public void PrintPlainText_ForAllSymbolicStyle_RendersTheSymbolicHead_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll();

        string plain = rule.PrintPlainText(showArgumentValues: true);
        string symbolic = PlainTextTreePrinter.Print(rule.Outline(), OperatorStyle.Symbolic);

        Assert.Contains("FORALL x IN items", plain, StringComparison.Ordinal);
        Assert.Contains("∀ x ∈ items", symbolic, StringComparison.Ordinal);
    }

    /// <summary>The equation printer writes a quantifier as <c>&#8704; x &#8712; items . (body)</c> in the Unicode dialect.</summary>
    [Fact]
    public void PrintEquation_ForAll_WritesTheUnicodeQuantifierForm_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll();

        string equation = rule.PrintEquation();

        Assert.Equal("∀ x ∈ items . (elementTrue)", equation);
    }

    /// <summary>The LaTeX equation dialect writes the quantifier with its own commands, not the Unicode glyphs.</summary>
    [Fact]
    public void PrintEquation_ForAllLatexDialect_WritesLatexQuantifierCommands_Test()
    {
        CompiledRule<QuantifierContext> rule = CompileForAll();

        string equation = rule.PrintEquation(EquationOptions.Default with { Dialect = EquationDialect.LaTeX });

        Assert.Contains(@"\forall", equation, StringComparison.Ordinal);
        Assert.Contains(@"\in", equation, StringComparison.Ordinal);
    }

    private static Expression Require(CompilationResult<QuantifierContext> result)
    {
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!.Root;
    }

    private static RuleCompiler<QuantifierContext> CreateCompiler()
    {
        PredicateRegistryBuilder<QuantifierContext> builder = PredicateRegistry<QuantifierContext>
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
        return new RuleCompiler<QuantifierContext>(builder.Build());
    }

    private static CompiledRule<QuantifierContext> CompileForAll()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        CompilationResult<QuantifierContext> result = RuleBuilder
            .ForAll("x", "items", RuleBuilder.Predicate("elementTrue"))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }

    private static CompiledRule<QuantifierContext> CompileExists()
    {
        RuleCompiler<QuantifierContext> compiler = CreateCompiler();
        CompilationResult<QuantifierContext> result = RuleBuilder
            .Exists("x", "items", RuleBuilder.Predicate("elementTrue"))
            .Compile(compiler);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }

    /// <summary>
    /// A marker application context: this test file only compiles and prints rules structurally, never evaluates them,
    /// so the collection's selector never actually runs and needs no real data to return.
    /// </summary>
#pragma warning disable S2094 // Intentionally empty: a marker TContext, not a stand-in for an interface.
    private sealed class QuantifierContext;
#pragma warning restore S2094
}
