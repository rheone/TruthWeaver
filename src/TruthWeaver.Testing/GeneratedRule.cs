namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>A rule from <see cref="K3RuleGenerator"/>: its rule text, its expected evaluation, and its operands.</summary>
/// <param name="Text">The rule text. Each compound node is in parentheses.</param>
/// <param name="Eval">
/// The expected value for one assignment, built only from <see cref="K3Oracle"/>. Entry <c>i</c> of the assignment is
/// the value of term <c>i</c> of the term list given to <see cref="K3RuleGenerator.GenerateRule"/>.
/// </param>
/// <param name="Children">The operands, or empty for a term or a constant.</param>
public sealed record GeneratedRule(
    string Text,
    Func<IReadOnlyList<TruthValue>, TruthValue> Eval,
    IReadOnlyList<GeneratedRule> Children
)
{
    /// <summary>
    /// Finds whether the rule is <c>True</c> in every assignment (a tautology) or <c>False</c> in every assignment (a
    /// contradiction), over all <c>3^termCount</c> assignments.
    /// </summary>
    /// <param name="termCount">The number of terms in the term list given to the generator.</param>
    /// <returns>The two verdicts. Both are <see langword="false"/> for a rule whose value depends on the assignment.</returns>
    public (bool Tautology, bool Contradiction) Verdict(int termCount)
    {
        bool always = true;
        bool never = true;
        foreach (TruthValue[] assignment in K3Oracle.Assignments(termCount))
        {
            TruthValue value = this.Eval(assignment);
            always &= value == TruthValue.True;
            never &= value == TruthValue.False;
        }

        return (always, never);
    }
}
