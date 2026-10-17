namespace TruthWeaver.AspNetCore;

using TruthWeaver.Evaluation;

/// <summary>The options of one rule requirement: its name, whether it allows <c>Unknown</c>, and the evaluation options.</summary>
public sealed record RuleRequirementOptions
{
    /// <summary>Gets the default options: no name, <c>Unknown</c> denies, and the default evaluation options.</summary>
    public static RuleRequirementOptions Default { get; } = new();

    /// <summary>
    /// Gets the name of the requirement for logs and problem-details responses, or <see langword="null"/> for the policy
    /// name (with <c>AddTruthWeaverPolicy</c>) or <see cref="RuleRequirement.DefaultName"/> (with <c>RequireRule</c>). A
    /// problem-details response shows the name to the client.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether a rule result of <see cref="TruthValue.Unknown"/> satisfies the requirement. The
    /// default <see langword="false"/> denies it. When it is <see langword="true"/>, an <c>Unknown</c> result that a fault
    /// caused also satisfies the requirement. <see cref="TruthValue.False"/> always denies.
    /// </summary>
    public bool AllowUnknown { get; init; }

    /// <summary>
    /// Gets the evaluation options of each check (fault budget, mode, timeout), or <see langword="null"/> for
    /// <see cref="EvaluationOptions.Default"/>.
    /// </summary>
    public EvaluationOptions? Evaluation { get; init; }
}
