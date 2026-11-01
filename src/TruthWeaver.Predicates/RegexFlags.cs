namespace TruthWeaver.Predicates;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

/// <summary>
/// Reads the <c>flags</c> rule argument of <see cref="RegexPredicates"/>: a string of single letters, each naming one
/// <see cref="RegexOptions"/> value.
/// </summary>
internal static class RegexFlags
{
    /// <summary>The letters that <see cref="TryParse"/> accepts, in the order the documentation lists them.</summary>
    internal const string Letters = "imsnx";

    /// <summary>
    /// The options every rule-supplied pattern is compiled with, whatever the flags are. A pattern can turn on
    /// case-insensitivity inline with <c>(?i)</c>, so <see cref="RegexOptions.CultureInvariant"/> is set even without the
    /// <c>i</c> flag. Without it the pattern would take the casing rules of the thread that compiles it first, and the
    /// process-wide pattern cache would keep that answer for every later evaluation.
    /// </summary>
    internal const RegexOptions Always = RegexOptions.CultureInvariant;

    /// <summary>
    /// Converts a flags string to <see cref="RegexOptions"/>. The letters are <c>i</c> (<see cref="RegexOptions.IgnoreCase"/>),
    /// <c>m</c> (<see cref="RegexOptions.Multiline"/>), <c>s</c> (<see cref="RegexOptions.Singleline"/>), <c>n</c>
    /// (<see cref="RegexOptions.ExplicitCapture"/>) and <c>x</c> (<see cref="RegexOptions.IgnorePatternWhitespace"/>).
    /// Letters are case-sensitive. The result always includes <see cref="Always"/>, so an empty string gives only
    /// <see cref="Always"/>.
    /// </summary>
    /// <param name="flags">The flags string from the rule.</param>
    /// <param name="options">The options the letters name, with <see cref="Always"/>. <see cref="RegexOptions.None"/> when parsing fails.</param>
    /// <param name="error">A short description of the first problem, or <see langword="null"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> when every letter is known and none repeats.</returns>
    internal static bool TryParse(string flags, out RegexOptions options, [NotNullWhen(false)] out string? error)
    {
        options = RegexOptions.None;
        error = null;
        RegexOptions parsed = Always;
        foreach (char letter in flags)
        {
            RegexOptions option = letter switch
            {
                'i' => RegexOptions.IgnoreCase,
                'm' => RegexOptions.Multiline,
                's' => RegexOptions.Singleline,
                'n' => RegexOptions.ExplicitCapture,
                'x' => RegexOptions.IgnorePatternWhitespace,
                _ => RegexOptions.None,
            };

            if (option == RegexOptions.None)
            {
                error = $"'{letter}' is not a regular-expression flag. The flags are {Letters}.";
                return false;
            }

            if (flags.IndexOf(letter, StringComparison.Ordinal) != flags.LastIndexOf(letter))
            {
                error = $"The flag '{letter}' appears more than once.";
                return false;
            }

            parsed |= option;
        }

        options = parsed;
        return true;
    }
}
