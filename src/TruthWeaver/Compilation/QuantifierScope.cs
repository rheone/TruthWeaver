namespace TruthWeaver.Compilation;

/// <summary>
/// The quantifier nesting that a library rule is checked in: how many quantifiers deep the place of the reference is, and
/// the variables those quantifiers bind. The check applies <see cref="CompilerOptions.MaxQuantifierDepth"/> and the
/// shadowed-variable rule to the library rule with this nesting, as if its text were written in place.
/// </summary>
/// <param name="Depth">How many quantifiers deep the place is: 0 for the rule itself.</param>
/// <param name="Variables">The variables of the enclosing quantifiers, innermost last: empty for the rule itself.</param>
internal sealed record QuantifierScope(int Depth, IReadOnlyList<string> Variables)
{
    /// <summary>Gets the nesting of the rule itself: no quantifier and no bound variable.</summary>
    public static QuantifierScope Root { get; } = new(0, []);

    /// <summary>Gets the nesting of the body of a quantifier that binds <paramref name="variable"/> inside this nesting.</summary>
    /// <param name="variable">The variable of the quantifier.</param>
    /// <returns>The nesting one quantifier deeper, with <paramref name="variable"/> bound.</returns>
    public QuantifierScope Enter(string variable)
    {
        return new QuantifierScope(this.Depth + 1, [.. this.Variables, variable]);
    }
}
