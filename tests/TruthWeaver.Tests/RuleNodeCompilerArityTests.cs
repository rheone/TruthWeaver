namespace TruthWeaver.Tests;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>RuleNodeCompiler</c> takes every operand count from the operator table (<see cref="OperatorDefinitions"/>), so
/// the table's <c>MinOperands</c> and <c>MaxOperands</c> alone decide which counts compile.
/// </summary>
public sealed class RuleNodeCompilerArityTests
{
    /// <summary>Gets every operator in the table so a new operator is checked without editing this test.</summary>
    public static TheoryData<string> OpNames => [.. OperatorDefinitions.All.Select(d => d.OpName)];

    /// <summary>One operand fewer than the table's minimum is rejected with an arity diagnostic and no exception.</summary>
    [Theory]
    [MemberData(nameof(OpNames))]
    public void CompileJson_OneOperandBelowTheMinimum_ProducesAnArityDiagnostic_Test(string opName)
    {
        OperatorDefinitions.TryGet(opName, out OperatorDefinition? definition);

        CompilationResult<RuleTestContext> result = Compile(definition!, definition!.MinOperands - 1);

        AssertArityRejected(result);
    }

    /// <summary>One operand more than the table's maximum is rejected with an arity diagnostic and no exception.</summary>
    [Theory]
    [MemberData(nameof(OpNames))]
    public void CompileJson_OneOperandAboveTheMaximum_ProducesAnArityDiagnostic_Test(string opName)
    {
        OperatorDefinitions.TryGet(opName, out OperatorDefinition? definition);
        if (definition!.MaxOperands is not { } max)
        {
            return;
        }

        CompilationResult<RuleTestContext> result = Compile(definition, max + 1);

        AssertArityRejected(result);
    }

    private static CompilationResult<RuleTestContext> Compile(OperatorDefinition definition, int operandCount)
    {
        string operands = string.Join(", ", Enumerable.Repeat("""{"const": true}""", operandCount));
        string bounds = definition.OpName switch
        {
            "AtLeast" or "AtMost" or "GreaterThan" or "LessThan" or "Exactly" => """, "k": 1""",
            "Between" => """, "min": 1, "max": 1""",
            _ => string.Empty,
        };
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        return compiler.CompileJson($$"""{"op": "{{definition.TreeFormatName}}"{{bounds}}, "operands": [{{operands}}]}""");
    }

    private static void AssertArityRejected(CompilationResult<RuleTestContext> result)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Severity == DiagnosticSeverity.Error
                && (d.Code == DiagnosticCodes.MalformedTree || d.Code == DiagnosticCodes.InfixArityViolation)
                && d.Expected?.Contains("operand", StringComparison.Ordinal) == true
        );
    }
}
