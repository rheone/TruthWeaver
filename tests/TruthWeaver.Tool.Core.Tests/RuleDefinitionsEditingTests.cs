namespace TruthWeaver.Tool.Core.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Yaml;

/// <summary>
/// A rule with <c>let</c> definitions in the tool core: the tree lists the definitions and marks each use, a structural
/// edit changes the definition body or the use argument and keeps the definitions, and the palette lists the definitions.
/// </summary>
public sealed class RuleDefinitionsEditingTests
{
    private const string Text = "let both(x, y) = x AND y;\nboth(a, b) OR c";

    /// <summary>
    /// The tree marks the node that a use expands to with the use and its arguments, shows the expanded body with the
    /// arguments in place below it, and lists the definition with its parameters after the rule.
    /// </summary>
    [Fact]
    public void Of_DocumentWithDefinitions_MarksTheUseAndListsTheDefinition_Test()
    {
        RuleTree tree = Tree(Text);

        Assert.Equal(
            [
                ("$", 0, RuleTreeNodeKind.Operator, "or"),
                ("$.operands[0]", 1, RuleTreeNodeKind.Operator, "use both(a, b) = and"),
                ("$.operands[0].operands[0]", 2, RuleTreeNodeKind.Term, "a"),
                ("$.operands[0].operands[1]", 2, RuleTreeNodeKind.Term, "b"),
                ("$.operands[1]", 1, RuleTreeNodeKind.Term, "c"),
                ("$.definitions.both", 0, RuleTreeNodeKind.Definition, "let both(x, y)"),
                ("$.definitions.both.body", 1, RuleTreeNodeKind.Operator, "and"),
                ("$.definitions.both.body.operands[0]", 2, RuleTreeNodeKind.Parameter, "x (parameter)"),
                ("$.definitions.both.body.operands[1]", 2, RuleTreeNodeKind.Parameter, "y (parameter)"),
            ],
            tree.Nodes.Select(n => (n.Path, n.Depth, n.Kind, n.Label))
        );
        Assert.Equal(["both(a, b)"], tree.Find("$.operands[0]")!.Uses);
        Assert.Empty(tree.Find("$")!.Uses);
    }

    /// <summary>
    /// A use inside a definition body is listed as a use with its arguments below it, and a node of the rule that expands
    /// through two uses names both.
    /// </summary>
    [Fact]
    public void Of_UseInsideDefinitionBody_ListsTheUseWithItsArguments_Test()
    {
        RuleTree tree = Tree("let both(x, y) = x AND y; let one = both(a, c); one OR b");

        Assert.Equal("use one = both(a, c) = and", tree.Find("$.operands[0]")!.Label);
        Assert.Equal(["one", "both(a, c)"], tree.Find("$.operands[0]")!.Uses);
        RuleTreeNode use = tree.Find("$.definitions.one.body")!;
        Assert.Equal((RuleTreeNodeKind.Use, "use both(a, c)", 2), (use.Kind, use.Label, use.OperandCount));
        Assert.Equal("a", tree.Find("$.definitions.one.body.operands[0]")!.Label);
    }

    /// <summary>A tree made without definitions lists the expanded rule only, as before.</summary>
    [Fact]
    public void Of_WithoutDefinitions_ListsTheExpandedRuleOnly_Test()
    {
        RuleTree tree = RuleTree.Of(Compile(Text));

        Assert.Equal(5, tree.Nodes.Count);
        Assert.All(tree.Nodes, n => Assert.Empty(n.Uses));
        Assert.Null(tree.Definitions);
    }

    /// <summary>
    /// Replacing the operator of the root of an expanded use changes the definition body, so the document keeps the
    /// definition and the use, and every use sees the change.
    /// </summary>
    [Fact]
    public void ReplaceOperator_RootOfAUse_EditsTheDefinitionBody_Test()
    {
        RuleTreeEdit edit = Editor(Text).ReplaceOperator("$.operands[0]", "or");

        Assert.True(edit.Succeeded, edit.Message);
        Assert.Equal(Compile("(a OR b) OR c").CanonicalText, edit.Rule.CanonicalText);
        Assert.Equal("let both(x, y) = x OR y;\nboth(a, b) OR c", Apply(Text, edit));
    }

    /// <summary>Replacing a node in the place of a parameter changes the argument of that use only.</summary>
    [Fact]
    public void Replace_NodeInTheParameterPlace_EditsTheUseArgument_Test()
    {
        RuleTreeEdit edit = Editor(Text).Replace("$.operands[0].operands[1]", "d");

        Assert.Equal("let both(x, y) = x AND y;\nboth(a, d) OR c", Apply(Text, edit));
    }

