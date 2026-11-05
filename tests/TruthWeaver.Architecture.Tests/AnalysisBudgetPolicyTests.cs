namespace TruthWeaver.Architecture.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// The term cap and the node-budget catch of the verdict analyses live once, in <c>AnalysisVerdicts</c>. A verdict type
/// that copies the cap check or the catch would drift from the shared Undecided wording.
/// </summary>
public sealed partial class AnalysisBudgetPolicyTests
{
    /// <summary>Files that keep their own budget handling on purpose, each with the reason.</summary>
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal)
    {
        ["AnalysisVerdicts.cs"] = "The budget module itself.",
        ["BddNodeBudgetExceededException.cs"] = "Declares the exception.",
        ["BddManager.cs"] = "Raises the exception when the node budget runs out.",
        ["Analyzer.cs"] = "Catches the exception to skip a lint check and carry on, and words its own diagnostics.",
        ["RuleSetAnalysis.cs"] = "Falls back to pairwise analysis on a budget error and caps the rule count too.",
        ["TruthTablePrinter.cs"] = "Swallows the exception and refuses the table with its own diagnostic.",
    };

    /// <summary>A budget-exception catch or the "configured cap" reason text appears only in the module and the allowlist.</summary>
    [Fact]
    public void ShippedSource_BudgetCatchAndCapText_StayInTheBudgetModuleOrAllowlist_Test()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root?.EnumerateFiles("*.slnx").Any() == false)
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        string[] offenders =
        [
            .. root.EnumerateDirectories("src")
                .SelectMany(folder => folder.EnumerateFiles("*.cs", SearchOption.AllDirectories))
                .Where(file =>
                    !IsBuildOutput(file.FullName)
                    && !Allowed.ContainsKey(file.Name)
                    && HoldsBudgetPolicy(File.ReadAllText(file.FullName))
                )
                .Select(file => Path.GetRelativePath(root.FullName, file.FullName)),
        ];

        string message =
            "Move the cap check and the BddNodeBudgetExceededException catch into AnalysisVerdicts.Run, or add a reasoned allowlist entry, for:\n"
            + string.Join('\n', offenders);
        Assert.True(offenders.Length == 0, message);
    }

    private static bool HoldsBudgetPolicy(string text)
    {
        // Comment lines (including XML docs) may name the type or the phrase; only code counts.
        string code = CommentLine().Replace(text, string.Empty);
        return BudgetPolicy().IsMatch(code);
    }

    [GeneratedRegex(@"^\s*//.*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex CommentLine();

    [GeneratedRegex(@"catch\s*\(\s*BddNodeBudgetExceededException|configured cap", RegexOptions.CultureInvariant)]
    private static partial Regex BudgetPolicy();

    private static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
