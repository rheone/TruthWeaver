namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Json;

/// <summary>
/// The constant readers leave their <c>out</c> value at <see cref="TruthValue.Unknown"/> when the text is not a
/// constant, so a caller that ignores the return value never reads a definite answer.
/// </summary>
public sealed class TruthValueTextTests
{
    /// <summary><c>TryParse</c> fails for text that is not a K3 word and sets the value to <c>Unknown</c>.</summary>
    /// <param name="text">Text that is not <c>true</c>, <c>false</c> or <c>unknown</c>.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("maybe")]
    [InlineData("0")]
    [InlineData("T")]
    public void TryParse_UnrecognisedText_ReturnsFalseWithUnknown_Test(string? text)
    {
        bool parsed = TruthValueText.TryParse(text, out TruthValue value);

        Assert.False(parsed);
        Assert.Equal(TruthValue.Unknown, value);
    }

    /// <summary><c>TryParseLetter</c> fails for text that is not <c>T</c>, <c>F</c> or <c>U</c> and sets the value to <c>Unknown</c>.</summary>
    /// <param name="text">Text that is not one of the three letters.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("0")]
    [InlineData("true")]
    public void TryParseLetter_UnrecognisedText_ReturnsFalseWithUnknown_Test(string? text)
    {
        bool parsed = TruthValueText.TryParseLetter(text, out TruthValue value);

        Assert.False(parsed);
        Assert.Equal(TruthValue.Unknown, value);
    }

    /// <summary>A JSON node that is not a constant (a number, <c>null</c>, an object) is read as no constant, with the value <c>Unknown</c>.</summary>
    /// <param name="json">A JSON value that is not a K3 constant.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("\"maybe\"")]
    public void TryGetTruthValue_JsonNonConstant_ReturnsFalseWithUnknown_Test(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonNodeCursor cursor = new(document.RootElement);

        bool read = cursor.TryGetTruthValue(out TruthValue value);

        Assert.False(read);
        Assert.Equal(TruthValue.Unknown, value);
    }
}
