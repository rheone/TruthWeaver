namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// <see cref="LiteralConversion.TryGuess"/> in lenient mode: the boolean and
/// decimal-form number results, and the empty-array result. These are exercised directly
/// against the raw parse-tree shape rather than through <c>RuleCompiler</c>, since the integer-form
/// number branch is already covered end-to-end via the lenient-mode compiler tests.
/// </summary>
public sealed class LiteralConversionGuessTests
{
    private static readonly SourceSpan Span = new(0, 0);

    /// <summary>TryGuess on a boolean form returns the corresponding boolean value.</summary>
    [Fact]
    public void TryGuess_BooleanForm_ReturnsBooleanValue_Test()
    {
        RawLiteral raw = RawLiteral.OfBoolean(true, Span);

        Assert.True(LiteralConversion.TryGuess(raw, out LiteralValue value, out _));

        Assert.Equal(LiteralKind.Boolean, value.Kind);
        Assert.True(value.AsBoolean());
    }

    /// <summary>TryGuess on a number with a decimal point returns a decimal value.</summary>
    [Fact]
    public void TryGuess_NumberWithDecimalPoint_ReturnsDecimalValue_Test()
    {
        RawLiteral raw = RawLiteral.OfNumber("1.5", Span);

        Assert.True(LiteralConversion.TryGuess(raw, out LiteralValue value, out _));

        Assert.Equal(LiteralKind.Decimal, value.Kind);
        Assert.Equal(1.5m, value.AsDecimal());
    }

    /// <summary>TryGuess on an empty array form returns an empty string array.</summary>
    [Fact]
    public void TryGuess_EmptyArrayForm_ReturnsEmptyStringArray_Test()
    {
        RawLiteral raw = RawLiteral.OfArray([], Span);

        Assert.True(LiteralConversion.TryGuess(raw, out LiteralValue value, out _));

        Assert.Equal(LiteralKind.StringArray, value.Kind);
        Assert.Empty(value.AsArray());
    }
}
