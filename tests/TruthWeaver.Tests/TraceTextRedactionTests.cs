namespace TruthWeaver.Tests;

using TruthWeaver.Json;

/// <summary>
/// <see cref="TraceTextRedaction"/>: the trace text of a term loses its literal argument values for a decision record
/// written without them, and every other text stays as it is.
/// </summary>
public sealed class TraceTextRedactionTests
{
    /// <summary>Each literal value of a term text becomes the placeholder, whatever its kind or the characters inside it.</summary>
    [Theory]
    [InlineData("lookup(id: 42)", "lookup(id: <omitted>)")]
    [InlineData("p(a: \"x, y)\", b: -1.50)", "p(a: <omitted>, b: <omitted>)")]
    [InlineData("p(a: \"say \\\"hi\\\" \\\\\")", "p(a: <omitted>)")]
    [InlineData("p(list: [\"a]\", \"b\"], on: true)", "p(list: <omitted>, on: <omitted>)")]
    [InlineData(
        "p(at: \"2025-01-02T03:04:05.0000000+02:00\", id: \"3fa85f64-5717-4562-b3fc-2c963f66afa6\")",
        "p(at: <omitted>, id: <omitted>)"
    )]
    public void Redact_TermWithLiteralArguments_ReplacesEachValue_Test(string text, string expected)
    {
        string redacted = TraceTextRedaction.Redact(text);

        Assert.Equal(expected, redacted);
    }

    /// <summary>A variable reference, the value a data source resolved for it and a no-match note are kept; only the literal goes.</summary>
    [Fact]
    public void Redact_TermWithVariables_KeepsTheReferencesResolvedValuesAndNotes_Test()
    {
        const string Text =
            "p(min: from(\"user\", \"$.a, b\") = 18, role: \"admin\", tags: from(\"user\", \"$.t\")) [no match for from(\"user\", \"$.t\")]";

        string redacted = TraceTextRedaction.Redact(Text);

        Assert.Equal(
            "p(min: from(\"user\", \"$.a, b\") = 18, role: <omitted>, tags: from(\"user\", \"$.t\")) [no match for from(\"user\", \"$.t\")]",
            redacted
        );
    }

    /// <summary>An operator label, a constant and a term without arguments hold no literal argument value and stay as they are.</summary>
    [Theory]
    [InlineData("AND")]
    [InlineData("Exactly(2)")]
    [InlineData("BETWEEN(1, 3)")]
    [InlineData("True")]
    [InlineData("isAdmin")]
    public void Redact_TextWithoutLiteralArguments_IsUnchanged_Test(string text)
    {
        string redacted = TraceTextRedaction.Redact(text);

        Assert.Same(text, redacted);
    }

    /// <summary>A redacted text redacts to itself, so a record read back and written again keeps the same trace texts.</summary>
    [Fact]
    public void Redact_RedactedText_IsUnchanged_Test()
    {
        string once = TraceTextRedaction.Redact("p(a: 1, b: from(\"s\", \"q\"))");

        string twice = TraceTextRedaction.Redact(once);

        Assert.Equal(once, twice);
    }
}
