namespace TruthWeaver.Analysis;

using System.Collections;
using TruthWeaver.Evaluation;

/// <summary>
/// An immutable, ordered list of compiled rules that <see cref="RuleSetAnalysis"/> analyzes as one group. A rule is
/// identified by its index in the list, because TruthWeaver has no rule names: a name or version around a rule belongs
/// to the application, which maps the indexes back to its own keys.
/// </summary>
/// <remarks>
/// The set has no combining behavior: it does not say how the rules combine into one answer, and it does not evaluate
/// anything. To combine rules into one decision, use a policy set from the <c>TruthWeaver.Policies</c> package.
/// </remarks>
/// <typeparam name="TContext">The application context type every rule was compiled for.</typeparam>
public sealed class RuleSet<TContext> : IReadOnlyList<CompiledRule<TContext>>
{
    private readonly CompiledRule<TContext>[] rules;

    /// <summary>Initializes a new instance of the <see cref="RuleSet{TContext}"/> class with a copy of <paramref name="rules"/>, in order.</summary>
    /// <param name="rules">The rules. The set keeps its own copy, so a later change to the source does not change the set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is <see langword="null"/>, or contains a <see langword="null"/> rule.</exception>
    public RuleSet(IEnumerable<CompiledRule<TContext>> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        this.rules = [.. rules];
        if (Array.IndexOf(this.rules, null) is int nullIndex and >= 0)
        {
            throw new ArgumentNullException(nameof(rules), $"The rule at index {nullIndex} is null.");
        }
    }

    /// <summary>Gets the number of rules in the set.</summary>
    public int Count => this.rules.Length;

    /// <summary>Gets the rule at <paramref name="index"/>.</summary>
    /// <param name="index">The index of the rule, which is also its identity in a <see cref="RuleSetAnalysisResult"/>.</param>
    /// <returns>The rule.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is not a valid index.</exception>
    public CompiledRule<TContext> this[int index] => this.rules[index];

    /// <summary>Returns an enumerator over the rules, in index order.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<CompiledRule<TContext>> GetEnumerator()
    {
        return ((IEnumerable<CompiledRule<TContext>>)this.rules).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