    /// <summary>Wrapping the root of an expanded use wraps the use itself, not the definition body.</summary>
    [Fact]
    public void WrapInNot_RootOfAUse_WrapsTheUse_Test()
    {
        RuleTreeEdit edit = Editor(Text).WrapInNot("$.operands[0]");

        Assert.Equal("let both(x, y) = x AND y;\nNOT both(a, b) OR c", Apply(Text, edit));
    }

    /// <summary>Swapping two nodes that are parameters of the body swaps the parameters in the definition.</summary>
    [Fact]
    public void Swap_ParameterPlacesOfAUse_SwapsThemInTheDefinitionBody_Test()
    {
        RuleTreeEdit edit = Editor(Text).Swap("$.operands[0].operands[0]", 1);

        Assert.Equal("let both(x, y) = y AND x;\nboth(a, b) OR c", Apply(Text, edit));
        Assert.Equal("$.operands[0].operands[1]", edit.Path);
    }

    /// <summary>A node of a definition can be edited directly by its own path.</summary>
    [Fact]
    public void Negate_NodeOfADefinition_EditsTheDefinition_Test()
    {
        RuleTreeEdit edit = Editor(Text).Negate("$.definitions.both.body");

        Assert.Equal("let both(x, y) = (x NAND y);\nboth(a, b) OR c", Apply(Text, edit));
    }

    /// <summary>An edit that would leave the definition invalid (NOT of two operands) is refused, and nothing changes.</summary>
    [Fact]
    public void ReplaceOperator_BreaksTheDefinition_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor(Text).ReplaceOperator("$.operands[0]", "not");

