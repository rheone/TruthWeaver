namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>The <c>Try</c> forms return a rule and diagnostics without a result object, and the rule is non-null exactly when they return true.</summary>
public sealed class TryFormTests
{
    /// <summary>A rule that compiles gives true, the rule, and no diagnostics.</summary>
    [Fact]
    public void TryCompile_ValidText_ReturnsTrueWithRule_Test()
    {
        bool ok = Compiler()
            .TryCompile("a AND b", out CompiledRule<RuleTestContext>? rule, out EquatableArray<Diagnostic> diagnostics);

        Assert.True(ok);
        Assert.NotNull(rule);
        Assert.Equal("a AND b", rule.CanonicalText);
        Assert.Empty(diagnostics);
    }

    /// <summary>An authoring error gives false, no rule, and the error diagnostics.</summary>
    [Fact]
    public void TryCompile_UnknownPredicate_ReturnsFalseWithErrors_Test()
    {
        bool ok = Compiler()
            .TryCompile("missing", out CompiledRule<RuleTestContext>? rule, out EquatableArray<Diagnostic> diagnostics);

        Assert.False(ok);
        Assert.Null(rule);
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>TryCompileText behaves as TryCompile.</summary>
    [Fact]
    public void TryCompileText_ValidAndInvalidText_MatchesCompileText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = Compiler();

        Assert.True(compiler.TryCompileText("a OR b", out CompiledRule<RuleTestContext>? rule, out _));
        Assert.Equal("a OR b", rule.CanonicalText);
        Assert.False(
            compiler.TryCompileText("a OR", out CompiledRule<RuleTestContext>? none, out EquatableArray<Diagnostic> diagnostics)
        );
        Assert.Null(none);
        Assert.NotEmpty(diagnostics);
    }

    /// <summary>Both TryCompileJson overloads return the rule for valid JSON and false for invalid JSON.</summary>
    [Fact]
    public void TryCompileJson_TextAndElement_ReturnRuleOrFailure_Test()
    {
        RuleCompiler<RuleTestContext> compiler = Compiler();
        const string Json = """{ "predicate": "a" }""";
        using JsonDocument document = JsonDocument.Parse(Json);

        Assert.True(compiler.TryCompileJson(Json, out CompiledRule<RuleTestContext>? fromText, out _));
        Assert.True(compiler.TryCompileJson(document.RootElement, out CompiledRule<RuleTestContext>? fromElement, out _));
        Assert.Equal("a", fromText.CanonicalText);
        Assert.Equal("a", fromElement.CanonicalText);
        Assert.False(
            compiler.TryCompileJson("{", out CompiledRule<RuleTestContext>? none, out EquatableArray<Diagnostic> diagnostics)
        );
        Assert.Null(none);
        Assert.NotEmpty(diagnostics);
    }

    /// <summary>RuleBuilder.TryCompile gives the rule for a valid tree and false for an unknown predicate.</summary>
    [Fact]
    public void RuleBuilderTryCompile_ValidAndUnknownPredicate_ReturnsRuleOrFailure_Test()
    {
        RuleCompiler<RuleTestContext> compiler = Compiler();

        Assert.True(RuleBuilder.Predicate("a").TryCompile(compiler, out CompiledRule<RuleTestContext>? rule, out _));
        Assert.Equal("a", rule.CanonicalText);
        Assert.False(RuleBuilder.Predicate("missing").TryCompile(compiler, out CompiledRule<RuleTestContext>? none, out _));
        Assert.Null(none);
    }

    /// <summary>Each rewrite Try form returns true with the same rule as its result-returning twin.</summary>
    [Fact]
    public void TryRewrites_ValidRule_ReturnTrueWithTheSameRuleAsTheResultForm_Test()
    {
        CompiledRule<RuleTestContext> source = Compiler().Compile("NOT (a AND b) OR c").GetRuleOrThrow();

        Assert.True(source.TryExpandToPrimitives(out CompiledRule<RuleTestContext>? primitives, out _));
        Assert.Equal(source.ExpandToPrimitives().Rule!.CanonicalText, primitives.CanonicalText);
        Assert.True(source.TryExpandToNand(out CompiledRule<RuleTestContext>? nand, out _));
        Assert.Equal(source.ExpandToNand().Rule!.CanonicalText, nand.CanonicalText);
        Assert.True(source.TryExpandToNor(out CompiledRule<RuleTestContext>? nor, out _));
        Assert.Equal(source.ExpandToNor().Rule!.CanonicalText, nor.CanonicalText);
        Assert.True(source.TryCompressToDerived(out CompiledRule<RuleTestContext>? derived, out _));
        Assert.Equal(source.CompressToDerived().Rule!.CanonicalText, derived.CanonicalText);
        Assert.True(source.TryCanonicalize(out CompiledRule<RuleTestContext>? canonical, out _));
        Assert.Equal(source.Canonicalize().Rule!.CanonicalText, canonical.CanonicalText);
        Assert.True(source.TrySimplify(out CompiledRule<RuleTestContext>? simplified, out _));
        Assert.Equal(source.Simplify().Rule!.CanonicalText, simplified.CanonicalText);
        Assert.True(source.TryToNnf(out CompiledRule<RuleTestContext>? nnf, out _));
        Assert.Equal(source.ToNnf().Rule!.CanonicalText, nnf.CanonicalText);
        Assert.True(source.TryToCnf(out CompiledRule<RuleTestContext>? cnf, out _));
        Assert.Equal(source.ToCnf().Rule!.CanonicalText, cnf.CanonicalText);
        Assert.True(source.TryToDnf(out CompiledRule<RuleTestContext>? dnf, out _));
        Assert.Equal(source.ToDnf().Rule!.CanonicalText, dnf.CanonicalText);
    }

