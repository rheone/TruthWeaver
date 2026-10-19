namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;
using TruthWeaver.Yaml;

/// <summary>The sample rules: each compiles in the permissive mode, together they cover the language, and choosing one goes through the undo history.</summary>
public sealed class SampleRulesTests
{
    /// <summary>Every sample compiles without an error in the permissive mode of the workbench and carries at least one note.</summary>
    [Fact]
    public void All_EverySample_CompilesAndCarriesNotes_Test()
    {
        Assert.NotEmpty(SampleRules.All);
        foreach (SampleRule sample in SampleRules.All)
        {
            CompilationResult<TruthWeaver.Tool.Core.Assignment.StubAssignment> result = RuleConverter
                .CreateCompiler(ToolSettings.Default)
                .CompileAs(sample.Text, RuleFormat.Text);

            if (sample.ExpectedDiagnostic is not null)
            {
                // A sample that shows a diagnostic on purpose must report that code and explain it in a comment.
                Assert.Contains(result.Diagnostics, d => d.Code == sample.ExpectedDiagnostic);
                Assert.Contains("//", sample.Text, StringComparison.Ordinal);
                continue;
            }

            Assert.True(
                result.Rule is not null,
                $"{sample.Name}: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}"
            );
            Assert.True(result.Rule.Notes.Count > 0, $"{sample.Name} has no note.");
        }
    }

    /// <summary>Exactly one sample shows a diagnostic on purpose: the quantifier sample, with TRE0042 (UnknownCollection).</summary>
    [Fact]
    public void All_SamplesWithAnExpectedDiagnostic_AreOnlyTheQuantifierSample_Test()
    {
        SampleRule[] expected = [.. SampleRules.All.Where(s => s.ExpectedDiagnostic is not null)];

        SampleRule only = Assert.Single(expected);
        Assert.Equal("Quantifiers", only.Name);
        Assert.Equal("TRE0042", only.ExpectedDiagnostic);
        Assert.Contains("host must declare", only.Text, StringComparison.Ordinal);
    }

    /// <summary>Every part of the language that the ticket names has at least one sample, and the check ignores the notes.</summary>
    [Fact]
    public void Features_EveryFeature_HasASampleThatShowsIt_Test()
    {
        string[] missing =
        [
            .. SampleRules
                .Features.Where(feature => !SampleRules.All.Any(sample => feature.Pattern.IsMatch(WithoutNotes(sample.Text))))
                .Select(feature => feature.Name),
        ];

        Assert.Empty(missing);
    }

    /// <summary>The samples have different names and a one-line description each.</summary>
    [Fact]
    public void All_Samples_HaveDistinctNamesAndOneLineDescriptions_Test()
    {
        Assert.Equal(
            SampleRules.All.Count,
            SampleRules.All.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()
        );
        Assert.All(
            SampleRules.All,
            s => Assert.False(string.IsNullOrWhiteSpace(s.Description) || s.Description.Contains('\n'))
        );
    }

    /// <summary>Choosing a sample puts its text in place of the rule as one undo step, and undo gives the old rule back.</summary>
    [Fact]
    public void OpenSample_OnACleanRule_ReplacesTheRuleAndUndoRestoresIt_Test()
    {
        ScriptedPrompts prompts = new();
        Workbench workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, prompts);
        SampleRule sample = SampleRules.All[0];

        bool opened = workbench.OpenSample(sample);
        string replaced = workbench.Document.Text;
        workbench.Undo();

        Assert.True(opened);
        Assert.Equal(sample.Text, replaced);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.Equal(0, prompts.DiscardQuestions);
    }

    /// <summary>A sample replaces a rule in another format: undo gives back the text and the format.</summary>
    [Fact]
    public void OpenSample_OverJson_SwitchesToRuleTextAndUndoRestoresTheFormat_Test()
    {
        Workbench workbench = new(
            RuleDocument.Create("""{"predicate":"a"}""", RuleFormat.Json),
            ToolSettings.Default,
            new ScriptedPrompts()
        );

        workbench.OpenSample(SampleRules.All[0]);
        RuleFormat after = workbench.Document.Format;
        workbench.Undo();

        Assert.Equal(RuleFormat.Text, after);
        Assert.Equal(RuleFormat.Json, workbench.Document.Format);
    }

    /// <summary>Unsaved changes need the same consent as Open: a no keeps the rule.</summary>
    [Fact]
    public void OpenSample_WithUnsavedChangesAndNoConsent_KeepsTheRule_Test()
    {
        ScriptedPrompts prompts = new() { Discard = false };
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, prompts);
        workbench.Document.SetText("a AND b");

        bool opened = workbench.OpenSample(SampleRules.All[0]);

        Assert.False(opened);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.Equal(1, prompts.DiscardQuestions);
    }

    /// <summary>With consent, a sample replaces a rule that has unsaved changes.</summary>
    [Fact]
    public void OpenSample_WithUnsavedChangesAndConsent_ReplacesTheRule_Test()
    {
        ScriptedPrompts prompts = new() { Discard = true };
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, prompts);
        workbench.Document.SetText("a AND b");

        bool opened = workbench.OpenSample(SampleRules.All[1]);

        Assert.True(opened);
        Assert.Equal(SampleRules.All[1].Text, workbench.Document.Text);
    }

    /// <summary>The samples list shows name and description, names the action on its button and uses the chosen sample.</summary>
    [Fact]
    public void Choose_PicksASampleFromTheList_UsesIt_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Choices.Enqueue(2);
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        bool chosen = SampleCommands.Choose(workbench, services.Services);

        Assert.True(chosen);
        Assert.Equal(SampleRules.All[2].Text, workbench.Document.Text);
        Assert.Equal(SampleCommands.UseButton, services.Dialogs.LastAccept);
        Assert.Equal(SampleCommands.LineOf(SampleRules.All[0]), services.Dialogs.LastItems[0]);
        Assert.Equal(SampleRules.All.Count, services.Dialogs.LastItems.Count);
    }

    /// <summary>A cancelled list changes nothing.</summary>
    [Fact]
    public void Choose_Cancelled_ChangesNothing_Test()
    {
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        bool chosen = SampleCommands.Choose(workbench, services.Services);

        Assert.False(chosen);
        Assert.Equal("a", workbench.Document.Text);
    }

    // The text without its comments, so a word in a note does not count as a part of the language the sample shows.
    private static string WithoutNotes(string text)
    {
        return Regex.Replace(text, @"/\*.*?\*/|//[^\n]*", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
    }
}
