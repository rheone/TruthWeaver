namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>Opening, switching and saving a rule that lives inside a larger JSON or YAML document, with no terminal.</summary>
public sealed class EmbeddedRuleControllerTests : IDisposable
{
    private const string CommentedYaml = """
        # Access policies for the editor service.
        service: editor
        rules:
          # Who may publish.
          publish:
            op: and  # all of these
            note: Editors who are active.
            operands:
              # The account must be active.
              - predicate: isActive  # checked first
                note: Checked first.
              - predicate: hasRole
                args:
                  role: editor   # the editor role
          review: 'isActive AND hasRole(role: "reviewer")'
          inline: {op: not, operands: [{predicate: isBanned}]}
        # Who owns the rules.
        owner: platform-team

        """;

    private const string Json = """
        {
          "service": "editor",
          "rules": {
            "publish": { "op": "and", "operands": [ { "predicate": "isActive" }, { "predicate": "isAdmin" } ] },
            "review": "isActive AND isReviewer"
          }
        }

        """;

    private readonly string folder = Directory.CreateTempSubdirectory("tw-embedded-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.folder, recursive: true);
    }

    /// <summary>Opening a rule by its path shows the rule alone in the workbench and names the document and path.</summary>
    [Fact]
    public void OpenAt_PathOfARule_ShowsTheRuleAlone_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );

        bool opened = controller.OpenAt(file, "rules.publish");

        Assert.True(opened);
        Assert.Equal(RuleFormat.Yaml, workbench.Document.Format);
        Assert.StartsWith("op: and", workbench.Document.Text.TrimStart(), StringComparison.Ordinal);
        Assert.Equal("policies.yaml:$.rules.publish", workbench.DocumentLabel);
        Assert.Contains("policies.yaml:$.rules.publish", WorkbenchWindow.StatusOf(workbench), StringComparison.Ordinal);
    }

    /// <summary>A path that selects a string opens rule text.</summary>
    [Fact]
    public void OpenAt_PathOfAString_OpensRuleText_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );

        controller.OpenAt(file, "$.rules.review");

        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
        Assert.Equal("isActive AND hasRole(role: \"reviewer\")", workbench.Document.Text);
    }

    /// <summary>Patch-in-place changes only the changed value; every byte outside the rule span, comments included, stays.</summary>
    [Fact]
    public void SaveInPlace_EditedYamlRule_LeavesEveryByteOutsideTheChangedValueUnchanged_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");
        workbench.Document.SetText(workbench.Document.Text.Replace("isActive", "isEnabled", StringComparison.Ordinal));
        services.Dialogs.Agree = true;

        bool saved = workbench.Save();

        Assert.True(saved);
        string expected = CommentedYaml.Replace(
            "- predicate: isActive  #",
            "- predicate: isEnabled  #",
            StringComparison.Ordinal
        );
        Assert.Equal(expected, File.ReadAllText(file));
        Assert.False(workbench.Document.IsDirty);
        Assert.Single(services.Dialogs.Confirmations);
    }

    /// <summary>Everything before and after the rule's own span is byte for byte the original, also when the rule is rewritten.</summary>
    [Fact]
    public void SaveInPlace_RuleWithAStructuralChange_KeepsTextOutsideTheRule_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.inline");
        workbench.Document.SetText("{op: or, operands: [{predicate: isBanned}, {predicate: isGuest}]}");
        services.Dialogs.Agree = true;

        bool saved = controller.SaveInPlace();

        Assert.True(saved);
        string written = File.ReadAllText(file);
        int span = CommentedYaml.IndexOf("inline:", StringComparison.Ordinal);
        Assert.StartsWith(CommentedYaml[..span], written, StringComparison.Ordinal);
        Assert.EndsWith("# Who owns the rules.\nowner: platform-team\n", written, StringComparison.Ordinal);
        Assert.Contains("isGuest", written, StringComparison.Ordinal);
    }

    /// <summary>The document is never written when the author declines the confirmation.</summary>
    [Fact]
    public void SaveInPlace_ConfirmationDeclined_WritesNothing_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");
        workbench.Document.SetText(workbench.Document.Text.Replace("isActive", "isEnabled", StringComparison.Ordinal));
        services.Dialogs.Agree = false;

        bool saved = controller.SaveInPlace();

        Assert.False(saved);
        Assert.Equal(CommentedYaml, File.ReadAllText(file));
        Assert.True(workbench.Document.IsDirty);
    }

    /// <summary>The confirmation says what a changed rule on disk means before the file is replaced.</summary>
    [Fact]
    public void SaveInPlace_RuleChangedOnDiskSinceOpening_WarnsInTheConfirmation_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");
        File.WriteAllText(file, CommentedYaml.Replace("role: editor", "role: owner", StringComparison.Ordinal));
        workbench.Document.SetText(workbench.Document.Text.Replace("isActive", "isEnabled", StringComparison.Ordinal));

        controller.SaveInPlace();

        Assert.Contains("changed since you opened it", Assert.Single(services.Dialogs.Confirmations), StringComparison.Ordinal);
    }

    /// <summary>A rule that has not changed writes nothing and says so, without asking.</summary>
    [Fact]
    public void SaveInPlace_UnchangedRule_WritesNothing_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");

        bool saved = controller.SaveInPlace();

        Assert.True(saved);
        Assert.Empty(services.Dialogs.Confirmations);
        Assert.Equal(CommentedYaml, File.ReadAllText(file));
        Assert.StartsWith("Nothing to write", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>The round-trip write-back warns that YAML comments and layout may be lost before it writes anything.</summary>
    [Fact]
    public void SaveRoundTrip_Declined_WarnsAndWritesNothing_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");
        workbench.Document.SetText(workbench.Document.Text.Replace("isActive", "isEnabled", StringComparison.Ordinal));
        services.Dialogs.Agree = false;

        bool saved = controller.SaveRoundTrip();

        Assert.False(saved);
        string warning = Assert.Single(services.Dialogs.Confirmations);
        Assert.Contains("comments", warning, StringComparison.Ordinal);
        Assert.Contains("may be lost", warning, StringComparison.Ordinal);
        Assert.Equal(CommentedYaml, File.ReadAllText(file));
    }

    /// <summary>After the author accepts the warning, the round-trip write-back writes the document again with the edited rule.</summary>
    [Fact]
    public void SaveRoundTrip_Accepted_WritesTheWholeDocumentAgain_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");
        workbench.Document.SetText(workbench.Document.Text.Replace("isActive", "isEnabled", StringComparison.Ordinal));
        services.Dialogs.Agree = true;

        bool saved = controller.SaveRoundTrip();

        Assert.True(saved);
        string written = File.ReadAllText(file);
        Assert.Contains("isEnabled", written, StringComparison.Ordinal);
        Assert.DoesNotContain("# Who owns the rules.", written, StringComparison.Ordinal);
        Assert.Contains("platform-team", written, StringComparison.Ordinal);
    }

    /// <summary>A path that names nothing reports it, opens nothing and leaves the document and the file alone.</summary>
    [Fact]
    public void OpenAt_PathThatMatchesNothing_ReportsItAndChangesNothing_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );

        bool opened = controller.OpenAt(file, "rules.missing");

        Assert.False(opened);
        Assert.Contains("selects nothing", workbench.Status, StringComparison.Ordinal);
        Assert.Null(controller.Session);
        Assert.Equal(CommentedYaml, File.ReadAllText(file));
    }

    /// <summary>A path to a value that is not a rule reports it.</summary>
    [Fact]
    public void OpenAt_PathOfANumber_SaysTheValueIsNotARule_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );

        bool opened = controller.OpenAt(file, "owner.name");

        Assert.False(opened);
        Assert.Contains("selects nothing", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A wildcard path that matches no rule says so.</summary>
    [Fact]
    public void OpenAt_WildcardThatMatchesNoRule_SaysSo_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );

        bool opened = controller.OpenAt(file, "nothing.*");

        Assert.False(opened);
        Assert.Contains("matches no rule", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A path that matches several values lists them and opens the one the author picks.</summary>
    [Fact]
    public void OpenAt_WildcardThatMatchesSeveralValues_AsksWhichOne_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        services.Dialogs.Choices.Enqueue(1);

        bool opened = controller.OpenAt(file, "$.rules.*");

        Assert.True(opened);
        Assert.Equal(3, services.Dialogs.LastItems.Count);
        Assert.Equal("$.rules.review", controller.Session!.RulePath);
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
    }

    /// <summary>Cancelling the pick among several matches opens nothing.</summary>
    [Fact]
    public void OpenAt_WildcardPickCancelled_OpensNothing_Test()
    {
        (EmbeddedRuleController controller, _, _, _, string file) = this.Create(CommentedYaml, "policies.yaml");

        bool opened = controller.OpenAt(file, "$.rules.*");

        Assert.False(opened);
        Assert.Null(controller.Session);
    }

    /// <summary>Browsing the document lists the entries with indentation, and picking one opens it.</summary>
    [Fact]
    public void OpenFile_BrowseAndPick_OpensTheChosenRule_Test()
    {
        (EmbeddedRuleController controller, _, ScriptedServices services, _, string file) = this.Create(Json, "policies.json");
        services.Dialogs.Choices.Enqueue(0);
        services.Dialogs.Choices.Enqueue(3);

        bool opened = controller.OpenFile(file);

        Assert.True(opened);
        Assert.Contains(services.Dialogs.LastItems, item => item.Contains("$.rules.publish", StringComparison.Ordinal));
        Assert.Equal("$.rules.publish", controller.Session!.RulePath);
    }

    /// <summary>Typing a path in the dialog opens that rule.</summary>
    [Fact]
    public void OpenFile_TypedPath_OpensTheRuleAtThePath_Test()
    {
        (EmbeddedRuleController controller, _, ScriptedServices services, _, string file) = this.Create(Json, "policies.json");
        services.Dialogs.Choices.Enqueue(1);
        services.Dialogs.Texts.Enqueue("$.rules.review");

        bool opened = controller.OpenFile(file);

        Assert.True(opened);
        Assert.Equal("$.rules.review", controller.Session!.RulePath);
    }

    /// <summary>A file of plain rule text has no paths, and the status line says to use the normal open key.</summary>
    [Fact]
    public void OpenFile_RuleTextFile_SaysItHasNoPaths_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create("a AND b\n", "plain.rule");

        bool opened = controller.OpenFile(file);

        Assert.False(opened);
        Assert.Contains("has no paths", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Switch lists the other rules of the document and opens the one the author picks.</summary>
    [Fact]
    public void Switch_DocumentWithSeveralRules_OpensTheOtherRule_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            CommentedYaml,
            "policies.yaml"
        );
        controller.OpenAt(file, "rules.publish");
        services.Dialogs.Choices.Enqueue(1);

        bool switched = controller.Switch();

        Assert.True(switched);
        Assert.Equal(2, services.Dialogs.LastItems.Count);
        Assert.Equal("$.rules.inline", controller.Session!.RulePath);
        Assert.Contains("isBanned", workbench.Document.Text, StringComparison.Ordinal);
    }

    /// <summary>Switching with unsaved changes asks first, and a refusal keeps the rule being edited.</summary>
    [Fact]
    public void Switch_UnsavedChangesAndRefusal_KeepsTheOpenRule_Test()
    {
        (
            EmbeddedRuleController controller,
            Workbench workbench,
            ScriptedServices services,
            ScriptedPrompts prompts,
            string file
        ) = this.Create(CommentedYaml, "policies.yaml");
        controller.OpenAt(file, "rules.publish");
        workbench.Document.SetText(workbench.Document.Text.Replace("isActive", "isEnabled", StringComparison.Ordinal));
        services.Dialogs.Choices.Enqueue(1);
        prompts.Discard = false;

        bool switched = controller.Switch();

        Assert.False(switched);
        Assert.Equal(1, prompts.DiscardQuestions);
        Assert.Equal("$.rules.publish", controller.Session!.RulePath);
    }

    /// <summary>Switch without an open embedded rule says how to open one.</summary>
    [Fact]
    public void Switch_NoEmbeddedRuleOpen_SaysSo_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, _) = this.Create(Json, "policies.json");

        Assert.False(controller.Switch());
        Assert.Contains("No rule of a larger document", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Opening a normal file again takes the save key back from the embedded rule.</summary>
    [Fact]
    public void OpenPath_AfterAnEmbeddedRule_RestoresTheNormalSave_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, _, _, string file) = this.Create(Json, "policies.json");
        controller.OpenAt(file, "$.rules.review");
        string plain = Path.Combine(this.folder, "plain.rule");
        File.WriteAllText(plain, "x AND y\n");

        workbench.OpenPath(plain);

        Assert.Null(workbench.SaveOverride);
        Assert.Null(workbench.DocumentLabel);
    }

    /// <summary>A JSON document is patched in place too, and its other keys stay.</summary>
    [Fact]
    public void SaveInPlace_EditedJsonRule_ChangesOnlyTheRule_Test()
    {
        (EmbeddedRuleController controller, Workbench workbench, ScriptedServices services, _, string file) = this.Create(
            Json,
            "policies.json"
        );
        controller.OpenAt(file, "$.rules.publish");
        workbench.Document.SetText(workbench.Document.Text.Replace("isAdmin", "isOwner", StringComparison.Ordinal));
        services.Dialogs.Agree = true;

        controller.SaveInPlace();

        Assert.Equal(Json.Replace("isAdmin", "isOwner", StringComparison.Ordinal), File.ReadAllText(file));
    }

    private (
        EmbeddedRuleController Controller,
        Workbench Workbench,
        ScriptedServices Services,
        ScriptedPrompts Prompts,
        string File
    ) Create(string content, string name)
    {
        string file = Path.Combine(this.folder, name);
        File.WriteAllText(file, content.ReplaceLineEndings("\n"));
        ScriptedPrompts prompts = new();
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(), ToolSettings.Default, prompts);
        return (new EmbeddedRuleController(workbench, services.Services), workbench, services, prompts, file);
    }
}
