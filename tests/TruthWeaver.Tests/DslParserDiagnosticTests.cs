namespace TruthWeaver.Tests;

using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// What the DSL parser reports for malformed rule text: the expected-versus-found pair, the hint and the recovery, so an
/// author sees which part to fix and one mistake does not raise a second diagnostic.
/// </summary>
public sealed class DslParserDiagnosticTests
{
    /// <summary>A bare infix operator in the condition of a ternary is reported as the condition.</summary>
    [Fact]
    public void Parse_AndChainAsTernaryCondition_ReportsTheConditionRole_Test()
    {
        Diagnostic diagnostic = Single("a AND b ? c : d");

        Assert.Equal(DiagnosticCodes.AmbiguousOperatorMixing, diagnostic.Code);
        Assert.Equal(
            "Mixing ?: with AND at the same level requires explicit parentheses. "
                + "Add parentheses around the AND expression used as the condition to say which operator applies first.",
            diagnostic.Message
        );
        Assert.Equal("parentheses around one of the groups", diagnostic.Expected);
        Assert.Equal("?: next to AND without parentheses", diagnostic.Found);
        Assert.Equal("Wrap the AND expression in parentheses: (a AND b)", diagnostic.Suggestion?.Text);
    }

    /// <summary>A bare OR chain in the true branch is reported as the true branch.</summary>
    [Fact]
    public void Parse_OrChainAsTrueBranch_ReportsTheTrueBranchRole_Test()
    {
        Diagnostic diagnostic = Single("a ? b OR c : d");

        Assert.Contains("Mixing ?: with OR at the same level", diagnostic.Message);
        Assert.Contains("used as the true branch", diagnostic.Message);
        Assert.Equal("?: next to OR without parentheses", diagnostic.Found);
    }

    /// <summary>A bare infix operator in the false branch is reported as the false branch, spelt with its symbol for COALESCE.</summary>
    [Fact]
    public void Parse_CoalesceChainAsFalseBranch_ReportsTheFalseBranchRoleWithTheSymbol_Test()
    {
        Diagnostic diagnostic = Single("a ? b : c ?? d");

        Assert.Contains("Mixing ?: with ?? at the same level", diagnostic.Message);
        Assert.Contains("used as the false branch", diagnostic.Message);
        Assert.Equal("?: next to ?? without parentheses", diagnostic.Found);
    }

    /// <summary>A parenthesized chain in a ternary needs no parentheses and raises nothing.</summary>
    [Fact]
    public void Parse_ParenthesizedChainsInATernary_RaiseNothing_Test()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("(a AND b) ? [c OR d] : {e AND f}");

