namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Ticket 24: <see cref="LiteralConversion.Guess"/>'s lenient-mode fallback branches — boolean and
/// decimal-form numbers, plus <c>GuessArray</c>'s empty-array branch. These are exercised directly
/// against the raw parse-tree shape rather than through <c>RuleCompiler</c>, since the integer-form
/// number branch is already covered end-to-end via the lenient-mode compiler tests.
/// </summary>
public sealed class LiteralConversionGuessTests
{
    private static readonly SourceSpan Span = new(0, 0);

    /// <summary>Guess on a boolean form returns the corresponding boolean value.</summary>
    [Fact]
    public void Guess_BooleanForm_ReturnsBooleanValue_Test()
    {
        RawLiteral raw = RawLiteral.OfBoolean(true, Span);

        LiteralValue value = LiteralConversion.Guess(raw);

        Assert.Equal(LiteralKind.Boolean, value.Kind);
        Assert.True(value.AsBoolean());
    }

    /// <summary>Guess on a number with a decimal point returns a decimal value.</summary>
    [Fact]
    public void Guess_NumberWithDecimalPoint_ReturnsDecimalValue_Test()
    {
        RawLiteral raw = RawLiteral.OfNumber("1.5", Span);

        LiteralValue value = LiteralConversion.Guess(raw);

        Assert.Equal(LiteralKind.Decimal, value.Kind);
        Assert.Equal(1.5m, value.AsDecimal());
    }

    /// <summary>Guess on an empty array form returns an empty string array.</summary>
    [Fact]
    public void Guess_EmptyArrayForm_ReturnsEmptyStringArray_Test()
    {
        RawLiteral raw = RawLiteral.OfArray([], Span);

        LiteralValue value = LiteralConversion.Guess(raw);

        Assert.Equal(LiteralKind.StringArray, value.Kind);
        Assert.Empty(value.AsArray());
    }
}
