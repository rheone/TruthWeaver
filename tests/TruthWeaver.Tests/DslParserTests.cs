namespace TruthWeaver.Tests;

using System.Diagnostics;
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
    /// A quantifier head with the <c>IN</c> keyword misspelled raises a syntax error naming the misspelling as its
    /// first diagnostic. Unlike the omitted-keyword case, the misspelled identifier is then consumed as the
    /// collection name (the same recovery), but what follows it is no longer the comma the grammar expects there, so
    /// this shape does cascade into follow-on diagnostics.
    /// </summary>
    [Fact]
    public void Parse_QuantifierMisspelledIn_RaisesSyntaxErrorExpectedInFirst_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("FORALL(x INN items, elementTrue)");

        Diagnostic diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal("Expected 'IN' but found 'INN'.", diagnostic.Message);
        Assert.Equal("'IN'", diagnostic.Expected);
        Assert.Equal("'INN'", diagnostic.Found);
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

    /// <summary>IsReservedWord returns true for every keyword, case insensitively.</summary>
    [Theory]
    [InlineData("and")]
    [InlineData("And")]
    [InlineData("AND")]
    [InlineData("xnor")]
    [InlineData("EXACTLYONE")]
    public void IsReservedWord_EveryKeyword_ReturnsTrueCaseInsensitively_Test(string keyword)
    {
        Assert.True(DslParser.IsReservedWord(keyword));
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
    /// (NoteAttacher.FindTarget/Deepest), which made a comment-heavy input quadratic. Parsing an input with 4x as many
    /// trailing-comment terms now takes close to 4x as long, not the ~16x a quadratic cost gives (the ticket's own
    /// repro measured ~14x for a 4x increase before the fix).
    /// </summary>
    [Fact]
    public void Parse_CommentHeavyInput_ScalesLinearlyNotQuadratically_Test()
    {
        string small = CommentHeavyRule(2_000);
        string large = CommentHeavyRule(8_000);

        // Warm up the JIT for each size so the first measured parse of either is not penalized by startup/tiering
        // costs.
        DslParser.Parse(small);
        DslParser.Parse(large);

        // Measure the two sizes interleaved, one pair per trial, and keep the fastest of each rather than a single
        // sample. This suite's own pre-commit hook runs many test assemblies in parallel, and that background
        // contention varies over the run, not just within it: measuring every small-input trial and only then
        // every large-input trial lets a contention spike land under one size's whole measurement window and a
        // quiet spell land under the other's, skewing the ratio even though each fastest-of-N value looks
        // plausible on its own. Interleaving keeps every pair close together in time, so a spike affects both
        // measurements of a pair together instead of confounding the comparison between them.
        double smallElapsedMs = double.MaxValue;
        double largeElapsedMs = double.MaxValue;
        for (int trial = 0; trial < 9; trial++)
        {
            smallElapsedMs = Math.Min(smallElapsedMs, TimeParse(small));
            largeElapsedMs = Math.Min(largeElapsedMs, TimeParse(large));
        }

        // Floor the small measurement so a near-zero denominator (a very fast machine) cannot inflate the ratio.
        double ratio = largeElapsedMs / Math.Max(smallElapsedMs, 1.0);
        string ratioMessage =
            $"4x more comments took {ratio:F1}x longer ({smallElapsedMs:F2} ms -> {largeElapsedMs:F2} ms); expected close to linear growth, not quadratic.";
        Assert.True(ratio < 8.0, ratioMessage);
        Assert.True(
            largeElapsedMs < 3_000,
            $"Parsing 8,000 comments took {largeElapsedMs:F2} ms; expected well under 3 seconds."
        );
    }

    // A double-millisecond reading (Elapsed.TotalMilliseconds), not the truncated-to-integer ElapsedMilliseconds:
    // the small input here can finish in low single-digit milliseconds, where integer truncation alone is a large
    // relative error and a separate source of ratio noise on top of scheduler contention.
    private static double TimeParse(string text)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        DslParser.Parse(text);
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static string CommentHeavyRule(int repeats)
    {
        return string.Concat(Enumerable.Repeat("a /* c */ AND ", repeats)) + "a";
    }
}
