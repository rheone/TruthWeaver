namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// <c>PARITY(a, b, ...)</c> is n-ary parity (ADR-0005 decision 4): a first-class function-call node that is
/// <c>Unknown</c> whenever any operand is <c>Unknown</c>. Binary <c>XOR</c> with three or more operands stays a
/// compile error whose message points at <c>PARITY</c>; <c>ExactlyOne</c> is unchanged and is a different operation.
/// </summary>
public sealed class ParityTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>PARITY over 2..4 operands, in upper, lower and mixed case, matches the oracle's parity for every assignment.</summary>
    [Theory]
    [InlineData("PARITY")]
    [InlineData("parity")]
    [InlineData("Parity")]
    public async Task Evaluate_ParityOverAllAssignments_MatchesOracle_Test(string keyword)
    {
        for (int arity = 2; arity <= 4; arity++)
        {
            string names = string.Join(", ", Enumerable.Range(0, arity).Select(i => (char)('a' + i)));
            K3Rule rule = K3Rule.TryCreate($"{keyword}({names})", arity)!;

            foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
            {
                Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                Assert.Equal(K3Oracle.Parity(assignment), actual.Result);
            }
        }
    }

    /// <summary>Parity, not exactly-one: three True operands make PARITY True but ExactlyOne False.</summary>
    [Fact]
    public async Task Evaluate_ParityWithThreeTrueOperands_DiffersFromExactlyOne_Test()
    {
        K3Rule parityRule = K3Rule.TryCreate("PARITY(a, b, c)", 3)!;
        K3Rule exactlyOne = K3Rule.TryCreate("ExactlyOne(a, b, c)", 3)!;
        TruthValue[] allTrue = [TruthValue.True, TruthValue.True, TruthValue.True];

        Decision parity = await parityRule.EvaluateAsync(allTrue, TestContext.Current.CancellationToken);
        Decision single = await exactlyOne.EvaluateAsync(allTrue, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.True, parity.Result);
        Assert.Equal(TruthValue.False, single.Result);
    }

    /// <summary>A known True next to an Unknown does not decide parity the way it decides ExactlyOne's "two Trues" case.</summary>
    [Fact]
    public async Task Evaluate_ParityWithTwoTrueAndAnUnknown_IsUnknownWhereExactlyOneIsFalse_Test()
    {
        K3Rule parityRule = K3Rule.TryCreate("PARITY(a, b, c)", 3)!;
        K3Rule exactlyOne = K3Rule.TryCreate("ExactlyOne(a, b, c)", 3)!;
        TruthValue[] values = [TruthValue.True, TruthValue.True, TruthValue.Unknown];

        Decision parity = await parityRule.EvaluateAsync(values, TestContext.Current.CancellationToken);
        Decision single = await exactlyOne.EvaluateAsync(values, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.Unknown, parity.Result);
        Assert.Equal(TruthValue.False, single.Result);
    }

    /// <summary>Every spelling prints as the canonical function-call form.</summary>
    [Theory]
    [InlineData("PARITY(a, b, c)")]
    [InlineData("parity(a,b,c)")]
    [InlineData("Parity( a , b , c )")]
    public void Compile_AnyParitySpelling_ProducesTheSameCanonicalText_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal("PARITY(a, b, c)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A function-call form has no precedence, so it combines with infix operators without parentheses.</summary>
    [Theory]
    [InlineData("PARITY(a, b) AND c")]
    [InlineData("a OR PARITY(b, c)")]
    [InlineData("PARITY(a, b) XOR c")]
    [InlineData("NOT PARITY(a, b, c)")]
    [InlineData("PARITY(a AND b, c XOR a, PARITY(a, b))")]
    public void Compile_ParityNextToOtherOperators_NeedsNoParentheses_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>The operator name is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("parity")]
    [InlineData("PARITY")]
    public void IsReservedWord_Parity_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>PARITY needs at least two operands, like AND, OR and ExactlyOne.</summary>
    [Theory]
    [InlineData("PARITY(a)")]
    [InlineData("PARITY()")]
    public void Compile_ParityWithFewerThanTwoOperands_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>Binary XOR with three operands is still the arity error, pointing at PARITY.</summary>
    [Fact]
    public void Compile_XorChainOfThree_ReportsArityViolationNamingParity_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a XOR b XOR c");

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Contains("PARITY", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>The hint also names ExactlyOne so an author who wanted "exactly one" is not misled into parity.</summary>
    [Fact]
    public void Compile_XorChainOfThree_StillMentionsExactlyOne_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a XOR b XOR c");

        Assert.Contains("ExactlyOne", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }

    /// <summary>A JSON XOR with three operands gets the same hint.</summary>
    [Fact]
    public void CompileJson_XorWithThreeOperands_ReportsArityViolationNamingParity_Test()
    {
        const string Json = """{"op": "xor", "operands": [{"predicate": "a"}, {"predicate": "b"}, {"predicate": "c"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(Json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Contains("PARITY", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>JSON uses the <c>parity</c> op, round-trips, and reads the op name in any letter case.</summary>
    [Fact]
    public void PrintJson_Parity_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("PARITY(a, b, c)").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace("parity", "PARITY", StringComparison.Ordinal))
            .CompiledRule!;

        Assert.Contains(
            "\"op\":\"parity\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Fact]
    public void PrintYaml_Parity_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("PARITY(a, b, c)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: parity", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The builder produces the same rule as the DSL.</summary>
    [Fact]
    public void Parity_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        CompiledRule<RuleTestContext> rule = RuleBuilder
            .Parity(RuleBuilder.Predicate("a"), RuleBuilder.Not(RuleBuilder.Predicate("b")), RuleBuilder.Predicate("c"))
            .Compile(Compiler)
            .CompiledRule!;

        Assert.Equal("PARITY(a, NOT b, c)", rule.CanonicalText);
    }

    /// <summary>The trace tree labels the node PARITY and keeps operand order.</summary>
    [Fact]
    public async Task EvaluateAsync_Parity_LabelsTheNodeAndKeepsOperandOrder_Test()
    {
        K3Rule rule = K3Rule.TryCreate("PARITY(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal("PARITY", decision.TraceTree!.Text);
        Assert.Equal(["a", "b", "c"], decision.TraceTree.Children.Select(c => c.Text));
    }

    /// <summary>The operator description explains parity and the Unknown rule.</summary>
    [Fact]
    public void Outline_Parity_ExplainsParityAndUnknown_Test()
    {
        OutlineNode description = Compiler.Compile("PARITY(a, b, c)").CompiledRule!.Outline();

        Assert.Equal("PARITY", description.Label);
        Assert.Contains("odd number", description.Description, StringComparison.Ordinal);
        Assert.Contains("Unknown", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b", "c"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Tree renderings keep the function-call word in every operator style (PARITY has no symbol).</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_Parity_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        OutlineNode tree = Compiler.Compile("PARITY(a, b, c)").CompiledRule!.Outline();

        Assert.Contains("PARITY", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("PARITY", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }

    /// <summary>
    /// The retired <c>NXOR</c> spelling is rejected in any letter case and wherever it sits, with a "did you mean PARITY"
    /// replacement, and the diagnostic spans the whole call.
    /// </summary>
    [Theory]
    [InlineData("NXOR(a, b, c)", "NXOR(a, b, c)")]
    [InlineData("nxor(a,b)", "nxor(a,b)")]
    [InlineData("a AND Nxor(b, c)", "Nxor(b, c)")]
    [InlineData("NOT NXOR(a, b)", "NXOR(a, b)")]
    public void Compile_DeclaredNxor_IsRejectedWithADidYouMeanParity_Test(string text, string call)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.SyntaxError, error.Code);
        Assert.Equal(DiagnosticSuggestionKind.Replacement, error.Suggestion?.Kind);
        Assert.Equal("PARITY", error.Suggestion?.Text);
        Assert.Equal(call, text.Substring(error.Span.Start, error.Span.Length));
    }

    /// <summary><c>NXOR</c> stays reserved so a predicate cannot shadow the rejected word.</summary>
    [Fact]
    public void IsReservedWord_Nxor_IsStillReserved_Test()
    {
        Assert.True(DslParser.IsReservedWord("NXOR"));
    }

    /// <summary>A JSON <c>nxor</c> op, in any letter case, is rejected with a "did you mean parity" replacement.</summary>
    [Theory]
    [InlineData("nxor")]
    [InlineData("NXOR")]
    public void CompileJson_DeclaredNxor_IsRejectedWithADidYouMeanParity_Test(string op)
    {
        string json = $$"""{"op": "{{op}}", "operands": [{"predicate": "a"}, {"predicate": "b"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, error.Code);
        Assert.Equal(DiagnosticSuggestionKind.Replacement, error.Suggestion?.Kind);
        Assert.Equal("parity", error.Suggestion?.Text);
    }

    /// <summary>A YAML <c>nxor</c> op is rejected the same way, at the node's path.</summary>
    [Fact]
    public void CompileYaml_DeclaredNxor_IsRejectedWithADidYouMeanParity_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(
            "op: not\noperands:\n- op: nxor\n  operands:\n  - predicate: a\n  - predicate: b\n"
        );

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, error.Code);
        Assert.Equal("parity", error.Suggestion?.Text);
        Assert.NotNull(error.Path);
    }

    /// <summary>The XOR arity hint names PARITY and no longer mentions the retired spelling.</summary>
    [Fact]
    public void Compile_XorChainOfThree_HintNamesParityNotNxor_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a XOR b XOR c");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("PARITY", diagnostic.Suggestion?.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("NXOR", diagnostic.Message + diagnostic.Suggestion?.Text, StringComparison.Ordinal);
    }
}
