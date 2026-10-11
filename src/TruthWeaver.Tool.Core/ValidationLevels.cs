namespace TruthWeaver.Tool.Core;

/// <summary>
/// The levels of <see cref="RuleValidator.Validate"/>. Each level is one flag, so a caller switches levels on and off,
/// and the report has one entry for each level. <see cref="All"/> is the default.
/// </summary>
[Flags]
public enum ValidationLevels
{
    /// <summary>No level.</summary>
    None = 0,

    /// <summary>The syntax and compile diagnostics of the permissive compile, except the limits and the analysis.</summary>
    Compile = 1,

    /// <summary>The JSON schema of the rule tree, <c>rule-tree.schema.json</c>, for a JSON or YAML rule.</summary>
    TreeSchema = 2,

    /// <summary>The depth limit (<c>TRE0009</c>) and the node limit (<c>TRE0010</c>) of the settings.</summary>
    Limits = 4,

    /// <summary>The predicate names, the arguments and their kinds, from the predicate schema file of the settings.</summary>
    PredicateSchema = 8,

    /// <summary>The Strong K3 analysis (tautologies, contradictions, a skipped analysis) and the lints of the options.</summary>
    Analysis = 16,

    /// <summary>The conversion self-check: rule text, JSON and YAML each compile to the same tree with the same notes.</summary>
    ConversionCheck = 32,

    /// <summary>Every level.</summary>
    All = Compile | TreeSchema | Limits | PredicateSchema | Analysis | ConversionCheck,
}
