namespace TruthWeaver.Testing;

using System.Diagnostics;
using System.Globalization;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>Assertions over compiled rules, for tests that refactor or rewrite a rule.</summary>
[StackTraceHidden]
public static class RuleAssertions
{
    /// <summary>
    /// Asserts that two compiled rules are equivalent under Strong Kleene (K3) logic, using
    /// <see cref="RuleEquivalence.Compare{TContext}"/>.
    /// </summary>
    /// <typeparam name="TContext">The application context type both rules were compiled for.</typeparam>
    /// <param name="first">The first rule.</param>
    /// <param name="second">The second rule.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms, counted across both rules, the equivalence check decides. It controls only that cap. Raise
    /// it to compare rules with more terms. <see langword="null"/> uses the <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    /// <exception cref="DecisionAssertionException">
    /// The rules are not equivalent (the message shows a counter-example), or the comparison is inconclusive because
    /// the rules have more distinct terms than <paramref name="maxAnalysisTerms"/>. An unproven claim
    /// never passes.
    /// </exception>
    public static void AssertEquivalent<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        int? maxAnalysisTerms = null
    )
    {
        RuleEquivalenceResult result = RuleEquivalence.Compare(first, second, maxAnalysisTerms);
        switch (result.Outcome)
        {
            case RuleEquivalenceOutcome.Equivalent:
                return;
            case RuleEquivalenceOutcome.NotEquivalent:
                throw new DecisionAssertionException(
                    $"Expected the rules to be equivalent, but they are not equivalent. With {FormatCounterExample(result)}, they differ."
                );
            default:
                throw new DecisionAssertionException(
                    $"The equivalence check is inconclusive: {result.Reason} Raise the maxAnalysisTerms argument or shrink the rules."
                );
        }
    }

    /// <summary>Formats the counter-example of a not-equivalent result as <c>term = value</c> pairs.</summary>
    /// <param name="result">A result whose outcome is <see cref="RuleEquivalenceOutcome.NotEquivalent"/>.</param>
    /// <returns>The assignment text.</returns>
    internal static string FormatCounterExample(RuleEquivalenceResult result)
    {
        return string.Join(
            ", ",
            result.CounterExample!.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Key} = {p.Value}"))
        );
    }
}
