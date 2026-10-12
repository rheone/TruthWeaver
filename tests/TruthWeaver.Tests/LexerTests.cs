namespace TruthWeaver.Tests;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>Ticket 03: direct unit tests for the DSL tokenizer.</summary>
public sealed class LexerTests
{
    /// <summary>Every punctuation token has the correct kind and a one character span.</summary>
    [Theory]
    [InlineData("(", nameof(TokenKind.LParen))]
    [InlineData(")", nameof(TokenKind.RParen))]
    [InlineData("[", nameof(TokenKind.LBracket))]
    [InlineData("]", nameof(TokenKind.RBracket))]
    [InlineData("{", nameof(TokenKind.LBrace))]
    [InlineData("}", nameof(TokenKind.RBrace))]
    [InlineData(",", nameof(TokenKind.Comma))]
    [InlineData(":", nameof(TokenKind.Colon))]
    public void Punctuation_EveryToken_HasCorrectKindAndSpan_Test(string source, string expectedKindName)
    {
        Lexer lexer = new(source);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = tokens[0];
        Assert.Equal(expectedKindName, token.Kind.ToString());
        Assert.Equal(new SourceSpan(0, 1), token.Span);
        Assert.Equal(TokenKind.Eof, tokens[1].Kind);
    }

    /// <summary>A negative integer is read as a single number literal token.</summary>
    [Fact]
    public void Number_NegativeInteger_ReadsAsOneToken_Test()
    {
        Lexer lexer = new("-5");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.NumberLiteral, tokens[0].Kind);
        Assert.Equal("-5", tokens[0].Text);
        Assert.Empty(lexer.Diagnostics);
    }

    /// <summary>A bare minus not followed by a digit raises an unexpected character diagnostic.</summary>
    [Fact]
    public void Minus_NotFollowedByDigit_RaisesUnexpectedCharacter_Test()
    {
        Lexer lexer = new("-");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Eof, tokens[0].Kind);
        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unexpected character"));
    }

    /// <summary>A decimal number is read as one number literal token.</summary>
    [Fact]
    public void Number_Decimal_ReadsAsOneToken_Test()
    {
        Lexer lexer = new("1.5");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.NumberLiteral, tokens[0].Kind);
        Assert.Equal("1.5", tokens[0].Text);
    }

    /// <summary>A trailing dot with no following digit is not consumed into the number.</summary>
    [Fact]
    public void Number_TrailingDot_IsNotConsumed_Test()
    {
        Lexer lexer = new("1.");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.NumberLiteral, tokens[0].Kind);
        Assert.Equal("1", tokens[0].Text);
        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unexpected character"));
    }

    /// <summary>Each supported escape decodes to the correct character.</summary>
    [Theory]
    [InlineData("\"a\\\"b\"", "a\"b")]
    [InlineData("\"a\\\\b\"", "a\\b")]
    [InlineData("\"a\\nb\"", "a\nb")]
    [InlineData("\"a\\tb\"", "a\tb")]
    public void Escape_Supported_DecodesToCorrectCharacter_Test(string source, string expected)
    {
        Lexer lexer = new(source);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal(expected, tokens[0].Text);
        Assert.Empty(lexer.Diagnostics);
    }

    /// <summary>Each supported escape raises no invalid escape diagnostic.</summary>
    [Theory]
    [InlineData("\"a\\\"b\"")]
    [InlineData("\"a\\\\b\"")]
    [InlineData("\"a\\nb\"")]
    [InlineData("\"a\\tb\"")]
    public void Escape_Supported_RaisesNoDiagnostic_Test(string source)
    {
        Lexer lexer = new(source);

        lexer.Tokenize();

        Assert.DoesNotContain(lexer.Diagnostics, d => d.Code == DiagnosticCodes.InvalidEscapeSequence);
    }

    /// <summary>An unrecognized escape sequence raises a diagnostic that covers the backslash and the following character.</summary>
    [Fact]
    public void Escape_Unrecognized_RaisesDiagnosticCoveringBackslashAndCharacter_Test()
    {
        Lexer lexer = new("\"a\\pb\"");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Diagnostic diagnostic = Assert.Single(lexer.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidEscapeSequence, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(new SourceSpan(2, 2), diagnostic.Span);
    }

    /// <summary>An unrecognized escape sequence still produces a string token, so lexing recovers.</summary>
    [Fact]
    public void Escape_Unrecognized_StillProducesStringToken_Test()
    {
        Lexer lexer = new("\"a\\pb\" (");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal(TokenKind.LParen, tokens[1].Kind);
        Assert.Equal(TokenKind.Eof, tokens[2].Kind);
    }

    /// <summary>An unterminated string raises a diagnostic and still returns a string token.</summary>
    [Fact]
    public void String_Unterminated_RaisesDiagnosticAndReturnsToken_Test()
    {
        Lexer lexer = new("\"abc");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("abc", tokens[0].Text);
        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unterminated string literal"));
    }

    /// <summary>An unrecognized character raises a diagnostic and tokenization continues past it.</summary>
    [Fact]
    public void Character_Unrecognized_RaisesDiagnosticAndContinues_Test()
    {
        Lexer lexer = new("a # b");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unexpected character"));
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("a", tokens[0].Text);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal(TokenKind.Eof, tokens[^1].Kind);
    }

    /// <summary>Tokenizing empty source returns only the end of file token.</summary>
    [Fact]
    public void Tokenize_EmptySource_ReturnsOnlyEof_Test()
    {
        Lexer lexer = new(string.Empty);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = Assert.Single(tokens);
        Assert.Equal(TokenKind.Eof, token.Kind);
    }

    /// <summary>Tokenizing whitespace only source returns only the end of file token.</summary>
    [Fact]
    public void Tokenize_WhitespaceOnlySource_ReturnsOnlyEof_Test()
    {
        Lexer lexer = new("   \t\n  ");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = Assert.Single(tokens);
        Assert.Equal(TokenKind.Eof, token.Kind);
    }

    /// <summary>An identifier is read up to the first non identifier character.</summary>
    [Fact]
    public void Identifier_Reading_ReadsUpToNonIdentifierCharacter_Test()
    {
        Lexer lexer = new("abc_123(x)");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("abc_123", tokens[0].Text);
        Assert.Equal(TokenKind.LParen, tokens[1].Kind);
    }
}
