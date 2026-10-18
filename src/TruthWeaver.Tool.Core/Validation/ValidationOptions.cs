namespace TruthWeaver.Tool.Core.Validation;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;

/// <summary>The options of <see cref="RuleValidator.Validate"/>.</summary>
public sealed record ValidationOptions
{
    /// <summary>Gets the defaults: every level, the logic and structure lints, and the default settings.</summary>
    public static ValidationOptions Default { get; } = new();

    /// <summary>Gets the levels to run. The default is <see cref="ValidationLevels.All"/>.</summary>
    public ValidationLevels Levels { get; init; } = ValidationLevels.All;

    /// <summary>
    /// Gets the lints that the analysis level runs. The default is <see cref="LintRules.All"/>, the logic and structure
    /// lints. Add <see cref="LintRules.Style"/> for the style lint.
    /// </summary>
    public LintRules Lints { get; init; } = LintRules.All;

    /// <summary>
    /// Gets the settings: the limits, and the path of the predicate schema file in <see cref="ToolSettings.SchemaFile"/>.
    /// The default is <see cref="ToolSettings.Default"/>.
    /// </summary>
    public ToolSettings Settings { get; init; } = ToolSettings.Default;

    /// <summary>
    /// Gets a predicate schema that is already read, or <see langword="null"/> (the default) to read the file that
    /// <see cref="ToolSettings.SchemaFile"/> names. With neither, the predicate schema level is skipped.
    /// </summary>
    public PredicateSchemaFile? PredicateSchema { get; init; }
}
