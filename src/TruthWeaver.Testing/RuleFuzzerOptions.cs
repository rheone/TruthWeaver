namespace TruthWeaver.Testing;

/// <summary>The size of a <see cref="RuleFuzzer"/> run.</summary>
public sealed record RuleFuzzerOptions
{
    /// <summary>Gets the default options: 200 rules, a maximum depth of 3 and at most 3 terms in a rule.</summary>
    public static RuleFuzzerOptions Default { get; } = new();

    /// <summary>Gets the number of rules to generate. The default is 200. The value must be one or more.</summary>
    public int RuleCount { get; init; } = 200;

    /// <summary>
    /// Gets the maximum nesting depth of a generated rule. The default is 3. Zero gives rules that are a single term or
    /// constant. The value must not be negative.
    /// </summary>
    public int MaxDepth { get; init; } = 3;

    /// <summary>
    /// Gets the maximum number of distinct predicates in one rule. The default is 3. The value must be one or more.
    /// </summary>
    /// <remarks>
    /// The fuzzer evaluates each rule for all <c>3^n</c> assignments of its <c>n</c> terms, so the run time grows by a
    /// factor of three for each added term.
    /// </remarks>
    public int MaxTerms { get; init; } = 3;
}
