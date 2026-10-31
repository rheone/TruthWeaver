namespace TruthWeaver.Tests;

using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Every entry of <see cref="ReservedWords"/> is rejected as a definition parameter name in all four formats. A new
/// reserved word is covered without a new test, so the check cannot lag behind the list.
/// </summary>
public sealed class ReservedWordParameterTests
{
    /// <summary>Gets every reserved word, one per theory row.</summary>
    public static TheoryData<string> Words => [with(ReservedWords.All)];

    /// <summary>A reserved word as a rule text definition parameter is a name collision.</summary>
    [Theory]
    [MemberData(nameof(Words))]
    public void Compile_ReservedWordAsParameter_ReportsDefinitionNameCollision_Test(string word)
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile($"let gate({word}) = a AND b; gate(a)");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A reserved word as a JSON definition parameter is a name collision.</summary>
    [Theory]
    [MemberData(nameof(Words))]
    public void CompileJson_ReservedWordAsParameter_ReportsDefinitionNameCollision_Test(string word)
    {
        string json = $$"""
            {
              "definitions": {
                "gate": {
                  "parameters": ["{{word}}"],
                  "body": { "op": "and", "operands": [{ "predicate": "a" }, { "predicate": "b" }] }
                }
              },
              "op": "use",
              "name": "gate",
              "operands": [{ "predicate": "a" }]
            }
            """;

        CompilationResult<RuleTestContext> result = CreateCompiler().CompileJson(json);

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A reserved word as a YAML definition parameter is a name collision.</summary>
    [Theory]
    [MemberData(nameof(Words))]
    public void CompileYaml_ReservedWordAsParameter_ReportsDefinitionNameCollision_Test(string word)
    {
        string yaml = $$"""
            definitions:
              gate:
                parameters: ["{{word}}"]
                body:
                  op: and
                  operands:
                    - predicate: a
                    - predicate: b
            op: use
            name: gate
            operands:
              - predicate: a
            """;

        CompilationResult<RuleTestContext> result = CreateCompiler().CompileYaml(yaml);

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A reserved word as a <see cref="RuleBuilder"/> definition parameter is a name collision.</summary>
    [Theory]
    [MemberData(nameof(Words))]
    public void Compile_BuilderReservedWordAsParameter_ReportsDefinitionNameCollision_Test(string word)
    {
        RuleDefinition gate = RuleBuilder.Define(
            "gate",
            [word],
            RuleBuilder.And(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
        );
        RuleBuilder builder = RuleBuilder.Use("gate", RuleBuilder.Predicate("a")).WithDefinitions(gate);

        CompilationResult<RuleTestContext> result = builder.Compile(CreateCompiler());

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Null(result.CompiledRule);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096)
        );
    }
}
