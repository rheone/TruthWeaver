namespace TruthWeaver.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// <see cref="RuleDefinitions"/> and <see cref="YamlRuleDefinitions"/>: reading the <c>let</c> definitions and uses of a
/// document without expanding them, mapping the nodes of the compiled rule back to them, and printing the document with
/// its definitions.
/// </summary>
public sealed class RuleDefinitionsTests
{
    private const string Text = "let both(x, y) = x AND y;\nlet one = both(a, c);\none OR both(b, a)";

    /// <summary>A definition is listed with its name, its parameters, its tree path and the span of its name.</summary>
    [Fact]
    public void ReadText_DefinitionWithParameters_ListsNameParametersPathAndSpan_Test()
    {
        RuleDefinitions read = RuleDefinitions.ReadText(Text);

        Assert.True(read.Succeeded);
        Assert.True(read.HasDefinitions);
        DefinitionDeclaration both = read.Definitions[0];
        Assert.Equal("both", both.Name);
        Assert.Equal(["x", "y"], both.Parameters);
        Assert.Equal("$.definitions.both", both.Path);
        Assert.Equal(Text.IndexOf("both", StringComparison.Ordinal), both.NameSpan.Start);
        Assert.Equal(0, both.Span.Start);
        Assert.Equal(["both", "one"], read.Definitions.Select(d => d.Name));
    }

    /// <summary>
    /// Each use is listed in source order with its argument count, the definition that holds it, its tree path and the
    /// paths of its expansions in the compiled rule.
    /// </summary>
    [Fact]
    public void ReadText_UsesInBodiesAndRule_ListsArgumentsOwnerAndExpandedPaths_Test()
    {
        RuleDefinitions read = RuleDefinitions.ReadText(Text);

        Assert.Equal(3, read.Uses.Count);
        DefinitionUse inBody = read.Uses[0];
        Assert.Equal(
            ("both", 2, "one", "$.definitions.one.body"),
            (inBody.Name, inBody.ArgumentCount, inBody.Definition, inBody.Path)
        );
        Assert.Equal(["$.operands[0]"], inBody.ExpandedPaths);
        DefinitionUse one = read.Uses[1];
        Assert.Equal(("one", 0, (string?)null, "$.operands[0]"), (one.Name, one.ArgumentCount, one.Definition, one.Path));
        DefinitionUse second = read.Uses[2];
        Assert.Equal("$.operands[1]", second.Path);
        Assert.Equal(["$.operands[1]"], second.ExpandedPaths);
        Assert.Equal(Text.LastIndexOf("both", StringComparison.Ordinal), second.NameSpan.Start);
    }

    /// <summary>
    /// The root of an expanded use comes from the use and then the definition body; a node in the place of a parameter
    /// comes from the argument of the use, not from the parameter.
    /// </summary>
    [Fact]
    public void SourcesOf_ExpandedUseAndParameterPlace_ListsUseBodyAndArgument_Test()
    {
        RuleDefinitions read = RuleDefinitions.ReadText(Text);

        Assert.Equal(["$.operands[1]", "$.definitions.both.body"], read.SourcesOf("$.operands[1]"));
        Assert.Equal(["$.operands[1].operands[0]"], read.SourcesOf("$.operands[1].operands[0]"));
        Assert.Equal(["$.operands[0]", "$.definitions.one.body", "$.definitions.both.body"], read.SourcesOf("$.operands[0]"));
        Assert.Equal(["$.definitions.one.body.operands[1]"], read.SourcesOf("$.operands[0].operands[1]"));
        Assert.Equal(["$"], read.SourcesOf("$"));
        Assert.Empty(read.SourcesOf("$.operands[9]"));
    }

    /// <summary>
    /// The printed tree keeps the definitions section and the use nodes, and compiles to the same rule as the source.
    /// </summary>
    [Fact]
    public void PrintJson_DocumentWithDefinitions_KeepsDefinitionsAndCompilesToTheSameRule_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        RuleDefinitions read = RuleDefinitions.ReadText(Text);

        string json = read.PrintJson()!;