    /// <summary>A rewrite over the node cap returns false, no rule, and the TRE0016 error.</summary>
    [Fact]
    public void TryRewrites_ResultOverTheNodeCap_ReturnFalseWithRewriteTooLarge_Test()
    {
        CompiledRule<RuleTestContext> source = Compiler().Compile("(a AND b) OR (c AND d)").GetRuleOrThrow();

        Assert.False(
            source.TryExpandToPrimitives(
                out CompiledRule<RuleTestContext>? p,
                out EquatableArray<Diagnostic> pd,
                maxNodeCount: 1
            )
        );
        Assert.False(
            source.TryExpandToNand(out CompiledRule<RuleTestContext>? na, out EquatableArray<Diagnostic> nad, maxNodeCount: 1)
        );
        Assert.False(
            source.TryExpandToNor(out CompiledRule<RuleTestContext>? no, out EquatableArray<Diagnostic> nod, maxNodeCount: 1)
        );
        Assert.False(
            source.TryToNnf(out CompiledRule<RuleTestContext>? nn, out EquatableArray<Diagnostic> nnd, maxNodeCount: 1)
        );
        Assert.False(source.TryToCnf(out CompiledRule<RuleTestContext>? c, out EquatableArray<Diagnostic> cd, maxNodeCount: 1));
        Assert.False(source.TryToDnf(out CompiledRule<RuleTestContext>? d, out EquatableArray<Diagnostic> dd, maxNodeCount: 1));
        foreach (CompiledRule<RuleTestContext>? rule in new[] { p, na, no, nn, c, d })
        {
            Assert.Null(rule);
        }

        foreach (EquatableArray<Diagnostic> diagnostics in new[] { pd, nad, nod, nnd, cd, dd })
        {
            Assert.Contains(diagnostics, x => x.Code == DiagnosticCodes.RewriteTooLarge);
        }
    }

    /// <summary>A result with a warning and a rule returns true and keeps the warning in the diagnostics.</summary>
    [Fact]
    public void TryToNnf_WarningOnlyResult_ReturnsTrueWithTheWarning_Test()
    {
        CompiledRule<RuleTestContext> source = Compiler().Compile("a AND AtLeast(2, a, b, c, d)").GetRuleOrThrow();

        bool ok = source.TryToNnf(out CompiledRule<RuleTestContext>? rule, out EquatableArray<Diagnostic> diagnostics);

        Assert.True(ok);
        Assert.NotNull(rule);
        Diagnostic warning = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.ThresholdKeptAsAtom, warning.Code);
    }

    /// <summary>TryGetRule returns the rule and diagnostics of a success.</summary>
    [Fact]
    public void TryGetRule_OnSuccess_ReturnsTheCompiledRule_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler().Compile("a");

        Assert.True(result.TryGetRule(out CompiledRule<RuleTestContext>? rule, out EquatableArray<Diagnostic> diagnostics));
        Assert.Same(result.CompiledRule, rule);
        Assert.Equal(result.Diagnostics, diagnostics);
    }

    /// <summary>TryGetRule on a failure returns false, a null rule and the error diagnostics.</summary>
    [Fact]
    public void TryGetRule_OnFailure_ReturnsFalseWithErrors_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler().Compile("missing");

        Assert.False(result.TryGetRule(out CompiledRule<RuleTestContext>? rule, out EquatableArray<Diagnostic> diagnostics));
        Assert.Null(rule);
        Assert.Equal(result.Diagnostics, diagnostics);
    }

    private static RuleCompiler<RuleTestContext> Compiler()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", false)
            .AddConstant("d", true)
            .Build();
        return new RuleCompiler<RuleTestContext>(registry);
    }
}
