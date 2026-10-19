namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Globalization;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;
using TruthWeaver.Yaml;

/// <summary>The Generate rule action: a seeded random rule that compiles, with the operator families the author chose.</summary>
public sealed class GenerateRuleTests
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly (string Name, string Pattern)[] Families =
    [
        ("Connectives", @"\b(NOT|AND|OR|XOR|EQUIVALENT|IMPLIES|NAND|NOR)\b|\bPARITY\("),
        ("Cardinality", @"\b(ANY|ALL|NONE|BETWEEN|ExactlyOne|AtLeast|AtMost|Exactly|GreaterThan|LessThan)\("),
        ("If", @"\bIf\("),
        ("Inspection", @"\b(IsTrue|IsFalse|IsUnknown|IsKnown|COALESCE)\("),
    ];

    /// <summary>The same seed, depth and families give the same text.</summary>
    [Fact]
    public void Generate_SameSeedDepthAndFamilies_GivesTheSameText_Test()
    {
        string? first = RuleGeneration.Generate(42, 3, RuleOperatorFamilies.All, ToolSettings.Default);
        string? second = RuleGeneration.Generate(42, 3, RuleOperatorFamilies.All, ToolSettings.Default);
        string? other = RuleGeneration.Generate(43, 3, RuleOperatorFamilies.All, ToolSettings.Default);

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    /// <summary>Each of 100 seeds gives a rule that compiles.</summary>
    [Fact]
    public void Generate_HundredSeeds_EachCompiles_Test()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            string? text = RuleGeneration.Generate(
                seed,
                RuleGeneration.DefaultDepth,
                RuleOperatorFamilies.All,
                ToolSettings.Default
            );

            Assert.NotNull(text);
            Assert.True(
                RuleConverter.CreateCompiler(ToolSettings.Default).CompileAs(text, RuleFormat.Text).Rule is not null,
                $"seed {seed}: {text}"
            );
        }
    }

    /// <summary>Together, 100 default-option rules use every operator family at least once.</summary>
    [Fact]
    public void Generate_HundredDefaultRules_TogetherUseEveryOperatorFamily_Test()
    {
        string[] texts =
        [
            .. Enumerable
                .Range(0, 100)
                .Select(seed =>
                    RuleGeneration.Generate(seed, RuleGeneration.DefaultDepth, RuleOperatorFamilies.All, ToolSettings.Default)!
                ),
        ];

        foreach ((string name, string pattern) in Families)
        {
            Assert.True(
                texts.Any(t => Regex.IsMatch(t, pattern, RegexOptions.None, RegexTimeout)),
                $"No rule uses the family {name}."
            );
        }
    }

    /// <summary>The terms are predicate names from the catalog, with arguments.</summary>
    [Fact]
    public void Generate_Terms_ArePredicatesOfTheCatalogWithArguments_Test()
    {
        string text = RuleGeneration.Generate(7, 2, RuleOperatorFamilies.Connectives, ToolSettings.Default)!;

        HashSet<string> names = [.. PredicateCatalog.All.Select(p => p.Entry.Name)];
        string[] calls =
        [
            .. Regex.Matches(text, @"\b[A-Za-z]\w*\(", RegexOptions.None, RegexTimeout).Select(m => m.Value.TrimEnd('(')),
        ];
        Assert.Contains(calls, names.Contains);
    }

    /// <summary>A narrowed family list keeps the rule inside it.</summary>
    [Fact]
    public void Generate_CardinalityOnly_UsesNoConnectiveAndNoIf_Test()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            string text = RuleGeneration.Generate(seed, 3, RuleOperatorFamilies.Cardinality, ToolSettings.Default)!;

            Assert.DoesNotMatch(new Regex(Families[0].Pattern, RegexOptions.None, RegexTimeout), text);
            Assert.DoesNotMatch(new Regex(Families[2].Pattern, RegexOptions.None, RegexTimeout), text);
        }
    }

    /// <summary>The action asks for the seed, depth and families, replaces the rule, shows the seed and undo restores the old rule.</summary>
    [Fact]
    public void Generate_Command_ReplacesTheRuleShowsTheSeedAndUndoRestoresIt_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Texts.Enqueue("123");
        services.Dialogs.Choices.Enqueue(1); // depth 2
        services.Dialogs.Choices.Enqueue(0); // every family
        Workbench workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, new ScriptedPrompts());

        bool generated = GenerateCommands.Generate(workbench, services.Services);
        string text = workbench.Document.Text;
        string status = workbench.Status;
        workbench.Undo();

        Assert.True(generated);
        Assert.Equal(RuleGeneration.Generate(123, 2, RuleOperatorFamilies.All, ToolSettings.Default), text);
        Assert.Contains("seed 123", status, StringComparison.Ordinal);
        Assert.Equal("a AND b", workbench.Document.Text);
    }

    /// <summary>A blank seed is random and the status line still names it, so the rule can be repeated.</summary>
    [Fact]
    public void Generate_Command_BlankSeed_ShowsTheChosenSeed_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Texts.Enqueue(" ");
        services.Dialogs.Choices.Enqueue(2);
        services.Dialogs.Choices.Enqueue(0);
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        bool generated = GenerateCommands.Generate(workbench, services.Services);
        Match seed = Regex.Match(workbench.Status, @"seed (\d+)", RegexOptions.None, RegexTimeout);

        Assert.True(generated);
        Assert.True(seed.Success);
        int value = int.Parse(seed.Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.Equal(
            RuleGeneration.Generate(value, 3, RuleOperatorFamilies.All, ToolSettings.Default),
            workbench.Document.Text
        );
    }

    /// <summary>A seed that is not a number changes nothing and says so.</summary>
    [Fact]
    public void Generate_Command_SeedNotANumber_KeepsTheRuleAndSaysSo_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Texts.Enqueue("abc");
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        bool generated = GenerateCommands.Generate(workbench, services.Services);

        Assert.False(generated);
        Assert.Equal("a", workbench.Document.Text);
        Assert.Contains("not a whole number", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A cancelled dialog changes nothing.</summary>
    [Fact]
    public void Generate_Command_Cancelled_KeepsTheRule_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Texts.Enqueue("5");
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        bool generated = GenerateCommands.Generate(workbench, services.Services);

        Assert.False(generated);
        Assert.Equal("a", workbench.Document.Text);
    }

    /// <summary>Unsaved changes need the same consent as Open: a no keeps the rule.</summary>
    [Fact]
    public void Generate_Command_UnsavedChangesWithoutConsent_KeepsTheRule_Test()
    {
        ScriptedServices services = new();
        services.Dialogs.Texts.Enqueue("5");
        services.Dialogs.Choices.Enqueue(2);
        services.Dialogs.Choices.Enqueue(0);
        ScriptedPrompts prompts = new() { Discard = false };
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, prompts);
        workbench.Document.SetText("a AND b");

        bool generated = GenerateCommands.Generate(workbench, services.Services);

        Assert.False(generated);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.Equal(1, prompts.DiscardQuestions);
    }
}
