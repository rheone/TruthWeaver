namespace TruthWeaver.Building;

/// <summary>
/// One named, optionally parameterized definition for <see cref="RuleBuilder.WithDefinitions(RuleDefinition[])"/>
/// (ADR-0008 decision 4): a reusable sub-expression that a <see cref="RuleBuilder.Use(string, RuleBuilder[])"/>
/// elsewhere in the same tree expands at compile time, the way a rule-text <c>let</c> declaration or a JSON/YAML
/// <c>definitions</c> entry already does. Create one with <see cref="RuleBuilder.Define(string, RuleBuilder)"/> or
/// <see cref="RuleBuilder.Define(string, IReadOnlyList{string}, RuleBuilder)"/>, not this record's constructor
/// directly, so a null argument is rejected consistently with the rest of <c>RuleBuilder</c>.
/// </summary>
/// <param name="Name">The definition's name, unique (case-insensitively) within the document it is attached to.</param>
/// <param name="Parameters">The definition's declared parameter names, in positional order; empty for a parameterless definition.</param>
/// <param name="Body">The definition's own expression.</param>
public sealed record RuleDefinition(string Name, IReadOnlyList<string> Parameters, RuleBuilder Body);
