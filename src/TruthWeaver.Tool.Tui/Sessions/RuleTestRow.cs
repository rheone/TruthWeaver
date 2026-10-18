namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Tool.Core.RuleCases;

/// <summary>One case of the open rule test file in the Tests pane, with the outcome of the last run.</summary>
/// <param name="Name">The name of the case.</param>
/// <param name="Spec">The assignment and the expectation of the case, to load into the Assign pane.</param>
/// <param name="Passed"><see langword="null"/> before the file ran; otherwise whether the case passed.</param>
/// <param name="Actual">The result and the number of faults of the last run, or <see langword="null"/> when the case did not run or the rule did not compile.</param>
/// <param name="Failures">The reasons the case failed, in the words of the runner. Empty for a case that passed or did not run.</param>
internal sealed record RuleTestRow(
    string Name,
    RuleTestCaseSpec Spec,
    bool? Passed,
    (TruthValue Result, int Faults)? Actual,
    IReadOnlyList<string> Failures
)
{
    /// <summary>Gets the line of the row in the list: the status, the name and the expected and actual values.</summary>
    public string Line
    {
        get
        {
            string expected = this.Spec.ExpectedFaults is { } faults
                ? $"{this.Spec.ExpectedResult}, {faults} fault(s)"
                : this.Spec.ExpectedResult.ToString();
            if (this.Passed is null)
            {
                return $"----  {this.Name}  (expects {expected})";
            }

            if (this.Passed == true)
            {
                return $"PASS  {this.Name}  ({expected})";
            }

            string actual = this.Actual is { } run ? $"{run.Result}, {run.Faults} fault(s)" : "the rule does not compile";
            return $"FAIL  {this.Name}  expected {expected}; got {actual}";
        }
    }
}
