namespace TruthWeaver.Tests;

using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.DocExamples;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// One authoring mistake gets one diagnostic code on every surface that can express it (rule text, JSON, YAML and
/// <see cref="RuleBuilder"/>), so a tool that matches on a code never depends on how the rule was written.
/// </summary>
public sealed class DiagnosticCodeConsistencyTests
{
    /// <summary>An operator given an operand count it does not accept is <c>TRE0006</c>, whatever the operator and the surface.</summary>
    [Theory]
    [InlineData("IF(isManager, isManager)")]
    [InlineData("IsTrue(isManager, isManager)")]
    [InlineData("PARITY(isManager)")]
    [InlineData("ExactlyOne(isManager)")]
    [InlineData("COALESCE(isManager)")]
    [InlineData("ANY(isManager)")]
    [InlineData("AtLeast(1)")]
    [InlineData("BETWEEN(0, 1, isManager)")]
    [InlineData("isManager XOR isManager XOR isManager")]
    public void Compile_OperandCountMistakeInRuleText_ReportsInfixArityViolation_Test(string text)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile(text);

        Assert.Equal(DiagnosticCodes.InfixArityViolation, Assert.Single(Errors(result)).Code);
    }

    /// <summary>The same operand-count mistakes in a JSON tree get the same code as in rule text.</summary>
    [Theory]
    [InlineData("""{"op":"if","operands":[{"predicate":"isManager"},{"predicate":"isManager"}]}""")]
    [InlineData("""{"op":"isTrue","operands":[{"predicate":"isManager"},{"predicate":"isManager"}]}""")]
    [InlineData("""{"op":"parity","operands":[{"predicate":"isManager"}]}""")]
    [InlineData("""{"op":"not","operands":[{"predicate":"isManager"},{"predicate":"isManager"}]}""")]
    [InlineData("""{"op":"atLeast","k":1,"operands":[]}""")]
    [InlineData("""{"op":"between","min":0,"max":1,"operands":[{"predicate":"isManager"}]}""")]
    [InlineData("""{"op":"xor","operands":[{"predicate":"isManager"},{"predicate":"isManager"},{"predicate":"isManager"}]}""")]
    public void CompileJson_OperandCountMistake_ReportsInfixArityViolation_Test(string json)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileJson(json);

        Assert.Equal(DiagnosticCodes.InfixArityViolation, Assert.Single(Errors(result)).Code);
    }

    /// <summary>The same operand-count mistakes in a YAML tree get the same code as in rule text.</summary>
    [Theory]
    [InlineData("op: if\noperands:\n  - predicate: isManager\n  - predicate: isManager\n")]
    [InlineData("op: not\noperands:\n  - predicate: isManager\n  - predicate: isManager\n")]
    [InlineData("op: parity\noperands:\n  - predicate: isManager\n")]
    public void CompileYaml_OperandCountMistake_ReportsInfixArityViolation_Test(string yaml)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileYaml(yaml);

        Assert.Equal(DiagnosticCodes.InfixArityViolation, Assert.Single(Errors(result)).Code);
    }

    /// <summary>A builder operator with too few operands gets the same code as the other surfaces.</summary>
    [Fact]
    public void Compile_BuilderOperandCountMistake_ReportsInfixArityViolation_Test()
    {
        CompilationResult<RuleTestContext> result = RuleBuilder
            .Parity(RuleBuilder.Predicate("isManager"))
            .Compile(CreateCompiler());

        Assert.Equal(DiagnosticCodes.InfixArityViolation, Assert.Single(Errors(result)).Code);
    }

    /// <summary><c>Collapse</c>, <c>Project</c> and <c>NXOR</c> are not operators, so rule text reports them as an unknown operator.</summary>
    [Theory]
    [InlineData("Collapse(isManager)")]
    [InlineData("Project(isManager)")]
    [InlineData("NXOR(isManager, isManager)")]
    public void Compile_RetiredOperatorInRuleText_ReportsUnknownPredicate_Test(string text)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile(text);

        Assert.Equal(DiagnosticCodes.UnknownPredicate, Assert.Single(Errors(result)).Code);
    }

    /// <summary>A JSON tree that names a retired operator gets the same code as rule text.</summary>
    [Theory]
    [InlineData("collapse")]
    [InlineData("project")]
    [InlineData("nxor")]
    public void CompileJson_RetiredOperator_ReportsUnknownPredicate_Test(string op)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileJson($$"""{"op":"{{op}}","operands":[{"predicate":"isManager"}]}""");

        Assert.Equal(DiagnosticCodes.UnknownPredicate, Assert.Single(Errors(result)).Code);
    }

    /// <summary>A YAML tree that names a retired operator gets the same code as rule text.</summary>
    [Theory]
    [InlineData("collapse")]
    [InlineData("project")]
    [InlineData("nxor")]
    public void CompileYaml_RetiredOperator_ReportsUnknownPredicate_Test(string op)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler()
            .CompileYaml($"op: {op}\noperands:\n  - predicate: isManager\n");

        Assert.Equal(DiagnosticCodes.UnknownPredicate, Assert.Single(Errors(result)).Code);
    }

    /// <summary>An operator or predicate name that nothing defines is <c>TRE0002</c> on every surface.</summary>
    [Fact]
    public void Compile_UnknownNameOnEverySurface_ReportsUnknownPredicate_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext>[] results =
        [
            compiler.Compile("bogus()"),
            compiler.CompileJson("""{"op":"bogus","operands":[{"predicate":"isManager"}]}"""),
            compiler.CompileYaml("op: bogus\noperands:\n  - predicate: isManager\n"),
            RuleBuilder.Predicate("bogus").Compile(compiler),
        ];

        Assert.All(results, r => Assert.Equal(DiagnosticCodes.UnknownPredicate, Assert.Single(Errors(r)).Code));
    }

    /// <summary>A threshold <c>k</c> or <c>BETWEEN</c> bound that is out of range is <c>TRE0008</c> on every surface.</summary>
    [Fact]
    public void Compile_ThresholdOutOfRangeOnEverySurface_ReportsInvalidThresholdValue_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext>[] results =
        [
            compiler.Compile("AtLeast(5, isManager, isManager)"),
            compiler.CompileJson("""{"op":"atLeast","k":5,"operands":[{"predicate":"isManager"},{"predicate":"isManager"}]}"""),
            compiler.CompileYaml("op: atLeast\nk: 5\noperands:\n  - predicate: isManager\n  - predicate: isManager\n"),
            RuleBuilder.AtLeast(5, RuleBuilder.Predicate("isManager"), RuleBuilder.Predicate("isManager")).Compile(compiler),
        ];

        Assert.All(results, r => Assert.Equal(DiagnosticCodes.InvalidThresholdValue, Assert.Single(Errors(r)).Code));
    }

    /// <summary>A threshold <c>k</c> or <c>BETWEEN</c> bound that is not a whole number gets the same code as one out of range.</summary>
    [Fact]
    public void Compile_ThresholdNotAWholeNumberOnEverySurface_ReportsInvalidThresholdValue_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext>[] results =
        [
            compiler.Compile("AtLeast(1.5, isManager, isManager)"),
            compiler.Compile("BETWEEN(0, 1.5, isManager, isManager)"),
            compiler.CompileJson(
                """{"op":"atLeast","k":1.5,"operands":[{"predicate":"isManager"},{"predicate":"isManager"}]}"""
            ),
            compiler.CompileJson(
                """{"op":"between","min":0,"operands":[{"predicate":"isManager"},{"predicate":"isManager"}]}"""
            ),
            compiler.CompileYaml("op: atLeast\nk: abc\noperands:\n  - predicate: isManager\n  - predicate: isManager\n"),
            compiler.CompileYaml("op: between\nmin: 0\noperands:\n  - predicate: isManager\n  - predicate: isManager\n"),
        ];

        Assert.All(results, r => Assert.Equal(DiagnosticCodes.InvalidThresholdValue, Assert.Single(Errors(r)).Code));
    }

    /// <summary>A predicate argument named twice is <c>TRE0032</c> on every surface, including YAML.</summary>
    [Fact]
    public void Compile_DuplicateArgumentOnEverySurface_ReportsDuplicateArgument_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext>[] results =
        [
            compiler.Compile("hasRole(role: \"Y\", role: \"Z\")"),
            compiler.CompileJson("""{"predicate":"hasRole","args":{"role":"Y","role":"Z"}}"""),
            compiler.CompileYaml("predicate: hasRole\nargs:\n  role: Y\n  role: Z\n"),
            RuleBuilder.Predicate("hasRole", ("role", "Y"), ("role", "Z")).Compile(compiler),
        ];

        Assert.All(results, r => Assert.Equal(DiagnosticCodes.DuplicateArgument, Assert.Single(Errors(r)).Code));
    }

    /// <summary>
    /// Every code in <see cref="DiagnosticCodes"/> is listed in the code table of <c>docs/strong-k3/specification/diagnostics.md</c>,
    /// so a new code cannot ship undocumented.
    /// </summary>
    [Fact]
    public void DiagnosticCodes_EveryCode_IsInTheCodeTable_Test()
    {
        string table = File.ReadAllText(RepositoryFile("docs", "strong-k3", "specification", "diagnostics.md"));
        string[] codes =
        [
            .. typeof(DiagnosticCodes)
                .GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!),
        ];

        Assert.NotEmpty(codes);
        Assert.All(codes, code => Assert.Contains($"| `{code}` |", table, StringComparison.Ordinal));
    }

    /// <summary>The code table gives each code its name, severity, phase and whether it is on by default.</summary>
    [Fact]
    public void DiagnosticCodes_EveryConstantName_IsInTheCodeTableNextToItsCode_Test()
    {
        string table = File.ReadAllText(RepositoryFile("docs", "strong-k3", "specification", "diagnostics.md"));

        foreach (System.Reflection.FieldInfo field in typeof(DiagnosticCodes).GetFields().Where(f => f.IsLiteral))
        {
            string code = (string)field.GetRawConstantValue()!;
            Assert.Contains($"| `{code}` | `{field.Name}` |", table, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<Diagnostic> Errors(CompilationResult<RuleTestContext> result)
    {
        return result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    }

    private static string RepositoryFile(params string[] segments)
    {
        return Path.Combine([DocExampleChecker.FindRepositoryRoot(), .. segments]);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .Build()
        );
    }
}
