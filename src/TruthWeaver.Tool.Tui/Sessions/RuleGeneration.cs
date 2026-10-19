namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Yaml;

/// <summary>
/// Makes a semi-random rule that compiles, for the Generate rule action. The terms are calls of predicates from the catalog
/// with sample arguments, and <see cref="K3RuleGenerator"/> combines them with the operators of the chosen families. The
/// same seed, depth and families give the same text.
/// </summary>
internal static class RuleGeneration
{
    /// <summary>The number of attempts before the generation gives up: some generated rules do not compile.</summary>
    public const int MaxAttempts = 50;

    /// <summary>The nesting depth that the dialog offers first.</summary>
    public const int DefaultDepth = 3;

    /// <summary>The largest depth that the dialog offers.</summary>
    public const int MaxDepth = 5;

    private static readonly string[] Words = ["admin", "editor", "viewer", "EU", "US", "active", "draft", "gold", "beta"];

    /// <summary>Gets the indexes into <see cref="FamilyChoices"/> that the dialog ticks first: the connectives only.</summary>
    public static IReadOnlyList<int> DefaultFamilyIndexes { get; } = [0];

    /// <summary>
    /// Gets the choices of the operator families dialog: the title and the families it stands for. The titles are the four
    /// groups of the Insert menu, in its order, so the menu is the one source. The Other group stands for <c>If</c>; the
    /// generator counts <c>COALESCE</c> with the Inspection group.
    /// </summary>
    public static IReadOnlyList<(string Title, RuleOperatorFamilies Families)> FamilyChoices { get; } =
    [.. InsertItems.Operators.Select(g => (g.Title, FamiliesOf(g.Title)))];

    /// <summary>Combines the families of the chosen entries of <see cref="FamilyChoices"/>.</summary>
    /// <param name="indexes">The indexes into <see cref="FamilyChoices"/>.</param>
    /// <returns>The combined families.</returns>
    public static RuleOperatorFamilies Combine(IEnumerable<int> indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);

        RuleOperatorFamilies families = RuleOperatorFamilies.None;
        foreach (int index in indexes)
        {
            families |= FamilyChoices[index].Families;
        }

        return families;
    }

    /// <summary>Makes a rule that compiles.</summary>
    /// <param name="seed">The seed of the random source.</param>
    /// <param name="depth">The maximum nesting depth. The value must not be negative.</param>
    /// <param name="families">The operator families the rule may use.</param>
    /// <param name="settings">The settings that the compile check uses.</param>
    /// <returns>The rule text, or <see langword="null"/> when none of <see cref="MaxAttempts"/> attempts compiles. It never throws for that.</returns>
    public static string? Generate(int seed, int depth, RuleOperatorFamilies families, ToolSettings settings)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        ArgumentNullException.ThrowIfNull(settings);

        Random random = new(seed);
        RuleCompiler<StubAssignment> compiler = RuleConverter.CreateCompiler(settings);
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            string[] terms = PickTerms(random, settings);
            if (terms.Length == 0)
            {
                return null;
            }

            string text = K3RuleGenerator.GenerateRule(random, depth, terms, families).Text;
            if (compiler.CompileAs(text, RuleFormat.Text).Rule is not null)
            {
                return text;
            }
        }

        return null;
    }

    private static RuleOperatorFamilies FamiliesOf(string group)
    {
        return group switch
        {
            "Connectives" => RuleOperatorFamilies.Connectives,
            "Cardinality" => RuleOperatorFamilies.Cardinality,
            "Inspection" => RuleOperatorFamilies.Inspection,
            _ => RuleOperatorFamilies.Conditional,
        };
    }

    private static string[] PickTerms(Random random, ToolSettings settings)
    {
        int wanted = random.Next(3, 6);
        List<string> terms = [];
        PaletteEntry[] order = [.. PredicateCatalog.All.OrderBy(_ => random.Next()).Select(p => p.Entry)];
        foreach (PaletteEntry entry in order)
        {
            if (terms.Count == wanted)
            {
                break;
            }

            string[] values = [.. entry.Arguments.Select(a => ValueOf(random, a))];
            ToolResult<string> text = RulePalette.Fill(entry, values, settings);
            if (text.Succeeded && !terms.Contains(text.Value))
            {
                terms.Add(text.Value);
            }
        }

        return [.. terms];
    }

    // A value that reads like real data where the kind is simple; anything else gets the placeholder of the Insert menu. An
    // optional argument is left out.
    private static string ValueOf(Random random, PaletteArgument argument)
    {
        if (!argument.Required)
        {
            return string.Empty;
        }

        return argument.Kind switch
        {
            LiteralKind.String => Words[random.Next(Words.Length)],
            LiteralKind.Int64 => random.Next(1, 100).ToString(System.Globalization.CultureInfo.InvariantCulture),
            LiteralKind.Boolean => random.Next(2) == 0 ? "true" : "false",
            _ => InsertItems.Placeholder(argument.Kind),
        };
    }
}
