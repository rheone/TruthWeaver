namespace TruthWeaver.Parsing;

/// <summary>The kind of a lexical token in the DSL.</summary>
internal enum TokenKind
{
    /// <summary>A bare word: a keyword (AND/OR/NOT/XOR/true/false/ExactlyOne/AtLeast) or a predicate name.</summary>
    Identifier,

    /// <summary>A double-quoted string literal.</summary>
    StringLiteral,

    /// <summary>A numeric literal (integral or decimal, sign optional).</summary>
    NumberLiteral,

    /// <summary>'('.</summary>
    LParen,

    /// <summary>')'.</summary>
    RParen,

    /// <summary>'['. A grouping opener like '(' and '{', and the opener of an array argument value.</summary>
    LBracket,

    /// <summary>']'.</summary>
    RBracket,

    /// <summary>'{'. A grouping opener like '(' and '[' (ADR-0005 decision 9).</summary>
    LBrace,

    /// <summary>'}'.</summary>
    RBrace,

    /// <summary>','.</summary>
    Comma,

    /// <summary>':'.</summary>
    Colon,

    /// <summary>'?', the ternary conditional's separator (the doubled <c>??</c> is an <see cref="Operator"/>).</summary>
    Question,

    /// <summary>'=', separating a <c>let</c> definition's name (and parameters) from its body (ADR-0008).</summary>
    Equals,

    /// <summary>';', ending a <c>let</c> definition statement (ADR-0008).</summary>
    Semicolon,

    /// <summary>
    /// A symbolic operator (<c>&amp;&amp; || ! ∧ ∨ ¬ ⊕ → ↔ ↑ ↓</c>). <see cref="Token.Text"/> holds the symbol as written;
    /// the parser maps it to the named operator it is an alias for (ADR-0005 decision 2).
    /// </summary>
    Operator,

    /// <summary>End of input.</summary>
    Eof,
}
