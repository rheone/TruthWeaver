namespace TruthWeaver.Testing.RuleTests;

/// <summary>What one case of a rule test expects: the result, and optionally the fault count and the trace.</summary>
/// <param name="Result">The expected result of the rule.</param>
/// <param name="Faults">The expected number of faults, or <see langword="null"/> to not check them.</param>
/// <param name="Trace">
/// The expected trace tree, as <c>PrintPlainText(decision)</c> writes it, or <see langword="null"/> to not check it. The
/// comparison ignores line-ending style, spaces at the end of a line and empty lines at the end.
/// </param>
public sealed record RuleTestExpectation(TruthValue Result, int? Faults = null, string? Trace = null);