        JsonObject tree = (JsonObject)JsonNode.Parse(json)!;
        Assert.Equal(["x", "y"], tree["definitions"]!["both"]!["parameters"]!.AsArray().Select(p => p!.ToString()));
        Assert.Equal("use", tree["operands"]![0]!["op"]!.ToString());
        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(json).Rule!;
        Assert.False(RuleDiff.Compare(compiler.Compile(Text).Rule!, fromJson).HasChanges);
    }

    /// <summary>Notes, threshold counts and term arguments survive the printed tree.</summary>
    [Fact]
    public void PrintJson_NotesCountsAndArguments_AreKept_Test()
    {
        const string json = """
            {"definitions": {"pick": {"parameters": ["p"], "body": {"op": "atLeast", "k": 1, "operands": [{"predicate": "p"}, {"predicate": "d", "args": {"n": 2, "s": "x", "f": 1.5, "b": true, "l": ["a"]}}]}}},
             "op": "use", "name": "pick", "operands": [{"predicate": "a", "note": "first"}]}
            """;
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(lenient: true);

        string printed = RuleDefinitions.ReadJson(json).PrintJson()!;

        CompiledRule<RuleTestContext> original = compiler.CompileJson(json).Rule!;
        CompiledRule<RuleTestContext> again = compiler.CompileJson(printed).Rule!;
        Assert.False(RuleDiff.Compare(original, again).HasChanges);
        Assert.Equal(original.Notes, again.Notes);
        Assert.Contains("\"note\":\"first\"", printed, StringComparison.Ordinal);
    }

    /// <summary>JSON nodes carry no positions, so the spans of a definition and a use are found from their paths.</summary>
    [Fact]
    public void ReadJson_DefinitionsAndUses_LocatesSpansFromPaths_Test()
    {
        const string json = """{"definitions": {"one": {"body": {"predicate": "a"}}}, "op": "use", "name": "one"}""";

        RuleDefinitions read = RuleDefinitions.ReadJson(json);

        Assert.Equal(json.IndexOf("{\"body\"", StringComparison.Ordinal), read.Definitions[0].Span.Start);
        Assert.Equal(json.LastIndexOf("\"one\"", StringComparison.Ordinal), read.Uses[0].NameSpan.Start);
        Assert.Equal(["$"], read.Uses[0].ExpandedPaths);
    }

    /// <summary>A syntax error is a diagnostic, not an exception: nothing is listed and nothing is printed.</summary>
    [Fact]
    public void ReadText_SyntaxError_FailsWithDiagnosticsAndPrintsNothing_Test()
    {
        RuleDefinitions read = RuleDefinitions.ReadText("let one = a AND; one");

        Assert.False(read.Succeeded);
        Assert.Contains(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(read.Definitions);
        Assert.Empty(read.Uses);
        Assert.Null(read.PrintJson());
    }

    /// <summary>A rule without definitions reads as such, and its printed tree is the plain tree of the rule.</summary>
    [Fact]
    public void ReadText_RuleWithoutDefinitions_HasNoDefinitions_Test()
    {
        RuleDefinitions read = RuleDefinitions.ReadText("a AND NOT b");

        Assert.True(read.Succeeded);
        Assert.False(read.HasDefinitions);
        Assert.Equal(["$.operands[1].operands[0]"], read.SourcesOf("$.operands[1].operands[0]"));
        Assert.DoesNotContain("definitions", read.PrintJson(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A use that the compiler refuses (here an unknown name) is still listed, at the place of its error, with no
    /// definition body under it.
    /// </summary>
    [Fact]
    public void ReadJson_UnknownDefinition_ListsTheUseWithoutABody_Test()
    {
        RuleDefinitions read = RuleDefinitions.ReadJson("""{"op": "use", "name": "nope"}""");

        DefinitionUse use = Assert.Single(read.Uses);
        Assert.Equal("nope", use.Name);
        Assert.Equal(["$"], read.SourcesOf("$"));
    }

    /// <summary>YAML reads to the same definitions and uses as rule text, and prints back as YAML that compiles to the same rule.</summary>
    [Fact]
    public void ReadYaml_DocumentWithDefinitions_ReadsAndPrintsBackAsYaml_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        string yaml = RuleDefinitions.ReadText(Text).PrintYaml()!;

        RuleDefinitions read = YamlRuleDefinitions.ReadAs(yaml, RuleFormat.Yaml);

        Assert.Equal(["both", "one"], read.Definitions.Select(d => d.Name));
        Assert.Equal(3, read.Uses.Count);
        Assert.NotEqual(SourceSpan.None, read.Uses[2].NameSpan);
        Assert.False(RuleDiff.Compare(compiler.Compile(Text).Rule!, compiler.CompileYaml(read.PrintYaml()!).Rule!).HasChanges);
    }

    /// <summary>ReadAs picks the reader of each format and rejects a format that is not defined.</summary>
    [Fact]
    public void ReadAs_EachFormat_UsesTheReaderOfTheFormat_Test()
    {
        string json = RuleDefinitions.ReadText(Text).PrintJson()!;

        Assert.Equal(2, YamlRuleDefinitions.ReadAs(Text, RuleFormat.Text).Definitions.Count);
        Assert.Equal(2, YamlRuleDefinitions.ReadAs(json, RuleFormat.Json).Definitions.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => YamlRuleDefinitions.ReadAs(Text, (RuleFormat)99));
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler(bool lenient = false)
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", false)
                .AddConstant("c", true)
                .Build(),
            new CompilerOptions(Mode: lenient ? CompilationMode.Lenient : CompilationMode.Strict)
        );
    }
}