        Assert.False(edit.Succeeded);
        Assert.StartsWith(
            "Not applied: the edit would make an invalid rule or definition.",
            edit.Message,
            StringComparison.Ordinal
        );
    }

    /// <summary>An operand added to a use gives the wrong number of arguments, so the compiler refuses the edit.</summary>
    [Fact]
    public void InsertOperand_IntoAUse_IsRefusedForTheArgumentCount_Test()
    {
        RuleTreeEdit edit = Editor("let both(x, y) = x AND y; let one = both(a, c); one OR b")
            .InsertOperand("$.definitions.one.body", "d");

        Assert.False(edit.Succeeded);
        Assert.Contains(edit.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionArgumentCountMismatch);
    }

    /// <summary>A literal on a parameter would turn it into a term, so the edit is refused.</summary>
    [Fact]
    public void ChangeLiteral_Parameter_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor(Text).ChangeLiteral("$.definitions.both.body.operands[0]", "role", "admin");

        Assert.False(edit.Succeeded);
        Assert.Contains("parameter", edit.Message, StringComparison.Ordinal);
    }

    /// <summary>The operator of a use cannot be replaced: the body of its definition holds the operator.</summary>
    [Fact]
    public void ReplaceOperator_UseInsideADefinition_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor("let both(x, y) = x AND y; let one = both(a, c); one OR b")
            .ReplaceOperator("$.definitions.one.body", "or");

        Assert.False(edit.Succeeded);
        Assert.Contains("use of a definition", edit.Message, StringComparison.Ordinal);
    }

    /// <summary>The entry of a definition is not a node to edit; its body is.</summary>
    [Fact]
    public void WrapInNot_DefinitionEntry_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor(Text).WrapInNot("$.definitions.both");

        Assert.False(edit.Succeeded);
        Assert.Contains("body of the definition", edit.Message, StringComparison.Ordinal);
    }

    /// <summary>A replacement in rule text can use a definition of the document.</summary>
    [Fact]
    public void Replace_ExpressionWithAUse_PutsTheUseIn_Test()
    {
        RuleTreeEdit edit = Editor(Text).Replace("$.operands[1]", "both(c, d)");

        Assert.Equal("let both(x, y) = x AND y;\nboth(a, b) OR both(c, d)", Apply(Text, edit));
    }

    /// <summary>An edit of a rule without definitions gives no document, so it is put in as the expanded rule.</summary>
    [Fact]
    public void WrapInNot_RuleWithoutDefinitions_GivesNoDocument_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").WrapInNot("$.operands[0]");

        Assert.True(edit.Succeeded);
        Assert.Null(edit.Document);
    }

    /// <summary>An edit of a JSON document keeps its definitions section and its use nodes.</summary>
    [Fact]
    public void ApplyEdit_JsonDocument_KeepsTheDefinitionsSection_Test()
    {
        string json = RuleDefinitions.ReadText(Text).PrintJson()!;
        RuleDocument document = RuleDocument.Create(json, RuleFormat.Json);
        RuleTreeEdit edit = new RuleTreeEditor(document.Rule!, null, document.Definitions).Replace("$.operands[1]", "d");

        ToolResult<string> applied = document.ApplyEdit(edit);

        Assert.True(applied.Succeeded);
        JsonObject tree = (JsonObject)JsonNode.Parse(document.Text)!;
        Assert.NotNull(tree["definitions"]?["both"]);
        Assert.Equal("use", tree["operands"]![0]!["op"]!.ToString());
        Assert.Equal("d", tree["operands"]![1]!["predicate"]!.ToString());
    }

    /// <summary>An edit of a YAML document keeps its definitions.</summary>
    [Fact]
    public void ApplyEdit_YamlDocument_KeepsTheDefinitions_Test()
    {
        string yaml = RuleDefinitions.ReadText(Text).PrintYaml()!;
        RuleDocument document = RuleDocument.Create(yaml, RuleFormat.Yaml);
        RuleTreeEdit edit = new RuleTreeEditor(document.Rule!, null, document.Definitions).ReplaceOperator(
            "$.operands[0]",
            "or"
        );

        Assert.True(document.ApplyEdit(edit).Succeeded);

        Assert.Equal(["both"], document.Definitions.Definitions.Select(d => d.Name));
        Assert.Contains("op: or", document.Text, StringComparison.Ordinal);
        Assert.Contains("op: use", document.Text, StringComparison.Ordinal);
    }

    /// <summary>Formatting rule text with definitions keeps the definitions.</summary>
    [Fact]
    public void FormatText_RuleWithDefinitions_KeepsTheDefinitions_Test()
    {
        RuleDocument document = RuleDocument.Create("let   both(x,y)=x and y;   both(a,b)   or c");

        ToolResult<string> formatted = document.FormatText(pretty: false);

        Assert.Equal("let both(x, y) = x AND y;\nboth(a, b) OR c", formatted.Value);
    }

    /// <summary>Converting a rule with definitions to JSON and back keeps the definitions.</summary>
    [Fact]
    public void ConvertTo_RuleWithDefinitions_KeepsTheDefinitionsBothWays_Test()
    {
        RuleDocument document = RuleDocument.Create(Text);

        ToolResult<string> json = document.ConvertTo(RuleFormat.Json);
        ToolResult<string> text = document.ConvertTo(RuleFormat.Text);

        Assert.Contains("\"definitions\"", json.Value, StringComparison.Ordinal);
        Assert.Equal(Text, text.Value);
    }

    /// <summary>The palette lists each definition with its parameters, and a filled entry is a use.</summary>
    [Fact]
    public void Create_DocumentWithDefinitions_ListsTheDefinitions_Test()
    {
        RuleDocument document = RuleDocument.Create(Text);

        RulePalette palette = RulePalette.Create(document.Rule, null, document.Definitions);

        PaletteEntry both = Assert.Single(palette.Entries, e => e.Source == PaletteSource.Definition);
        Assert.Equal(["x", "y"], both.Arguments.Select(a => a.Name));
        Assert.Equal("let      both(x: String, y: String)", both.Display);
        Assert.Equal("both(a, d)", RulePalette.Fill(both, ["a", " d "]).Value);
    }

    /// <summary>The definitions of a document follow its text and are read again after a change.</summary>
    [Fact]
    public void Definitions_TextChanges_FollowsTheText_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");
        bool before = document.Definitions.HasDefinitions;

        document.SetText(Text);

        Assert.False(before);
        Assert.Equal(["both"], document.Definitions.Definitions.Select(d => d.Name));
    }

    private static RuleTree Tree(string text)
    {
        return RuleTree.Of(Compile(text), null, RuleDefinitions.ReadText(text));
    }

    private static RuleTreeEditor Editor(string text)
    {
        return new RuleTreeEditor(Compile(text), null, RuleDefinitions.ReadText(text));
    }

    // Puts an edit into a document of the text and gives the new text.
    private static string Apply(string text, RuleTreeEdit edit)
    {
        Assert.True(edit.Succeeded, edit.Message);
        RuleDocument document = RuleDocument.Create(text);
        ToolResult<string> applied = document.ApplyEdit(edit);
        Assert.True(applied.Succeeded, string.Join("; ", applied.Diagnostics.Select(d => d.Message)));
        return document.Text;
    }

    private static CompiledRule<StubAssignment> Compile(string text, RuleFormat format = RuleFormat.Text)
    {
        CompilationResult<StubAssignment> result = RuleConverter.CreateCompiler().CompileAs(text, format);
        Assert.NotNull(result.Rule);
        return result.Rule;
    }
}
