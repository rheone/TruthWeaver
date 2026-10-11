namespace TruthWeaver.Testing;

using System.Diagnostics;
using System.Text;

/// <summary>The result of a <see cref="PredicateHarness.RunAsync{TContext}"/> run: one outcome per check or check case.</summary>
public sealed class PredicateHarnessReport
{
    /// <summary>Initializes a new instance of the <see cref="PredicateHarnessReport"/> class.</summary>
    /// <param name="predicateName">The name of the predicate under test.</param>
    /// <param name="outcomes">The outcomes, in the order the harness produced them.</param>
    internal PredicateHarnessReport(string predicateName, IReadOnlyList<PredicateHarnessOutcome> outcomes)
    {
        this.PredicateName = predicateName;
        this.Outcomes = outcomes;
        this.Failures = [.. outcomes.Where(o => o.Status == PredicateHarnessStatus.Failed)];
    }

    /// <summary>Gets the name of the predicate under test, from its schema.</summary>
    public string PredicateName { get; }

    /// <summary>Gets every outcome, in the order the harness produced them.</summary>
    public IReadOnlyList<PredicateHarnessOutcome> Outcomes { get; }

    /// <summary>Gets the outcomes with status <see cref="PredicateHarnessStatus.Failed"/>.</summary>
    public IReadOnlyList<PredicateHarnessOutcome> Failures { get; }

    /// <summary>
    /// Gets a value indicating whether no check failed. An expected fault and an observed outcome do not make a run fail.
    /// </summary>
    public bool Passed => this.Failures.Count == 0;

    /// <summary>Throws when a check failed, so that a test fails with the list of failures.</summary>
    /// <exception cref="PredicateHarnessException">One or more checks failed. The message lists each failure on its own line.</exception>
    [StackTraceHidden]
    public void ShouldPass()
    {
        if (this.Passed)
        {
            return;
        }

        StringBuilder message = new();
        message.Append($"Predicate '{this.PredicateName}' failed {this.Failures.Count} harness check(s):");
        foreach (PredicateHarnessOutcome failure in this.Failures)
        {
            message.AppendLine().Append("- ").Append(failure);
        }

        throw new PredicateHarnessException(message.ToString());
    }

    /// <summary>Returns every outcome, one per line, after a line with the predicate name.</summary>
    /// <returns>The report text.</returns>
    public override string ToString()
    {
        StringBuilder text = new($"Predicate harness report for '{this.PredicateName}':");
        foreach (PredicateHarnessOutcome outcome in this.Outcomes)
        {
            text.AppendLine().Append("- ").Append(outcome);
        }

        return text.ToString();
    }
}
