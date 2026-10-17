namespace TruthWeaver.Extensions.Configuration;

using TruthWeaver.Diagnostics;

/// <summary>What one load or reload of the configuration did to one named rule.</summary>
/// <param name="Name">The rule name: the key of the rule in the configuration section.</param>
/// <param name="Outcome">The result for this rule.</param>
/// <param name="Diagnostics">The compiler diagnostics. They are empty unless <paramref name="Outcome"/> is <see cref="RuleReloadOutcome.Failed"/> or the rule compiled with notes.</param>
public sealed record RuleReloadReport(string Name, RuleReloadOutcome Outcome, EquatableArray<Diagnostic> Diagnostics);
