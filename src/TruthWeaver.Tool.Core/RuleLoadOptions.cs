namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;

/// <summary>The options for <see cref="RuleLoader"/>.</summary>
public sealed record RuleLoadOptions
{
    /// <summary>Gets the default options: detect the format, and read the whole input as one rule.</summary>
    public static RuleLoadOptions Default { get; } = new();

    /// <summary>
    /// Gets the format of the input, or <see langword="null"/> (the default) to detect it with <see cref="FormatDetector"/>.
    /// With <see cref="Path"/>, this is the format of the document, not of the rule inside it.
    /// </summary>
    public RuleFormat? Format { get; init; }

    /// <summary>
    /// Gets the path of a rule inside a JSON or YAML document, or <see langword="null"/> (the default) when the whole input
    /// is the rule. The path is a singular JSONPath-style query such as <c>$.rules.admin</c>, <c>rules[0]</c> or
    /// <c>$['my rule']</c>. It selects a JSON object or a YAML mapping (a rule in the document's format) or a string (rule
    /// text).
    /// </summary>
    public string? Path { get; init; }
}
