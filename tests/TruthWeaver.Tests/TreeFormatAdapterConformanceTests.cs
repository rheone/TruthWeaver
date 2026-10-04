namespace TruthWeaver.Tests;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// One conformance suite for the two tree-format adapters (JSON and YAML) over the shared
/// <see cref="TruthWeaver.Parsing.TreeFormatReader"/>: the same rule written in each format must compile to the same
/// canonical text, and the same mistake must be reported with the same code at the same path. A change to how an
/// operator is read is therefore made once and checked for both formats.
/// </summary>
public sealed class TreeFormatAdapterConformanceTests
{
    /// <summary>Gets every tree-format operator name, so adding an operator to the closed set adds a conformance case.</summary>
    public static TheoryData<string> AllOperators => [.. TreeFormatOpNames.ReadableNames];

    /// <summary>Gets one malformed document per reader rule, written once in each format.</summary>
    public static TheoryData<string, string> MalformedDocuments =>
        new()
        {
            { "[]", "[]" },
            { "{}", "{}" },
            { """{"op": {}}""", "{op: {a: 1}}" },
            { """{"predicate": []}""", "{predicate: [a]}" },
            { """{"op": "adn", "operands": []}""", "{op: adn, operands: []}" },
            { """{"op": "collapse", "operands": []}""", "{op: collapse, operands: []}" },
            { """{"op": "nxor", "operands": []}""", "{op: nxor, operands: []}" },
            { """{"op": "project", "operands": []}""", "{op: project, operands: []}" },
            { """{"op": "and"}""", "{op: and}" },
            { """{"op": "and", "operands": 3}""", "{op: and, operands: 3}" },
            { """{"op": "and", "operands": [{}]}""", "{op: and, operands: [{}]}" },
            {
                """{"op": "not", "operands": [{"const": true}, {"const": true}]}""",
                "{op: not, operands: [{const: true}, {const: true}]}"
            },
            { """{"op": "atleast", "operands": [{"const": true}]}""", "{op: atleast, operands: [{const: true}]}" },
            {
                """{"op": "atleast", "k": "x", "operands": [{"const": true}]}""",
                "{op: atleast, k: x, operands: [{const: true}]}"
            },
            {
                """{"op": "between", "min": 0, "operands": [{"const": true}]}""",
                "{op: between, min: 0, operands: [{const: true}]}"
            },
            { """{"const": "maybe"}""", "{const: maybe}" },
            { """{"predicate": "isManager", "args": 3}""", "{predicate: isManager, args: 3}" },
            { """{"predicate": "hasRole", "args": {"role": {"a": 1}}}""", "{predicate: hasRole, args: {role: {a: 1}}}" },
        };

    /// <summary>
    /// Verifies that an operator written in JSON and in YAML is read identically: the same outcome, the same canonical
    /// text, and the same diagnostic codes at the same paths (for example a mis-sized operand list).
    /// </summary>
    /// <param name="treeFormatName">The operator's tree-format name.</param>
    [Theory]
    [MemberData(nameof(AllOperators))]
    public void Compile_SameOperatorInJsonAndYaml_ReadsIdentically_Test(string treeFormatName)
    {
        // Arrange
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        string json =
            $$"""{"op": "{{treeFormatName}}", "k": 1, "min": 0, "max": 2, "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""";
        string yaml = $"""
            op: {treeFormatName}
            k: 1
            min: 0
            max: 2
            operands:
              - predicate: isManager
              - predicate: isDepartmentHead
            """;

        // Act
        CompilationResult<RuleTestContext> fromJson = compiler.CompileJson(json);
        CompilationResult<RuleTestContext> fromYaml = compiler.CompileYaml(yaml);

        // Assert
        Assert.Equal(fromJson.Succeeded, fromYaml.Succeeded);
        Assert.Equal(fromJson.CompiledRule?.CanonicalText, fromYaml.CompiledRule?.CanonicalText);
        Assert.Equal(Located(fromJson), Located(fromYaml));
    }

    /// <summary>
    /// Verifies that the same mistake in JSON and in YAML is reported with the same codes at the same paths, so the shared
    /// reader's validation cannot drift between the formats.
    /// </summary>
    /// <param name="json">The malformed document in JSON.</param>
    /// <param name="yaml">The same document in YAML.</param>
    [Theory]
    [MemberData(nameof(MalformedDocuments))]
    public void Compile_SameMistakeInJsonAndYaml_ReportsTheSameCodeAtTheSamePath_Test(string json, string yaml)
    {
        // Arrange
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        // Act
        CompilationResult<RuleTestContext> fromJson = compiler.CompileJson(json);
        CompilationResult<RuleTestContext> fromYaml = compiler.CompileYaml(yaml);

        // Assert
        Assert.False(fromJson.Succeeded);
        Assert.False(fromYaml.Succeeded);
        Assert.Equal(Located(fromJson), Located(fromYaml));
    }

    private static (string Code, string? Path)[] Located(CompilationResult<RuleTestContext> result)
    {
        return [.. result.Diagnostics.Select(d => (d.Code, d.Path))];
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