        Assert.Empty(diagnostics);
    }

    /// <summary>A chain whose operands are each parenthesized is still a bare chain next to the ternary.</summary>
    [Fact]
    public void Parse_ChainOfParenthesizedOperandsAsCondition_IsReported_Test()
    {
        Diagnostic diagnostic = Single("(a) AND (b) ? c : d");

        Assert.Equal("?: next to AND without parentheses", diagnostic.Found);
    }

    /// <summary>A missing colon in a ternary expects a colon at the end of the rule.</summary>
    [Fact]
    public void Parse_TernaryWithoutAColon_ExpectsAColon_Test()
    {
        Diagnostic diagnostic = Assert.Single(Parse("a ? b"), d => d.Expected == "':'");

        Assert.Equal("end of rule", diagnostic.Found);
    }

    /// <summary>A ternary nested in a branch is reported once, with its hint, and its bare condition is reported once.</summary>
    [Fact]
    public void Parse_NestedTernaryWithBareCondition_ReportsEachProblemOnce_Test()
    {
        IReadOnlyList<Diagnostic> diagnostics = Parse("a ? b : c AND d ? e : f");

        Diagnostic nested = Assert.Single(diagnostics, d => d.Found == "?: next to ?: without parentheses");
        Assert.Equal("Add parentheses around the nested conditional to say how the branches group.", nested.Suggestion?.Text);
        Assert.Contains("Mixing ?: with another ?: at the same level", nested.Message);
        Diagnostic bare = Assert.Single(diagnostics, d => d.Found == "?: next to AND without parentheses");
        Assert.Contains("used as the false branch", bare.Message);
    }

    /// <summary>A chain that mixes two infix operators names both and reports at the second operator.</summary>
    [Fact]
    public void Parse_TwoDifferentInfixOperators_NameBothInTheFoundText_Test()
    {
        Diagnostic diagnostic = Single("a XOR b XNOR c");

        Assert.Equal("XOR next to EQUIVALENT without parentheses", diagnostic.Found);
        Assert.Equal("parentheses around one of the groups", diagnostic.Expected);
        Assert.Equal("Add parentheses around the operands that should be grouped first.", diagnostic.Suggestion?.Text);
    }

    /// <summary>A bare infix operand in an AND chain is reported at its own span with a hint that quotes the source.</summary>
    [Fact]
    public void Parse_XorOperandInAnAndChain_ReportsTheOperandAndAPasteableHint_Test()
    {
        Diagnostic diagnostic = Single("c AND a XOR b");

        Assert.Equal("XOR next to AND without parentheses", diagnostic.Found);
        Assert.Equal("Wrap the XOR expression in parentheses: (a XOR b)", diagnostic.Suggestion?.Text);
        Assert.Equal(new SourceSpan(6, 7), diagnostic.Span);
    }

    /// <summary>A bare COALESCE operand in an OR chain is spelt <c>??</c>.</summary>
    [Fact]
    public void Parse_CoalesceOperandInAnOrChain_IsSpeltWithTheSymbol_Test()
    {
        Diagnostic diagnostic = Single("c OR a ?? b");

        Assert.Equal("?? next to OR without parentheses", diagnostic.Found);
        Assert.Contains("Mixing ?? with AND/OR at the same level", diagnostic.Message);
        Assert.Equal("Wrap the ?? expression in parentheses: (a ?? b)", diagnostic.Suggestion?.Text);
    }

    /// <summary>A trailing token that is not a word never gets an operator suggestion.</summary>
    [Fact]
    public void Parse_TrailingStringLiteral_HasNoOperatorSuggestion_Test()
    {
        Diagnostic diagnostic = Single("a \"ANDD\"");

        Assert.Equal("an operator or the end of the rule", diagnostic.Expected);
        Assert.Null(diagnostic.Suggestion);
    }

    /// <summary>A trailing misspelt operator word gets the nearest operator as a suggestion.</summary>
    [Fact]
    public void Parse_TrailingMisspeltOperator_SuggestsTheOperator_Test()
    {
        Diagnostic diagnostic = Single("a ANDD b");

        Assert.Equal("AND", diagnostic.Suggestion?.Text);
    }

    /// <summary>An argument that does not start with a name expects an argument name.</summary>
    [Fact]
    public void Parse_ArgumentWithoutAName_ExpectsAnArgumentName_Test()
    {
        Diagnostic diagnostic = Assert.Single(Parse("hasRole(5: x)"), d => d.Expected == "an argument name");

        Assert.Equal("'5'", diagnostic.Found);
    }

    /// <summary>A variable reference with a non-string source expects a quoted source name and keeps an empty source.</summary>
    [Fact]
    public void Parse_VariableReferenceWithAnUnquotedSource_ExpectsAQuotedSourceName_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("p(x: from(5, \"q\"))");

        Assert.Contains(diagnostics, d => d.Expected == "a quoted source name" && d.Found == "'5'");
        RawLiteral variable = Assert.IsType<TermNode>(root).Arguments[0].Value;
        Assert.Equal(RawLiteralForm.Variable, variable.Form);
        Assert.Equal(string.Empty, variable.Text);
    }

    /// <summary>A variable reference with a non-string query expects a quoted query and keeps an empty query.</summary>
    [Fact]
    public void Parse_VariableReferenceWithAnUnquotedQuery_ExpectsAQuotedQuery_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("p(x: from(\"s\", 6))");

        Assert.Contains(diagnostics, d => d.Expected == "a quoted query" && d.Found == "'6'");
        RawLiteral variable = Assert.IsType<TermNode>(root).Arguments[0].Value;
        Assert.Equal("s", variable.Text);
        Assert.Equal(string.Empty, variable.Query);
    }

    /// <summary>A variable reference with no comma between its parts expects a comma.</summary>
    [Fact]
    public void Parse_VariableReferenceWithoutAComma_ExpectsAComma_Test()
    {
        Assert.Contains(Parse("p(x: from(\"s\" \"q\"))"), d => d.Expected == "','");
    }

    /// <summary>A word that is not <c>from</c>, or <c>from</c> without a parenthesis, is not a variable reference.</summary>
    [Theory]
    [InlineData("p(x: other(\"s\", \"q\"))", "'other'")]
    [InlineData("p(x: from)", "'from'")]
    [InlineData("p(x: from \"s\")", "'from'")]
    public void Parse_ArgumentValueThatIsNotAVariableReference_IsAnUnexpectedLiteral_Test(string source, string found)
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Diagnostic first = diagnostics[0];
        Assert.Equal("a literal value", first.Expected);
        Assert.Equal(found, first.Found);
    }

    /// <summary>The word <c>from</c> starts a variable reference in any letter case.</summary>
    [Fact]
    public void Parse_UppercaseFromWithParentheses_IsAVariableReference_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("p(x: FROM(\"s\", \"q\"))");

        Assert.Empty(diagnostics);
        Assert.Equal(RawLiteralForm.Variable, Assert.IsType<TermNode>(root).Arguments[0].Value.Form);
    }

    /// <summary>A bad literal is consumed once, so the closing parenthesis that follows raises nothing more.</summary>
    [Fact]
    public void Parse_UnexpectedTokenAsArgumentValue_RaisesOneDiagnostic_Test()
    {
        Diagnostic diagnostic = Single("p(x: ?)");

        Assert.Equal("a literal value", diagnostic.Expected);
        Assert.Equal("'?'", diagnostic.Found);
    }

    /// <summary>A rule that ends where an argument value should start is reported at the end without failing.</summary>
    [Fact]
    public void Parse_RuleEndingAtAnArgumentValue_ReportsTheEndOfRule_Test()
    {
        IReadOnlyList<Diagnostic> diagnostics = Parse("p(x:");

        Assert.Contains(diagnostics, d => d.Expected == "a literal value" && d.Found == "end of rule");
    }

    /// <summary>The bounds of BETWEEN are named minimum and maximum, as its first and second argument.</summary>
    [Fact]
    public void Parse_BetweenWithBadBounds_NamesTheMinimumAndMaximum_Test()
    {
        IReadOnlyList<Diagnostic> diagnostics = Parse("BETWEEN(x, y, a)");

        Diagnostic minimum = Assert.Single(diagnostics, d => d.Message.Contains("minimum"));
        Diagnostic maximum = Assert.Single(diagnostics, d => d.Message.Contains("maximum"));
        Assert.Equal("Expected an integer minimum as BETWEEN's first argument.", minimum.Message);
        Assert.Equal("Expected an integer maximum as BETWEEN's second argument.", maximum.Message);
        Assert.Equal("an integer", minimum.Expected);
        Assert.Equal("'x'", minimum.Found);
    }

    /// <summary>A number that is not an integer is consumed, so the operands after it still parse.</summary>
    [Fact]
    public void Parse_BetweenWithADecimalBound_ConsumesTheNumberAndKeepsTheOperands_Test()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("BETWEEN(1.5, 2, a)");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("'1.5'", diagnostic.Found);
        BetweenNode between = Assert.IsType<BetweenNode>(root);
        Assert.Single(between.Operands);
    }

    /// <summary>A call without its opening parenthesis expects one and reports the token it found.</summary>
    [Fact]
    public void Parse_CallWithoutAnOpeningParenthesis_ExpectsAParenthesis_Test()
    {
        Diagnostic diagnostic = Assert.Single(Parse("ANY a)"), d => d.Expected == "'('");

        Assert.Equal("'a'", diagnostic.Found);
    }

    /// <summary>A bracket cannot open a call, and the stand-in parenthesis shows in the closing diagnostic.</summary>
    [Fact]
    public void Parse_CallOpenedWithABracket_ReportsTheStandInParenthesis_Test()
    {
        IReadOnlyList<Diagnostic> diagnostics = Parse("ANY[a, b]");

        Assert.Contains(diagnostics, d => d.Expected == "'('");
        Assert.Contains(diagnostics, d => d.Message.StartsWith("Expected ')' to close '('", StringComparison.Ordinal));
    }

    /// <summary>The retired NXOR spelling is rejected with PARITY as the expected word.</summary>
    [Fact]
    public void Parse_Nxor_ExpectsParity_Test()
    {
        Diagnostic diagnostic = Single("NXOR(a, b)");

        Assert.Equal("PARITY", diagnostic.Expected);
        Assert.Equal("NXOR", diagnostic.Found);
    }

    /// <summary>Every reserved word is rejected as a predicate name, in any letter case.</summary>
    [Theory]
    [InlineData("AND")]
    [InlineData("or")]
    [InlineData("Not")]
    [InlineData("xor")]
    [InlineData("equivalent")]
    [InlineData("iff")]
    [InlineData("xnor")]
    [InlineData("implies")]
    [InlineData("nand")]
    [InlineData("nor")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("unknown")]
    [InlineData("parity")]
    [InlineData("nxor")]
    [InlineData("any")]
    [InlineData("all")]
    [InlineData("none")]
    [InlineData("between")]
    [InlineData("coalesce")]
    [InlineData("if")]
    [InlineData("isTrue")]
    [InlineData("isFalse")]
    [InlineData("isUnknown")]
    [InlineData("isKnown")]
    [InlineData("project")]
    [InlineData("collapse")]
    [InlineData("exactlyOne")]
    [InlineData("atLeast")]
    [InlineData("atMost")]
    [InlineData("greaterThan")]
    [InlineData("lessThan")]
    [InlineData("exactly")]
    public void IsReservedWord_DslKeyword_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>A word that is not a keyword is not reserved.</summary>
    [Fact]
    public void IsReservedWord_OrdinaryName_IsNotReserved_Test()
    {
        Assert.False(DslParser.IsReservedWord("isManager"));
    }

    /// <summary>Each inspection call parses to its inspection kind.</summary>
    [Theory]
    [InlineData("IsTrue(a)", InspectionKind.IsTrue)]
    [InlineData("isFalse(a)", InspectionKind.IsFalse)]
    [InlineData("ISUNKNOWN(a)", InspectionKind.IsUnknown)]
    [InlineData("IsKnown(a)", InspectionKind.IsKnown)]
    public void Parse_InspectionCall_YieldsTheInspectionKind_Test(string source, InspectionKind kind)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Assert.Empty(diagnostics);
        Assert.Equal(kind, Assert.IsType<InspectionNode>(root).Kind);
    }

    /// <summary>Each infix word parses to its operator node.</summary>
    [Theory]
    [InlineData("a XOR b", typeof(XorNode))]
    [InlineData("a EQUIVALENT b", typeof(EquivalentNode))]
    [InlineData("a IFF b", typeof(EquivalentNode))]
    [InlineData("a XNOR b", typeof(EquivalentNode))]
    [InlineData("a IMPLIES b", typeof(ImpliesNode))]
    [InlineData("a NAND b", typeof(NandNode))]
    [InlineData("a NOR b", typeof(NorNode))]
    [InlineData("a ?? b", typeof(CoalesceNode))]
    public void Parse_InfixOperator_YieldsItsNode_Test(string source, Type nodeType)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Assert.Empty(diagnostics);
        Assert.IsType(nodeType, root);
    }

    /// <summary>The infix words offered as suggestions are the documented spellings.</summary>
    [Fact]
    public void InfixWords_ListsTheDocumentedWordOperators_Test()
    {
        Assert.Equal(["AND", "OR", "XOR", "EQUIVALENT", "IFF", "XNOR", "IMPLIES", "NAND", "NOR"], DslVocabulary.InfixWords);
    }

    /// <summary>The keywords offered as suggestions add the prefix operators, calls and constants in documented spelling.</summary>
    [Fact]
    public void Keywords_ListsEveryKeywordInDocumentedSpelling_Test()
    {
        Assert.Equal(
            [
                .. DslVocabulary.InfixWords,
                "NOT",
                "PARITY",
                "ANY",
                "ALL",
                "NONE",
                "BETWEEN",
                "COALESCE",
                "If",
                "IsTrue",
                "IsFalse",
                "IsUnknown",
                "IsKnown",
                "ExactlyOne",
                "AtLeast",
                "AtMost",
                "GreaterThan",
                "LessThan",
                "Exactly",
                "True",
                "False",
                "Unknown",
            ],
            DslVocabulary.Keywords
        );
    }

    /// <summary>A stray half of a doubled symbol maps to the doubled symbol.</summary>
    [Theory]
    [InlineData('&', "&&")]
    [InlineData('|', "||")]
    public void DoubledSymbolFor_HalfOfADoubledSymbol_ReturnsTheDoubledSymbol_Test(char character, string expected)
    {
        Assert.Equal(expected, DslVocabulary.DoubledSymbolFor(character));
    }

    /// <summary>Any other character has no doubled symbol.</summary>
    [Fact]
    public void DoubledSymbolFor_OtherCharacter_ReturnsNull_Test()
    {
        Assert.Null(DslVocabulary.DoubledSymbolFor('^'));
    }

    private static IReadOnlyList<Diagnostic> Parse(string source)
    {
        return DslParser.Parse(source).Diagnostics;
    }

    private static Diagnostic Single(string source)
    {
        return Assert.Single(Parse(source));
    }
}
