namespace TruthWeaver.Ast;

/// <summary>
/// The one place that spells a <see cref="QuantifierKind"/>: its canonical DSL word and its symbolic glyph. The
/// evaluator, the collection binding, the trace text, the outline and the printers all read these, so a trace entry
/// and the text the coverage accumulator compares it against can never disagree.
/// </summary>
internal static class QuantifierText
{
    /// <summary>Gets the canonical DSL word for <paramref name="kind"/>: <c>FORALL</c> or <c>EXISTS</c> (ADR-0010 decision 2).</summary>
    /// <param name="kind">The quantifier kind.</param>
    /// <returns>The canonical word.</returns>
    public static string Label(QuantifierKind kind)
    {
        return kind switch
        {
            QuantifierKind.ForAll => "FORALL",
            QuantifierKind.Exists => "EXISTS",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown quantifier kind."),
        };
    }

    /// <summary>Gets the lowercase tree-format word (the JSON and YAML <c>op</c> value) for <paramref name="kind"/>.</summary>
    /// <param name="kind">The quantifier kind.</param>
    /// <returns>The word: <c>forall</c> or <c>exists</c>.</returns>
    public static string TreeFormatWord(QuantifierKind kind)
    {
        return kind switch
        {
            QuantifierKind.ForAll => "forall",
            QuantifierKind.Exists => "exists",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown quantifier kind."),
        };
    }

    /// <summary>Gets the operator description shown for a quantifier node of <paramref name="kind"/>.</summary>
    /// <param name="kind">The quantifier kind.</param>
    /// <returns>The description.</returns>
    public static string Description(QuantifierKind kind)
    {
        return kind switch
        {
            QuantifierKind.ForAll =>
                "Universal quantifier: the generalized AND over a registered collection binding's elements.",
            QuantifierKind.Exists =>
                "Existential quantifier: the generalized OR over a registered collection binding's elements.",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown quantifier kind."),
        };
    }

    /// <summary>Gets the symbolic glyph for <paramref name="kind"/>: <c>∀</c> or <c>∃</c>.</summary>
    /// <param name="kind">The quantifier kind.</param>
    /// <returns>The glyph.</returns>
    public static string Glyph(QuantifierKind kind)
    {
        return kind switch
        {
            QuantifierKind.ForAll => "∀",
            QuantifierKind.Exists => "∃",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown quantifier kind."),
        };
    }

    /// <summary>Gets the symbolic glyph for an outline label, which is the canonical word of a quantifier.</summary>
    /// <param name="label">The quantifier's <see cref="Label(QuantifierKind)"/> word.</param>
    /// <returns>The glyph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="label"/> is not the word of a quantifier.</exception>
    public static string Glyph(string label)
    {
        QuantifierKind? match = Enum.GetValues<QuantifierKind>()
            .Select(kind => (QuantifierKind?)kind)
            .FirstOrDefault(kind => label == Label(kind!.Value));
        return match is { } found
            ? Glyph(found)
            : throw new ArgumentOutOfRangeException(nameof(label), label, "Not the word of a quantifier.");
    }
}
