namespace TruthWeaver.Tool.Tui.Commands;

using System.Globalization;
using Terminal.Gui.Input;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The Generate rule action: it asks for a seed, a depth and the operator families, and puts a random rule that compiles in place of the rule.</summary>
internal static class GenerateCommands
{
    /// <summary>The text of the accept button of the dialogs: it names the action.</summary>
    public const string GenerateButton = "_Generate";

    /// <summary>Gets the key of the Generate rule command.</summary>
    public static Key GenerateKey => Key.F5.WithShift;

    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            GenerateKey,
            CommandGroup.Rule,
            "Generate rule...",
            "Replace the rule with a random rule that compiles: asks for a seed (blank is random), a depth and the operator families (Ctrl+Z undoes it)",
            w => Generate(w.Workbench, WorkbenchServices.Current)
        ),
    ];

    /// <summary>The depths that the depth dialog lists, from <c>1</c> to <see cref="RuleGeneration.MaxDepth"/>.</summary>
    /// <returns>The lines of the dialog.</returns>
    public static IReadOnlyList<string> DepthLines()
    {
        return
        [
            .. Enumerable
                .Range(1, RuleGeneration.MaxDepth)
                .Select(d => d == RuleGeneration.DefaultDepth ? $"{d} (default)" : d.ToString(CultureInfo.InvariantCulture)),
        ];
    }

    /// <summary>Asks for the options and puts a generated rule in place of the rule. A cancelled question changes nothing.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs.</param>
    /// <returns><see langword="true"/> when a generated rule replaced the rule.</returns>
    public static bool Generate(Workbench workbench, WorkbenchServices services)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(services);

        string? seedText = services.Dialogs.AskText("Generate rule: seed (blank for a random seed)", string.Empty);
        if (seedText is null)
        {
            return false;
        }

        int seed;
        if (string.IsNullOrWhiteSpace(seedText))
        {
            seed = Random.Shared.Next();
        }
        else if (!int.TryParse(seedText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
        {
            workbench.SetStatus($"Generate rule: the seed '{seedText.Trim()}' is not a whole number.");
            return false;
        }

        IReadOnlyList<string> depths = DepthLines();
        if (services.Dialogs.Choose("Generate rule: depth", depths, GenerateButton) is not { } depthIndex || depthIndex < 0)
        {
            return false;
        }

        IReadOnlyList<(string Title, RuleOperatorFamilies Families)> choices = RuleGeneration.FamilyChoices;
        int? familyIndex = services.Dialogs.Choose(
            "Generate rule: operator families",
            [.. choices.Select(c => c.Title)],
            GenerateButton
        );
        if (familyIndex is not { } family || family < 0 || family >= choices.Count)
        {
            return false;
        }

        int depth = depthIndex + 1;
        string? text = RuleGeneration.Generate(seed, depth, choices[family].Families, workbench.Settings);
        if (text is null)
        {
            workbench.SetStatus(
                $"Generate rule: no rule that compiles in {RuleGeneration.MaxAttempts} attempts (seed {seed}, depth {depth}). Try another seed."
            );
            return false;
        }

        return workbench.OpenText(
            text,
            "replace the rule with a generated rule",
            $"Generated rule: seed {seed}, depth {depth}, {choices[family].Title.ToLowerInvariant()}. Ctrl+Z undoes it."
        );
    }
}
