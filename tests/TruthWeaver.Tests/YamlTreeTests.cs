namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;
using YamlDotNet.RepresentationModel;

/// <summary>Ticket 08: YAML tree parse/print.</summary>
/// <remarks>
/// Ticket 29: <c>YamlTreeParser.ParseOperator</c>'s unhandled-canonical-op-name <c>default</c> arm
/// (thrown as an <see cref="InvalidOperationException"/>) is left undocumented-by-test rather than
/// exercised directly, unlike ticket 15's <c>ThresholdDescription</c> precedent. That switch takes its
/// discriminant as a method parameter that a test can hand-build with a bogus value; this one instead
/// switches on the out-value of <c>TreeFormatOpNames.TryFromTreeFormat</c>, an internal lookup whose
/// backing dictionary has exactly the same ten entries as the switch's non-default cases (see
/// <c>TreeFormatOpNames.CanonicalToTreeFormat</c>). There is no parameter or public seam through which
/// a test can make that lookup yield an eleventh, unhandled canonical name, so the branch is genuinely
/// unreachable rather than merely untested - it exists defensively against the closed set (ADR-0003)
/// ever growing without updating this switch in lockstep.
/// </remarks>
public sealed class YamlTreeTests
{
    private const string WorkedExampleYaml = """
        op: and
        operands:
          - predicate: hasRole
            args:
              role: "Y"
          - op: or
            operands:
              - predicate: hasTraining
                args:
                  training: "Q"
              - predicate: hasTraining
                args:
                  training: "Z"
              - op: xor
                operands:
                  - predicate: isManager
                  - predicate: isDepartmentHead
        """;

    private const string WorkedExampleDsl =
        "hasRole(role: \"Y\") AND (hasTraining(training: \"Q\") OR hasTraining(training: \"Z\") OR (isManager XOR isDepartmentHead))";

