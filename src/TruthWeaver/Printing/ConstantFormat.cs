namespace TruthWeaver.Printing;

using TruthWeaver.Ast;

/// <summary>
/// How a printer writes a constant: a <see cref="ConstantStyle"/>, plus an optional override string for each of
/// <c>True</c>, <c>False</c> and <c>Unknown</c>. An override wins over the style for that one constant.
/// </summary>
/// <remarks>
/// <see cref="Evaluation.CompiledRule{TContext}.PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/> accepts only a
/// format that the rule text parser reads back. Equation, plain-text and Mermaid printers accept any format. An override
/// is literal text: each dialect escapes it, so it cannot break the output.
/// </remarks>
public sealed record ConstantFormat
{
    /// <summary>Gets the default format: <see cref="ConstantStyle.Word"/> with no overrides.</summary>
    public static ConstantFormat Default { get; } = new();

    /// <summary>Gets the style for every constant that has no override. The default is <see cref="ConstantStyle.Word"/>.</summary>
    public ConstantStyle Style { get; init; } = ConstantStyle.Word;

    /// <summary>Gets the text for <c>True</c> that wins over <see cref="Style"/>, or <see langword="null"/> (the default) for none.</summary>
    /// <exception cref="ArgumentException">The value is empty or has a line break.</exception>
    public string? TrueText
    {
        get;
        init => field = Validate(value, nameof(this.TrueText));
    }

    /// <summary>Gets the text for <c>False</c> that wins over <see cref="Style"/>, or <see langword="null"/> (the default) for none.</summary>
    /// <exception cref="ArgumentException">The value is empty or has a line break.</exception>
    public string? FalseText
    {
        get;
        init => field = Validate(value, nameof(this.FalseText));
    }

    /// <summary>Gets the text for <c>Unknown</c> that wins over <see cref="Style"/>, or <see langword="null"/> (the default) for none.</summary>
    /// <exception cref="ArgumentException">The value is empty or has a line break.</exception>
    public string? UnknownText
    {
        get;
        init => field = Validate(value, nameof(this.UnknownText));
    }

    /// <summary>Gets the plain text of a constant: its override if it has one, otherwise the text of <see cref="Style"/>.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns>The unescaped text.</returns>
    internal string Text(TruthValue value)
    {
        return this.OverrideFor(value)
            ?? this.Style switch
            {
                ConstantStyle.Word => TruthValueText.Canonical(value),
                ConstantStyle.Letters => value switch
                {
                    TruthValue.True => "T",
                    TruthValue.False => "F",
                    TruthValue.Unknown => "U",
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unhandled truth value."),
                },
                ConstantStyle.LogicSymbols => value switch
                {
                    TruthValue.True => "⊤",
                    TruthValue.False => "⊥",
                    TruthValue.Unknown => "U",
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unhandled truth value."),
                },
                _ => throw new InvalidOperationException($"Unhandled constant style '{this.Style}'."),
            };
    }

    /// <summary>
    /// Gets whether the constant prints as the logic symbol <c>⊤</c> or <c>⊥</c> (the style is
    /// <see cref="ConstantStyle.LogicSymbols"/> and the constant has no override), so a dialect can write its own
    /// command for the symbol instead of escaping the character.
    /// </summary>
    internal bool IsLogicSymbol(TruthValue value)
    {
        return this.Style == ConstantStyle.LogicSymbols && value != TruthValue.Unknown && this.OverrideFor(value) is null;
    }

    /// <summary>Picks the dialect's command for <c>⊤</c> (for a true value) or <c>⊥</c> (for a false value).</summary>
    /// <param name="value">The constant's value, which is not <see cref="TruthValue.Unknown"/>.</param>
    /// <param name="top">The dialect's command for <c>⊤</c>.</param>
    /// <param name="bottom">The dialect's command for <c>⊥</c>.</param>
    /// <returns><paramref name="top"/> for <see cref="TruthValue.True"/>, otherwise <paramref name="bottom"/>.</returns>
    internal static string SymbolCommand(TruthValue value, string top, string bottom)
    {
        return value == TruthValue.True ? top : bottom;
    }

    /// <summary>
    /// Checks that the rule text parser reads this format back to the same constants.
    /// </summary>
    /// <param name="reason">The reason when the format is not read back, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when printed rule text with this format re-parses to an equal tree.</returns>
    internal bool IsRuleTextSafe(out string? reason)
    {
        if (this.Style != ConstantStyle.Word)
        {
            reason =
                $"ConstantStyle.{this.Style} is not rule text: the parser reads only True, False and Unknown, so "
                + "a bare letter or symbol would parse as a term name or fail. Use ConstantStyle.Word.";
            return false;
        }

        foreach (TruthValue value in (TruthValue[])[TruthValue.True, TruthValue.False, TruthValue.Unknown])
        {
            if (this.OverrideFor(value) is { } text && !(TruthValueText.TryParse(text, out TruthValue read) && read == value))
            {
                reason =
                    $"The override '{text}' for {TruthValueText.Canonical(value)} is not rule text: the parser would not "
                    + $"read it back as {TruthValueText.Canonical(value)}. Only a letter-case variant of the word is read back.";
                return false;
            }
        }

        reason = null;
        return true;
    }

    private string? OverrideFor(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => this.TrueText,
            TruthValue.False => this.FalseText,
            _ => this.UnknownText,
        };
    }

    private static string? Validate(string? value, string propertyName)
    {
        if (value is not null && (value.Length == 0 || value.AsSpan().ContainsAny('\r', '\n')))
        {
            throw new ArgumentException("A constant override must not be empty or contain a line break.", propertyName);
        }

        return value;
    }
}
