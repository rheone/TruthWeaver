namespace TruthWeaver.Abstractions;

/// <summary>
/// The identity of a term — a predicate name bound to concrete arguments — per CONTEXT.md's
/// term-identity rule. Two terms are the same identity if and only if their predicate names are
/// equal (already normalized to the registry's casing by the time a <see cref="TermIdentity"/> is
/// constructed) and their arguments, sorted by name, are pairwise equal by exact type-normalized
/// value; a <see cref="VariableReference"/> argument compares by its source name and query text, never by
/// the value it resolves to (ADR-0006 decision 11). This is the key used for per-evaluation memoization and
/// for the analyzer's structural equality checks.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="TermIdentity"/> class.</remarks>
/// <param name="predicateName">The predicate name, normalized to the registered casing.</param>
/// <param name="arguments">The literal arguments; sorted by name (ordinal) on construction.</param>
/// <param name="variables">The arguments given as variable references instead of literals, or <see langword="null"/> for none; sorted by name (ordinal) on construction.</param>
public sealed class TermIdentity(
    string predicateName,
    IReadOnlyList<KeyValuePair<string, LiteralValue>> arguments,
    IReadOnlyList<KeyValuePair<string, VariableReference>>? variables = null
) : IEquatable<TermIdentity>
{
    /// <summary>Gets the predicate name, normalized to the registered casing.</summary>
    public string PredicateName { get; } = predicateName;

    /// <summary>Gets the literal arguments, sorted by name. Arguments given as variable references are in <see cref="Variables"/>.</summary>
    public EquatableArray<KeyValuePair<string, LiteralValue>> Arguments { get; } =
        new EquatableArray<KeyValuePair<string, LiteralValue>>(arguments.OrderBy(a => a.Key, StringComparer.Ordinal));

    /// <summary>Gets the arguments given as variable references, sorted by name; empty for a term with only literal arguments.</summary>
    public EquatableArray<KeyValuePair<string, VariableReference>> Variables { get; } =
        new EquatableArray<KeyValuePair<string, VariableReference>>(
            (variables ?? []).OrderBy(a => a.Key, StringComparer.Ordinal)
        );

    /// <summary>Determines whether two term identities are equal.</summary>
    /// <param name="left">The left identity.</param>
    /// <param name="right">The right identity.</param>
    /// <returns><see langword="true"/> if the identities are equal.</returns>
    public static bool operator ==(TermIdentity? left, TermIdentity? right)
    {
        return Equals(left, right);
    }

    /// <summary>Determines whether two term identities are not equal.</summary>
    /// <param name="left">The left identity.</param>
    /// <param name="right">The right identity.</param>
    /// <returns><see langword="true"/> if the identities are not equal.</returns>
    public static bool operator !=(TermIdentity? left, TermIdentity? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public bool Equals(TermIdentity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(this.PredicateName, other.PredicateName, StringComparison.Ordinal)
            && this.Arguments.Equals(other.Arguments)
            && this.Variables.Equals(other.Variables);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as TermIdentity);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(this.PredicateName, this.Arguments, this.Variables);
    }

    /// <summary>
    /// Renders the literal and variable arguments together, sorted by name, as comma-joined <c>name: value</c> pairs
    /// (<c>min: from("user", "$.minAge"), role: "Y"</c>), without the predicate name.
    /// </summary>
    /// <returns>The joined argument text, or <see langword="null"/> for a term with no arguments.</returns>
    public string? FormatArguments()
    {
        if (this.Arguments.Count == 0 && this.Variables.Count == 0)
        {
            return null;
        }

        IEnumerable<KeyValuePair<string, string>> literals = this.Arguments.Select(a => new KeyValuePair<string, string>(
            a.Key,
            a.Value.ToString()
        ));
        IEnumerable<KeyValuePair<string, string>> references = this.Variables.Select(v => new KeyValuePair<string, string>(
            v.Key,
            v.Value.ToString()
        ));
        return string.Join(
            ", ",
            literals.Concat(references).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}: {a.Value}")
        );
    }

    /// <summary>Renders this term identity as it would appear in the canonical DSL (e.g. <c>hasRole(role: "Y")</c>).</summary>
    /// <returns>The canonical term text.</returns>
    public override string ToString()
    {
        return this.FormatArguments() is { } text ? $"{this.PredicateName}({text})" : this.PredicateName;
    }
}
