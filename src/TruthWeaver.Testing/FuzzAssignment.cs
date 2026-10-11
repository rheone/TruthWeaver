namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>
/// The evaluation context of a <see cref="RuleFuzzer"/> rule: one truth value for each predicate in the rule. The stand-in
/// predicates of the fuzzer read their answer from it, so the real predicates never run.
/// </summary>
/// <param name="values">The value of each predicate, by registered name.</param>
internal sealed class FuzzAssignment(IReadOnlyDictionary<string, TruthValue> values)
{
    private readonly IReadOnlyDictionary<string, TruthValue> values = values;

    /// <summary>Gets the value assigned to <paramref name="predicateName"/>.</summary>
    /// <param name="predicateName">The registered predicate name.</param>
    /// <returns>The assigned value, or <see cref="TruthValue.Unknown"/> for a predicate that the rule does not contain.</returns>
    public TruthValue ValueOf(string predicateName)
    {
        return this.values.GetValueOrDefault(predicateName, TruthValue.Unknown);
    }
}
