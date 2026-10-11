namespace TruthWeaver.Testing;

using System.Diagnostics;
using System.Globalization;
using System.Text;

/// <summary>The result of a <see cref="RuleFuzzer.RunAsync{TContext}"/> run.</summary>
public sealed class RuleFuzzReport
{
    /// <summary>Initializes a new instance of the <see cref="RuleFuzzReport"/> class.</summary>
    /// <param name="seed">The seed of the run.</param>
    /// <param name="terms">The term text of each predicate that the run used.</param>
    /// <param name="skippedPredicates">The names of the predicates that the run could not use.</param>
    /// <param name="rulesGenerated">The number of generated rules.</param>
    /// <param name="rulesChecked">The number of generated rules that compiled and were checked.</param>
    /// <param name="failures">The failed checks, in the order of the run.</param>
    internal RuleFuzzReport(
        int seed,
        IReadOnlyList<string> terms,
        IReadOnlyList<string> skippedPredicates,
        int rulesGenerated,
        int rulesChecked,
        IReadOnlyList<RuleFuzzFailure> failures
    )
    {
        this.Seed = seed;
        this.Terms = terms;
        this.SkippedPredicates = skippedPredicates;
        this.RulesGenerated = rulesGenerated;
        this.RulesChecked = rulesChecked;
        this.Failures = failures;
    }

    /// <summary>Gets the seed of the run. The same seed, registry schemas and options give the same rules.</summary>
    public int Seed { get; }

    /// <summary>
    /// Gets the term text of each predicate that the run used, for example <c>hasRole(role: "text")</c>, ordered by
    /// predicate name.
    /// </summary>
    public IReadOnlyList<string> Terms { get; }

    /// <summary>
    /// Gets the names of the predicates that the run did not use, because a call with the generated arguments does not
    /// compile. For example, the argument validator of the predicate rejects the generated values.
    /// </summary>
    public IReadOnlyList<string> SkippedPredicates { get; }

    /// <summary>Gets the number of generated rules.</summary>
    public int RulesGenerated { get; }

    /// <summary>
    /// Gets the number of generated rules that compiled and were checked. A generated rule that the compiler rejects, for
    /// example for an out-of-range threshold, is not checked.
    /// </summary>
    public int RulesChecked { get; }

    /// <summary>Gets the failed checks, in the order of the run.</summary>
    public IReadOnlyList<RuleFuzzFailure> Failures { get; }

    /// <summary>Gets a value indicating whether no check failed.</summary>
    public bool Passed => this.Failures.Count == 0;

    /// <summary>Throws when a check failed, so that a test fails with the seed and the list of failures.</summary>
    /// <exception cref="RuleFuzzException">One or more checks failed. The message lists each failure on its own line.</exception>
    [StackTraceHidden]
    public void ShouldPass()
    {
        if (this.Passed)
        {
            return;
        }

        StringBuilder message = new(
            string.Create(
                CultureInfo.InvariantCulture,
                $"The rule fuzzer found {this.Failures.Count} failure(s) with seed {this.Seed}:"
            )
        );
        foreach (RuleFuzzFailure failure in this.Failures)
        {
            message.AppendLine().Append("- ").Append(failure);
        }

        throw new RuleFuzzException(message.ToString());
    }

    /// <summary>Returns the seed, the counts, the terms, the skipped predicates and each failure, one per line.</summary>
    /// <returns>The report text.</returns>
    public override string ToString()
    {
        StringBuilder text = new(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Rule fuzz report for seed {this.Seed}: {this.RulesChecked} of {this.RulesGenerated} rules checked, {this.Failures.Count} failure(s)."
            )
        );
        text.AppendLine().Append("Terms: ").AppendJoin(", ", this.Terms);
        if (this.SkippedPredicates.Count > 0)
        {
            text.AppendLine().Append("Skipped predicates: ").AppendJoin(", ", this.SkippedPredicates);
        }

        foreach (RuleFuzzFailure failure in this.Failures)
        {
            text.AppendLine().Append("- ").Append(failure);
        }

        return text.ToString();
    }
}
