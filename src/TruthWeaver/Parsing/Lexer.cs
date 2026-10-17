namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// Hand-written tokenizer for the word-operator DSL, which also accepts symbolic operator aliases (ADR-0005; ADR-0003 notes this is a hand-written
/// recursive-descent parser, not Roslyn-based — there is no C# involved in rule text).
/// </summary>
internal sealed class Lexer(string source)
{
    private readonly string source = source;
    private int position;

    /// <summary>Gets the diagnostics raised while lexing (unterminated strings, invalid characters).</summary>
    public List<Diagnostic> Diagnostics { get; } = [];

    /// <summary>Gets the <c>//</c> and <c>/* */</c> comments read while lexing, in source order.</summary>
    public List<Comment> Comments { get; } = [];

    public IReadOnlyList<Token> Tokenize()
    {
        List<Token> tokens = [];
        Token token;
        do
        {
            token = this.NextToken();
            tokens.Add(token);
        } while (token.Kind != TokenKind.Eof);

        return tokens;
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private Token NextToken()
    {
        this.SkipTrivia();

        if (this.position >= this.source.Length)
        {
            return new Token(TokenKind.Eof, string.Empty, new SourceSpan(this.position, 0));
        }

        int start = this.position;
        char c = this.source[this.position];

        switch (c)
        {
            case '(':
                this.position++;
                return new Token(TokenKind.LParen, "(", new SourceSpan(start, 1));
            case ')':
                this.position++;
                return new Token(TokenKind.RParen, ")", new SourceSpan(start, 1));
            case '[':
                this.position++;
                return new Token(TokenKind.LBracket, "[", new SourceSpan(start, 1));
            case ']':
                this.position++;
                return new Token(TokenKind.RBracket, "]", new SourceSpan(start, 1));
            case '{':
                this.position++;
                return new Token(TokenKind.LBrace, "{", new SourceSpan(start, 1));
            case '}':
                this.position++;
                return new Token(TokenKind.RBrace, "}", new SourceSpan(start, 1));
            case ',':
                this.position++;
                return new Token(TokenKind.Comma, ",", new SourceSpan(start, 1));
            case ':':
                this.position++;
                return new Token(TokenKind.Colon, ":", new SourceSpan(start, 1));
            case ';':
                this.position++;
                return new Token(TokenKind.Semicolon, ";", new SourceSpan(start, 1));
            case '=':
                this.position++;
                return new Token(TokenKind.Equals, "=", new SourceSpan(start, 1));
            case '"':
                return this.ReadString(start);
            case '!' or '¬' or '∧' or '∨' or '⊕' or '→' or '↔' or '↑' or '↓' or '⊼' or '⊽' or '⊻' or '⇒' or '⇔':
                this.position++;
                return new Token(TokenKind.Operator, c.ToString(), new SourceSpan(start, 1));
            case '&' or '|' or '?' when this.position + 1 < this.source.Length && this.source[this.position + 1] == c:
                // Doubled form only (&&, ||, ??): a lone '&' or '|' is not an operator and falls through to the
                // unexpected-character diagnostic below (a lone '?' is the ternary token, handled by the next case).
                this.position += 2;
                return new Token(TokenKind.Operator, new string(c, 2), new SourceSpan(start, 2));
            case '?':
                // A lone '?' separates the condition from the branches of the ternary conditional (a ? b : c).
                this.position++;
                return new Token(TokenKind.Question, "?", new SourceSpan(start, 1));
        }

        if (
            char.IsDigit(c)
            || (c == '-' && this.position + 1 < this.source.Length && char.IsDigit(this.source[this.position + 1]))
        )
        {
            return this.ReadNumber(start);
        }

        if (IsIdentifierStart(c))
        {
            return this.ReadIdentifier(start);
        }

        this.position++;

        // A lone '&' or '|' is almost always half of the doubled symbol, so say which one.
        DiagnosticSuggestion? suggestion = DslVocabulary.DoubledSymbolFor(c) is { } doubled
            ? new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, doubled)
            : null;
        this.Diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Unexpected character '{c}'.",
                new SourceSpan(start, 1),
                expected: "a term, operator or delimiter",
                found: $"'{c}'",
                suggestion: suggestion
            )
        );
        return this.NextToken();
    }

    /// <summary>
    /// Skips whitespace and comments. Comments are trivia to the token stream, like whitespace, but each one is kept in
    /// <see cref="Comments"/> so the parser can attach it to a node as a note.
    /// </summary>
    private void SkipTrivia()
    {
        while (this.position < this.source.Length)
        {
            char c = this.source[this.position];
            char next = this.position + 1 < this.source.Length ? this.source[this.position + 1] : '\0';
            if (char.IsWhiteSpace(c))
            {
                this.position++;
            }
            else if (c == '/' && next == '/')
            {
                this.ReadLineComment();
            }
            else if (c == '/' && next == '*')
            {
                this.ReadBlockComment();
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>Reads a <c>//</c> comment up to, not including, the line feed that ends it.</summary>
    private void ReadLineComment()
    {
        int start = this.position;
        int end = this.source.IndexOf('\n', start);
        if (end < 0)
        {
            end = this.source.Length;
        }

        // One space after the slashes is the usual spacing, not content; any further indentation is kept.
        string text = this.source[(start + 2)..end].TrimEnd('\r');
        if (text.StartsWith(' '))
        {
            text = text[1..];
        }

        this.position = end;
        this.Comments.Add(new Comment(text, new SourceSpan(start, end - start), IsLine: true));
    }

    /// <summary>Reads a <c>/* */</c> comment. Comments do not nest; the first <c>*/</c> ends it.</summary>
    private void ReadBlockComment()
    {
        int start = this.position;
        int close = this.source.IndexOf("*/", start + 2, StringComparison.Ordinal);
        if (close < 0)
        {
            this.position = this.source.Length;
            this.Diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    "Unterminated block comment.",
                    new SourceSpan(start, this.position - start),
                    expected: "a closing '*/'",
                    found: "end of rule"
                )
            );
            return;
        }

        this.position = close + 2;
        this.Comments.Add(
            new Comment(this.source[(start + 2)..close], new SourceSpan(start, this.position - start), IsLine: false)
        );
    }

    private Token ReadIdentifier(int start)
    {
        while (this.position < this.source.Length && IsIdentifierPart(this.source[this.position]))
        {
            this.position++;
        }

        string text = this.source[start..this.position];
        return new Token(TokenKind.Identifier, text, new SourceSpan(start, this.position - start));
    }

    private Token ReadNumber(int start)
    {
        if (this.source[this.position] == '-')
        {
            this.position++;
        }

        while (this.position < this.source.Length && char.IsDigit(this.source[this.position]))
        {
            this.position++;
        }

        if (
            this.position < this.source.Length
            && this.source[this.position] == '.'
            && this.position + 1 < this.source.Length
            && char.IsDigit(this.source[this.position + 1])
        )
        {
            this.position++;
            while (this.position < this.source.Length && char.IsDigit(this.source[this.position]))
            {
                this.position++;
            }
        }

        string text = this.source[start..this.position];
        return new Token(TokenKind.NumberLiteral, text, new SourceSpan(start, this.position - start));
    }

    private Token ReadString(int start)
    {
        this.position++;
        System.Text.StringBuilder builder = new();
        while (this.position < this.source.Length && this.source[this.position] != '"')
        {
            char c = this.source[this.position];
            if (c == '\\' && this.position + 1 < this.source.Length)
            {
                char next = this.source[this.position + 1];
                switch (next)
                {
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    default:
                        this.Diagnostics.Add(
                            Diagnostic.Error(
                                DiagnosticCodes.InvalidEscapeSequence,
                                $"Unrecognized escape sequence '\\{next}' in string literal.",
                                new SourceSpan(this.position, 2),
                                expected: "one of \\\", \\\\, \\n, \\t",
                                found: $"\\{next}"
                            )
                        );
                        builder.Append(next);
                        break;
                }

                this.position += 2;
            }
            else
            {
                builder.Append(c);
                this.position++;
            }
        }

        if (this.position >= this.source.Length)
        {
            this.Diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    "Unterminated string literal.",
                    new SourceSpan(start, this.position - start),
                    expected: "a closing '\"'",
                    found: "end of rule"
                )
            );
        }
        else
        {
            this.position++;
        }

        return new Token(TokenKind.StringLiteral, builder.ToString(), new SourceSpan(start, this.position - start));
    }
}