    /// <summary>The YAML worked example parses to a structure equal to the DSL and JSON forms.</summary>
    [Fact]
    public void Parse_YamlWorkedExample_EqualsDslAndJsonForms_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> fromYaml = compiler.CompileYaml(WorkedExampleYaml).CompiledRule!;
        CompiledRule<RuleTestContext> fromDsl = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        Assert.Equal(fromDsl.CanonicalText, fromYaml.CanonicalText);
    }

    /// <summary>A compiled rule prints to YAML and reparses to a structurally equal tree.</summary>
    [Fact]
    public void Print_CompiledRule_ReparsesToEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> original = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The threshold family round trips through YAML.</summary>
    [Theory]
    [InlineData("AtLeast", "atLeast")]
    [InlineData("AtMost", "atMost")]
    [InlineData("GreaterThan", "greaterThan")]
    [InlineData("LessThan", "lessThan")]
    [InlineData("Exactly", "exactly")]
    public void RoundTrip_ThresholdFamily_ThroughYaml_Test(string dslKeyword, string yamlOpName)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile($"{dslKeyword}(2, a, b, c)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains($"op: {yamlOpName}", yaml, StringComparison.Ordinal);
    }

    /// <summary>A constant false node round trips through YAML.</summary>
    [Fact]
    public void RoundTrip_ConstantFalse_ThroughYaml_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> original = compiler.Compile("false").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains("const: false", yaml, StringComparison.Ordinal);
    }

    /// <summary>XNOR round trips through YAML.</summary>
    [Fact]
    public void RoundTrip_Xnor_ThroughYaml_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("a XNOR b").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains("op: equivalent", yaml, StringComparison.Ordinal);
    }

    /// <summary>A NOT node wraps its single operand and round trips through YAML.</summary>
    [Fact]
    public void RoundTrip_Not_WrapsSingleOperand_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("NOT a").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains("op: not", yaml, StringComparison.Ordinal);
    }

    /// <summary>An ExactlyOne node wraps its operands and round trips through YAML.</summary>
    [Fact]
    public void RoundTrip_ExactlyOne_WrapsOperands_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("ExactlyOne(a, b, c)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains("op: exactlyOne", yaml, StringComparison.Ordinal);
    }

    /// <summary>A malformed nested operand short circuits operator parsing, without a parent diagnostic.</summary>
    [Fact]
    public void NestedOperand_Malformed_ShortCircuitsWithoutParentDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(
            """
            op: and
            operands:
              - predicate: isManager
              - noRecognizedKey: true
            """
        );

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Contains("must have a 'const', 'predicate', or 'op' key", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A malformed element in an array literal fails the whole literal rather than truncating it.</summary>
    [Fact]
    public void ArrayLiteral_MalformedElement_FailsWholeLiteral_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(
            """
            predicate: hasRole
            args:
              role:
                - valid
                - weird: 1
            """
        );

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Contains("Unsupported YAML node type", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A string argument that reads like a boolean stays a string when quoted.</summary>
    [Fact]
    public void Literal_QuotedBooleanLikeString_StaysString_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasCode", "code", "true").Build()
        );

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(
            """
            predicate: hasCode
            args:
              code: "true"
            """
        );

        Assert.True(result.Succeeded);
    }

    /// <summary>Malformed YAML produces a diagnostic, not an exception.</summary>
    [Theory]
    [InlineData("not: [valid, yaml: [")]
    [InlineData("op: bogus\noperands: []")]
    public void Parse_MalformedYaml_ProducesDiagnosticNotException_Test(string yaml)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(yaml);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>Every distinct malformed tree branch raises its specific message.</summary>
    [Theory]
    [InlineData("not: [valid, yaml: [", "Malformed YAML:")]
    [InlineData("", "The YAML document is empty.")]
    [InlineData("- just\n- a\n- sequence", "Expected a YAML mapping node but found")]
    [InlineData("const: notabool", "'const' must be a YAML boolean or one of")]
    [InlineData("predicate: [not, a, string]", "'predicate' must be a YAML string.")]
    [InlineData("predicate: isManager\nargs: [not, a, mapping]", "'args' must be a YAML mapping.")]
    [InlineData("predicate: isManager\nargs:\n  ? [not, a, scalar]\n  : true", "An argument name must be a YAML string.")]
    [InlineData("op: and", "requires an 'operands' sequence")]
    [InlineData("op: not\noperands:\n  - const: true\n  - const: false", "'not' requires exactly one operand.")]
    [InlineData("op: bogus\noperands: []", "Unknown operator 'bogus'.")]
    [InlineData("op: atLeast\noperands:\n  - const: true", "requires a numeric 'k'")]
    [InlineData("predicate: isManager\nargs:\n  x:\n    weird: 1", "A variable reference has only")]
    public void Malformed_EveryBranch_RaisesSpecificMessage_Test(string yaml, string expectedMessageSubstring)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(yaml);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Severity == DiagnosticSeverity.Error && d.Message.Contains(expectedMessageSubstring, StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// YAML text that holds more than one document is a malformed tree located at the root node of the second document,
    /// so no document after the first is silently dropped.
    /// </summary>
    [Theory]
    [InlineData("const: true\n---\nconst: false\n")]
    [InlineData("---\npredicate: isManager\n---\npredicate: isDepartmentHead\n---\nconst: true\n")]
    public void CompileYaml_MoreThanOneDocument_ReportsMalformedTree_Test(string yaml)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(yaml);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$", diagnostic.Path);
        Assert.Equal(yaml.IndexOf("---\n", 1, StringComparison.Ordinal) + 4, diagnostic.Span.Start);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>One document with an explicit <c>---</c> start and <c>...</c> end marker still compiles.</summary>
    [Fact]
    public void CompileYaml_OneDocumentWithMarkers_Compiles_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileYaml("---\nconst: true\n...\n");

        Assert.True(result.Succeeded);
    }

    /// <summary>Compiling a YAML node subtree is structurally equal to compiling the same tree as standalone text.</summary>
    [Fact]
    public void Compile_YamlSubtree_EqualsStandaloneText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        YamlMappingNode document = LoadYamlMappingRoot(
            $"""
            metadata:
              name: example
              version: 1
            rule:
            {Indent(WorkedExampleYaml)}
            """
        );
        YamlNode ruleNode = document.Children[new YamlScalarNode("rule")];

        CompiledRule<RuleTestContext> fromNode = compiler.CompileYaml(ruleNode).CompiledRule!;
        CompiledRule<RuleTestContext> fromText = compiler.CompileYaml(WorkedExampleYaml).CompiledRule!;

        Assert.Equal(fromText.CanonicalText, fromNode.CanonicalText);
    }

    /// <summary>Two sibling rule expressions in one YAML document compile independently.</summary>
    [Fact]
    public void Compile_SiblingRulesInYamlDocument_AreIndependent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        YamlMappingNode document = LoadYamlMappingRoot(
            """
            first:
              predicate: isManager
            second:
              op: bogus
              operands: []
            """
        );
        YamlNode firstNode = document.Children[new YamlScalarNode("first")];
        YamlNode secondNode = document.Children[new YamlScalarNode("second")];

        CompilationResult<RuleTestContext> firstResult = compiler.CompileYaml(firstNode);
        CompilationResult<RuleTestContext> secondResult = compiler.CompileYaml(secondNode);

        Assert.True(firstResult.Succeeded);
        Assert.Empty(firstResult.Diagnostics);
        Assert.False(secondResult.Succeeded);
        Assert.Contains(
            secondResult.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.UnknownPredicate
                && d.Message.Contains("Unknown operator 'bogus'.", StringComparison.Ordinal)
        );
    }

    /// <summary>A malformed YAML node produces the same diagnostic as the equivalent standalone YAML text.</summary>
    [Fact]
    public void Parse_MalformedYamlNode_MatchesStandaloneDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string malformedYaml = "nothingRecognized: true";
        YamlNode node = LoadYamlRoot(malformedYaml);

        CompilationResult<RuleTestContext> fromNode = compiler.CompileYaml(node);
        CompilationResult<RuleTestContext> fromText = compiler.CompileYaml(malformedYaml);

        Assert.False(fromNode.Succeeded);
        Assert.Equal(
            fromText.Diagnostics.Select(d => (d.Code, d.Message)),
            fromNode.Diagnostics.Select(d => (d.Code, d.Message))
        );
    }

    private static YamlNode LoadYamlRoot(string yaml)
    {
        YamlStream stream = [];
        using StringReader reader = new(yaml);
        stream.Load(reader);
        return stream.Documents[0].RootNode;
    }

    private static YamlMappingNode LoadYamlMappingRoot(string yaml)
    {
        return (YamlMappingNode)LoadYamlRoot(yaml);
    }

    private static string Indent(string yaml)
    {
        return string.Join('\n', yaml.Split('\n').Select(line => line.Length == 0 ? line : "  " + line));
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddStringArgPredicate("hasTraining", "training", "Q")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
