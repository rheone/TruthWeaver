namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>Ticket 04: direct unit tests for the DSL recursive-descent parser.</summary>
public sealed class DslParserTests
{
    /// <summary>Trailing text after a complete expression raises a diagnostic and keeps the valid prefix.</summary>
    [Fact]
    public void Parse_TrailingGarbage_RaisesDiagnosticAndKeepsValidPrefix_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("a AND b c");

        Assert.IsType<AndNode>(root);
        Assert.Contains(
            diagnostics,
            d => d.Message.Contains("Unexpected token") && d.Message.Contains("after end of expression")
        );
    }

    /// <summary>A quantifier head with the <c>IN</c> keyword omitted (the collection name sits where it belongs) raises a single syntax error naming what was found there.</summary>
    [Fact]
    public void Parse_QuantifierMissingIn_RaisesSyntaxErrorExpectedInWithNoCascade_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("FORALL(x items, elementTrue)");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal("Expected 'IN' but found 'items'.", diagnostic.Message);
        Assert.Equal("'IN'", diagnostic.Expected);
        Assert.Equal("'items'", diagnostic.Found);
    }

    /// <summary>
    /// A quantifier head with the <c>IN</c> keyword misspelled, or replaced by another token, raises one syntax error
    /// naming that token and no follow-on diagnostics: the token is skipped, so the real collection name after it is
    /// still read as the collection, the same recovery as the omitted-keyword case.
    /// </summary>
    [Theory]
    [InlineData("FORALL(x INN items, elementTrue)", "INN")]
    [InlineData("EXISTS(x : items, elementTrue)", ":")]
    public void Parse_QuantifierMisspelledIn_RaisesOneSyntaxErrorWithNoCascade_Test(string text, string found)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(text);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal($"Expected 'IN' but found '{found}'.", diagnostic.Message);
        Assert.Equal("'IN'", diagnostic.Expected);
        QuantifierNode quantifier = Assert.IsType<QuantifierNode>(root);
        Assert.Equal("items", quantifier.CollectionName);
    }

    /// <summary>Mixing XOR with AND or OR without parentheses raises ambiguous operator mixing.</summary>
    [Fact]
    public void Mixing_XorWithAndOr_RaisesAmbiguousOperatorMixing_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("a XOR b AND c");

        Assert.Contains(diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>A parenthesized XOR chain avoids ambiguous operator mixing.</summary>
    [Fact]
    public void Mixing_ParenthesizedXorChain_AvoidsAmbiguity_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("(a XOR b) AND c");

        Assert.DoesNotContain(diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>Mixing XOR and XNOR at the same chain level raises the specific message.</summary>
    [Fact]
    public void Mixing_XorAndXnorAtSameLevel_RaisesSpecificMessage_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("a XOR b XNOR c");

        Assert.Contains(diagnostics, d => d.Message.Contains("Mixing XOR with EQUIVALENT"));
    }

    /// <summary>A chain of only XOR or only XNOR does not raise the mixing message.</summary>
    [Theory]
    [InlineData("a XOR b XOR c")]
    [InlineData("a XNOR b XNOR c")]
    public void Mixing_ChainOfOnlyXor_DoesNotRaiseMixingMessage_Test(string source)
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Assert.DoesNotContain(diagnostics, d => d.Message.Contains("Mixing XOR with EQUIVALENT"));
    }

    /// <summary>Input that starts with neither a term, a constant nor an open parenthesis raises the expected-term diagnostic.</summary>
    [Theory]
    [InlineData(",")]
    [InlineData(")")]
    public void Parse_InvalidStart_RaisesExpectedTermDiagnostic_Test(string source)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Assert.IsType<ErrorNode>(root);

        // Exactly one diagnostic (rather than several) proves parsing reached Eof and stopped
        // instead of looping back into the same fallback error path.
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Expected a term, constant, or '('", diagnostic.Message);
    }

    /// <summary>
    /// A comment attaches to a node of the parsed tree, so a rule that is a bare, unrecoverable parse error at the
    /// root (no node to measure an extent for) parses without throwing and drops the comment rather than attaching
    /// it anywhere.
    /// </summary>
    [Fact]
    public void Parse_CommentOverABareParseErrorRoot_DropsTheCommentWithoutThrowing_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> _) = DslParser.Parse("// a comment\n)");

        Assert.IsType<ErrorNode>(root);
        Assert.Null(root.Note);
    }

    /// <summary>A threshold that omits its integer first argument raises a diagnostic and defaults k to zero.</summary>
    [Fact]
    public void Threshold_OmittedInteger_RaisesDiagnosticAndDefaultsKToZero_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("AtLeast(a, b)");

        ThresholdNode threshold = Assert.IsType<ThresholdNode>(root);
        Assert.Equal(0, threshold.K);
        Assert.Contains(diagnostics, d => d.Message.Contains("Expected an integer threshold as AtLeast's first argument"));
    }

    /// <summary>A term argument with a missing colon raises an expected-colon diagnostic.</summary>
    [Fact]
    public void Argument_MissingColon_RaisesExpectedColon_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("f(x 1)");

        Assert.Contains(diagnostics, d => d.Message.Contains("Expected ':'"));
    }

    /// <summary>A literal position holding a bare identifier raises expected-literal-value and recovers with false.</summary>
    [Fact]
    public void Literal_BareIdentifier_RaisesExpectedLiteralAndRecoversWithFalse_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("f(x: y)");

        TermNode term = Assert.IsType<TermNode>(root);
        RawLiteral value = term.Arguments[0].Value;
        Assert.Equal(RawLiteralForm.Boolean, value.Form);
        Assert.False(value.BooleanValue);
        Assert.Contains(diagnostics, d => d.Message.Contains("Expected a literal value"));
    }

    /// <summary>An empty array literal parses to a zero element array.</summary>
    [Fact]
    public void ArrayLiteral_Empty_ParsesToZeroElementArray_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> _) = DslParser.Parse("f(x: [])");

        TermNode term = Assert.IsType<TermNode>(root);
        RawLiteral value = term.Arguments[0].Value;
        Assert.Equal(RawLiteralForm.Array, value.Form);
        Assert.Empty(value.Elements!);
    }

    /// <summary>An array literal missing its closing bracket raises an expected-bracket diagnostic.</summary>
    [Fact]
    public void ArrayLiteral_MissingClosingBracket_RaisesExpectedBracket_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("f(x: [1, 2)");

        Assert.Contains(diagnostics, d => d.Message.Contains("Expected ']'"));
    }

    /// <summary>The constant letters T, F and U are reserved words, in either case.</summary>
    [Theory]
    [InlineData("T")]
    [InlineData("f")]
    [InlineData("U")]
    public void IsReservedWord_ConstantLetter_ReturnsTrueCaseInsensitively_Test(string letter)
    {
        Assert.True(DslParser.IsReservedWord(letter));
    }

    /// <summary>The bare letters T, F and U, in any case, parse as the constants True, False and Unknown.</summary>
    [Theory]
    [InlineData("T", TruthValue.True)]
    [InlineData("t", TruthValue.True)]
    [InlineData("F", TruthValue.False)]
    [InlineData("f", TruthValue.False)]
    [InlineData("U", TruthValue.Unknown)]
    [InlineData("u", TruthValue.Unknown)]
    public void Parse_ConstantLetter_YieldsTheConstant_Test(string text, TruthValue expected)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(text);

        Assert.Empty(diagnostics);
        ConstantNode constant = Assert.IsType<ConstantNode>(root);
        Assert.Equal(expected, constant.Value);
    }

    /// <summary>A longer identifier that starts with a constant letter is still a term name.</summary>
    [Fact]
    public void Parse_IdentifierStartingWithAConstantLetter_IsATerm_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("Total AND farm");

        Assert.Empty(diagnostics);
        AndNode and = Assert.IsType<AndNode>(root);
        Assert.All(and.Operands, operand => Assert.IsType<TermNode>(operand));
    }

    /// <summary>IsReservedWord returns false for an arbitrary predicate name.</summary>
    [Fact]
    public void IsReservedWord_ArbitraryName_ReturnsFalse_Test()
    {
        Assert.False(DslParser.IsReservedWord("hasRole"));
    }

    /// <summary>IsReservedWord returns true for the 'let' keyword (ADR-0008), case-insensitively.</summary>
    [Fact]
    public void IsReservedWord_Let_ReturnsTrue_Test()
    {
        Assert.True(DslParser.IsReservedWord("let"));
        Assert.True(DslParser.IsReservedWord("LET"));
    }

    /// <summary>A document with one parameterless 'let' and a bare use parses to a RuleDocumentNode wrapping one UseNode.</summary>
    [Fact]
    public void Parse_ParameterlessLetAndBareUse_ProducesRuleDocumentNodeWithUseNode_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("let adult = a; adult");

        Assert.Empty(diagnostics);
        RuleDocumentNode document = Assert.IsType<RuleDocumentNode>(root);
        DefinitionNode definition = Assert.Single(document.Definitions);
        Assert.Equal("adult", definition.Name);
        Assert.Empty(definition.Parameters);
        Assert.IsType<TermNode>(definition.Body);
        UseNode use = Assert.IsType<UseNode>(document.Body);
        Assert.Equal("adult", use.Name);
        Assert.Empty(use.Arguments);
    }

    /// <summary>A parameterized 'let' and a call-like use parse to a DefinitionNode with its parameters and a UseNode with positional argument expressions.</summary>
    [Fact]
    public void Parse_ParameterizedLetAndCallLikeUse_ProducesDefinitionAndUseWithArguments_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(
            "let bothTrue(x, y) = x AND y; bothTrue(a, b)"
        );

        Assert.Empty(diagnostics);
        RuleDocumentNode document = Assert.IsType<RuleDocumentNode>(root);
        DefinitionNode definition = Assert.Single(document.Definitions);
        Assert.Equal(["x", "y"], definition.Parameters);
        AndNode body = Assert.IsType<AndNode>(definition.Body);
        Assert.IsType<ParameterReferenceNode>(body.Operands[0]);
        Assert.IsType<ParameterReferenceNode>(body.Operands[1]);
        UseNode use = Assert.IsType<UseNode>(document.Body);
        Assert.Equal(2, use.Arguments.Count);
        Assert.All(use.Arguments, argument => Assert.IsType<TermNode>(argument));
    }

    /// <summary>A 'let' declaration missing its '=' raises a syntax error naming it.</summary>
    [Fact]
    public void Parse_LetMissingEquals_RaisesSyntaxErrorForEquals_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("let adult a; adult");

        Assert.Contains(diagnostics, d => d.Code == DiagnosticCodes.SyntaxError && d.Message.Contains("Expected '='"));
    }

    /// <summary>A 'let' declaration missing its terminating ';' raises a syntax error naming it.</summary>
    [Fact]
    public void Parse_LetMissingSemicolon_RaisesSyntaxErrorForSemicolon_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("let adult = a adult");

        Assert.Contains(diagnostics, d => d.Code == DiagnosticCodes.SyntaxError && d.Message.Contains("Expected ';'"));
    }

    /// <summary>A bare identifier that is a term name, not a declared definition or parameter, still parses as an ordinary TermNode.</summary>
    [Fact]
    public void Parse_BareIdentifierNotADefinitionOrParameter_StaysATermNode_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("let adult = a; hasRole");

        Assert.Empty(diagnostics);
        RuleDocumentNode document = Assert.IsType<RuleDocumentNode>(root);
        Assert.IsType<TermNode>(document.Body);
    }

    /// <summary>
    /// Ticket 25: comment-to-note attachment used to re-scan every token and every node extent for every comment
    /// (NoteAttacher.FindTarget/Deepest), which made a comment-heavy input quadratic. Parsing an input with 4x as
    /// many trailing-comment terms now does close to 4x as much attachment work, not the ~16x a quadratic cost
    /// gives (the ticket's own repro measured ~14x wall-clock for a 4x increase before the fix).
    /// </summary>
    /// <remarks>
    /// Flaky-scaling-tests ticket 01: this used to assert a wall-clock ratio between the two sizes, which failed
    /// once in CI under contention despite a JIT warm-up, best-of-nine sampling and interleaved measurement,
    /// because the code path is genuinely linear but the ratio of two noisy wall-clock samples is not a
    /// deterministic signal. <see cref="DslParser.ParseWithNoteAttachSteps"/> exposes
    /// <see cref="NoteAttacher.Attach"/>'s own step count instead: a count is exact and machine-independent, so
    /// this removes the CI-noise dependency entirely rather than trading it for a wider, less sensitive margin.
    /// </remarks>
    [Fact]
    public void Parse_CommentHeavyInput_ScalesLinearlyNotQuadratically_Test()
    {
        string small = CommentHeavyRule(2_000);
        string large = CommentHeavyRule(8_000);

        (RuleNode _, IReadOnlyList<Diagnostic> _, long smallSteps) = DslParser.ParseWithNoteAttachSteps(small);
        (RuleNode _, IReadOnlyList<Diagnostic> _, long largeSteps) = DslParser.ParseWithNoteAttachSteps(large);

        Assert.True(smallSteps > 0, "Expected the small input to exercise the token-scan step counter at all.");
        double ratio = (double)largeSteps / smallSteps;
        Assert.True(
            ratio < 4.5,
            $"4x more comments did {ratio:F2}x as many token-scan steps ({smallSteps} -> {largeSteps}); expected close to linear growth, not quadratic."
        );
    }

    private static string CommentHeavyRule(int repeats)
    {
        return string.Concat(Enumerable.Repeat("a /* c */ AND ", repeats)) + "a";
    }
}
