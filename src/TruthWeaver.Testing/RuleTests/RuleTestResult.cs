namespace TruthWeaver.Testing.RuleTests;

/// <summary>The outcome of one case of a rule test file.</summary>
/// <param name="Case">The case.</param>
/// <param name="Decision">The decision of the rule for the case, or <see langword="null"/> when the rule does not compile.</param>
/// <param name="Trace">The trace tree of the decision, as <c>PrintPlainText(decision)</c> writes it, or <see langword="null"/> when the rule does not compile.</param>
/// <param name="Failures">Each reason the case failed; empty when it passed.</param>
/// <param name="RuleCompiles"><see langword="false"/> when the case failed because the rule does not compile.</param>
public sealed record RuleTestResult(
    RuleTestCase Case,
    Decision? Decision,
    string? Trace,
    IReadOnlyList<string> Failures,
    bool RuleCompiles
)
{
    /// <summary>Gets a value indicating whether the case passed: the rule compiles and every expectation holds.</summary>
    public bool Passed => this.Failures.Count == 0;

    /// <summary>Returns <c>PASS</c> or <c>FAIL</c> and the case name, then each failure reason indented on its own lines.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        StringBuilder text = new(this.Passed ? "PASS " : "FAIL ");
        text.Append(this.Case.Name);
        foreach (string failure in this.Failures)
        {
            foreach (string line in failure.Split('\n'))
            {
                text.Append('\n').Append("  ").Append(line);
            }
        }

        return text.ToString();
    }
}
