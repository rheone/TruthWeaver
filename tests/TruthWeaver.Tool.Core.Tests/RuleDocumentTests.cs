namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class RuleDocumentTests
{
    private const string LongRule =
        "isActive AND hasRole(role: \"editor\") AND isVerified AND NOT isSuspended AND (isOwner OR isAdministrator OR isModerator)";

    /// <summary>Every change compiles the new text, so the rule follows the text.</summary>
    [Fact]
    public void SetText_ValidText_CompilesTheNewRule_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive");

        document.SetText("isActive AND isVerified");

        Assert.True(document.IsRuleCurrent);
        Assert.Equal("isActive AND isVerified", document.Rule!.CanonicalText);
        Assert.DoesNotContain(document.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>Text with a syntax error keeps the last tree that compiled and reports the error with its span.</summary>
    [Fact]
    public void SetText_SyntaxError_KeepsLastValidRuleAndMarksTheError_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive AND isVerified");

        document.SetText("isActive AND (isVerified");

        Assert.False(document.IsRuleCurrent);
        Assert.Equal("isActive AND isVerified", document.Rule!.CanonicalText);
        Diagnostic error = Assert.Single(document.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.InRange(error.Span.Start, 0, document.Text.Length);
    }

    /// <summary>A document whose text never compiled has no rule.</summary>
    [Fact]
    public void Create_TextThatDoesNotCompile_HasNoRule_Test()
    {
        RuleDocument document = RuleDocument.Create("AND AND");

        Assert.Null(document.Rule);
        Assert.False(document.IsRuleCurrent);
        Assert.Contains(document.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>Undo restores the text before the last change, and the rule compiled from it.</summary>
    [Fact]
    public void Undo_AfterAnEdit_RestoresTheTextAndTheRule_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive");
        document.SetText("isActive OR isAdministrator");

        bool undone = document.Undo();

        Assert.True(undone);
        Assert.Equal("isActive", document.Text);
        Assert.Equal("isActive", document.Rule!.CanonicalText);
        Assert.True(document.CanRedo);
        Assert.False(document.CanUndo);
    }

    /// <summary>Redo applies a step that undo went back.</summary>
    [Fact]
    public void Redo_AfterUndo_AppliesTheEditAgain_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive");
        document.SetText("isActive OR isAdministrator");
        document.Undo();

        bool redone = document.Redo();

        Assert.True(redone);
        Assert.Equal("isActive OR isAdministrator", document.Text);
        Assert.False(document.CanRedo);
    }

    /// <summary>A new edit after undo drops the steps that redo could apply.</summary>
    [Fact]
    public void SetText_AfterUndo_ClearsTheRedoSteps_Test()
    {
        RuleDocument document = RuleDocument.Create("a");
        document.SetText("a AND b");
        document.Undo();

        document.SetText("a OR c");

        Assert.False(document.CanRedo);
        Assert.False(document.Redo());
    }

    /// <summary>Undo and redo with no step change nothing and say so.</summary>
    [Fact]
    public void Undo_NoHistory_ReturnsFalse_Test()
    {
        RuleDocument document = RuleDocument.Create("a");

        Assert.False(document.Undo());
        Assert.False(document.Redo());
        Assert.Equal("a", document.Text);
    }

    /// <summary>Undo takes the document back to a text that compiled, so the rule is current again.</summary>
    [Fact]
    public void Undo_AfterABrokenEdit_MakesTheRuleCurrentAgain_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");
        document.SetText("a AND");

        document.Undo();

        Assert.True(document.IsRuleCurrent);
        Assert.DoesNotContain(document.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>The canonical form prints the rule on one line in the operator style of the settings.</summary>
    [Fact]
    public void FormatText_Canonical_PrintsOneLine_Test()
    {
        RuleDocument document = RuleDocument.Create("a   and\n  (b or   c)");

        ToolResult<string> formatted = document.FormatText(pretty: false);

        Assert.True(formatted.Succeeded);
        Assert.Equal("a AND (b OR c)", document.Text);
        Assert.Equal(document.Text, formatted.Value);
    }

    /// <summary>The pretty form breaks a long rule over lines of at most 80 characters.</summary>
    [Fact]
    public void FormatText_Pretty_BreaksALongRule_Test()
    {
        RuleDocument document = RuleDocument.Create(LongRule);

        document.FormatText(pretty: true);

        string[] lines = document.Text.Split('\n');
        Assert.True(lines.Length > 1);
        Assert.All(lines, line => Assert.True(line.Length <= ArtifactCatalog.PrettyWidth, line));
        Assert.True(document.IsRuleCurrent);
    }

    /// <summary>The pretty form of JSON is indented; the canonical form is one line.</summary>
    [Fact]
    public void FormatText_Json_PrettyIndentsAndCanonicalIsOneLine_Test()
    {
        RuleDocument document = RuleDocument.Create(
            """{ "op": "and", "operands": [ { "predicate": "a" }, { "predicate": "b" } ] }""",
            RuleFormat.Json
        );

        document.FormatText(pretty: true);
        string pretty = document.Text;
        document.FormatText(pretty: false);

        Assert.Contains('\n', pretty);
        Assert.DoesNotContain('\n', document.Text);
        Assert.True(document.IsRuleCurrent);
    }

    /// <summary>Text that does not compile is not formatted: the text stays as the author wrote it.</summary>
    [Fact]
    public void FormatText_TextWithAnError_ChangesNothing_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND");

        ToolResult<string> formatted = document.FormatText(pretty: false);

        Assert.False(formatted.Succeeded);
        Assert.Equal("a AND", document.Text);
        Assert.False(document.CanUndo);
    }

    /// <summary>Formatting is one undo step.</summary>
    [Fact]
    public void FormatText_ThenUndo_RestoresTheOriginalLayout_Test()
    {
        RuleDocument document = RuleDocument.Create("a   and b");
        document.FormatText(pretty: false);

        document.Undo();

        Assert.Equal("a   and b", document.Text);
    }

    /// <summary>Each change raises <see cref="RuleDocument.Changed"/> once.</summary>
    [Fact]
    public void SetText_NewText_RaisesChangedOnce_Test()
    {
        RuleDocument document = RuleDocument.Create("a");
        int raised = 0;
        document.Changed += (_, _) => raised++;

        document.SetText("b");
        document.SetText("b");

        Assert.Equal(1, raised);
    }

    /// <summary>An edit makes the document dirty, and a save makes it clean again.</summary>
    [Fact]
    public void Save_AfterAnEdit_WritesTheFileAndClearsDirty_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.rule");
        RuleDocument document = RuleDocument.Create("a");
        document.SetText("a AND b");

        ToolResult<string> saved = document.Save(path);

        Assert.True(saved.Succeeded);
        Assert.False(document.IsDirty);
        Assert.Equal(Path.GetFullPath(path), document.Path);
        Assert.Equal("a AND b\n", File.ReadAllText(path));
    }

    /// <summary>Saving with a JSON extension converts the rule to JSON, and the file opens to the same tree.</summary>
    [Fact]
    public void Save_JsonExtension_ConvertsTheRuleToJson_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.json");
        RuleDocument document = RuleDocument.Create("a AND NOT b");

        document.Save(path);
        RuleDocument reopened = RuleDocument.Open(path).Value!;

        Assert.Equal(RuleFormat.Json, document.Format);
        Assert.Equal(RuleFormat.Json, reopened.Format);
        Assert.Equal("a AND NOT b", reopened.Rule!.CanonicalText);
        Assert.False(document.IsDirty);
    }

    /// <summary>Saving in another format needs text that compiles; otherwise nothing is written.</summary>
    [Fact]
    public void Save_OtherFormatWithAnError_WritesNothing_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.yaml");
        RuleDocument document = RuleDocument.Create("a AND");

        ToolResult<string> saved = document.Save(path);

        Assert.False(saved.Succeeded);
        Assert.False(File.Exists(path));
        Assert.Equal(RuleFormat.Text, document.Format);
    }

    /// <summary>Opening a YAML file finds its format from the extension and compiles it.</summary>
    [Fact]
    public void Open_YamlFile_DetectsTheFormatAndCompiles_Test()
    {
        ToolResult<RuleDocument> opened = RuleDocument.Open(Fixtures.Path("access.yaml"));

        Assert.True(opened.Succeeded);
        Assert.Equal(RuleFormat.Yaml, opened.Value.Format);
        Assert.True(opened.Value.IsRuleCurrent);
        Assert.False(opened.Value.IsDirty);
    }

    /// <summary>A file that cannot be read gives diagnostics, not a document.</summary>
    [Fact]
    public void Open_MissingFile_ReturnsTheDiagnostics_Test()
    {
        using TemporaryDirectory directory = new();

        ToolResult<RuleDocument> opened = RuleDocument.Open(directory.File("missing.rule"));

        Assert.False(opened.Succeeded);
        Assert.Contains(opened.Diagnostics, d => d.Code == ToolDiagnosticCodes.InputUnreadable);
    }

    /// <summary>
    /// A structural edit replaces the rule as one undo step, and undo and redo restore the text and the tree exactly.
    /// </summary>
    [Fact]
    public void ReplaceRule_EditedTree_IsOneStepThatUndoAndRedoRestore_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");
        string before = document.Text;
        string beforeTree = document.Rule!.PrintJson(includeNotes: false);
        RuleTreeEdit edit = new RuleTreeEditor(document.Rule).WrapInNot("$.operands[1]");

        ToolResult<string> replaced = document.ReplaceRule(edit.Rule!);
        string after = document.Text;
        string afterTree = document.Rule.PrintJson(includeNotes: false);
        document.Undo();
        (string Text, string Tree) undone = (document.Text, document.Rule.PrintJson(includeNotes: false));
        document.Redo();

        Assert.True(replaced.Succeeded);
        Assert.Equal("a AND NOT b", after);
        Assert.Equal((before, beforeTree), undone);
        Assert.Equal((after, afterTree), (document.Text, document.Rule.PrintJson(includeNotes: false)));
    }

    /// <summary>A JSON document keeps its format: the edited rule is written as JSON.</summary>
    [Fact]
    public void ReplaceRule_JsonDocument_WritesTheRuleAsJson_Test()
    {
        RuleDocument document = RuleDocument.Create(
            """{"op": "and", "operands": [{"predicate": "a"}, {"predicate": "b"}]}""",
            RuleFormat.Json
        );
        RuleTreeEdit edit = new RuleTreeEditor(document.Rule!).ReplaceOperator("$", "or");

        document.ReplaceRule(edit.Rule!);

        Assert.Equal(RuleFormat.Json, document.Format);
        Assert.True(document.IsRuleCurrent);
        Assert.Equal("a OR b", document.Rule!.CanonicalText);
    }
}
