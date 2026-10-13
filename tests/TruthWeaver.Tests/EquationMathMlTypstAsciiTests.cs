namespace TruthWeaver.Tests;

using System.Text;
using System.Xml.Linq;
using CsCheck;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.DocExamples;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintEquation(EquationOptions?)"/> in the <see cref="EquationDialect.MathML"/>,
/// <see cref="EquationDialect.Typst"/> and <see cref="EquationDialect.Ascii"/> dialects (ticket 08): every operator,
/// term, constant, the simple-variable mode with its legend, and the escaping rules of each dialect.
/// </summary>
public sealed class EquationMathMlTypstAsciiTests
{
    private const string MathOpen = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\">";

    /// <summary>A single backslash, kept in a constant so the expected escape sequences read clearly.</summary>
    private const string Slash = @"\";

    private const int SampleIterations = 300;

    private static readonly EquationOptions MathMl = new() { Dialect = EquationDialect.MathML };
    private static readonly EquationOptions Typst = new() { Dialect = EquationDialect.Typst };
    private static readonly EquationOptions Ascii = new() { Dialect = EquationDialect.Ascii };

    private static readonly Gen<char> GenAnyChar = Gen.Frequency(
        (60, Gen.Char.AlphaNumeric),
        (40, Gen.Int[0, 0xFFFF].Select(code => (char)code))
    );

    private static readonly Gen<string> GenText = Gen.String[GenAnyChar, 1, 8];

    private static readonly Gen<string?> GenOverride = Gen.OneOf(
        Gen.Const((string?)null),
        GenText.Select(text => (string?)text.Replace('\r', ' ').Replace('\n', ' '))
    );

    private static readonly Gen<ConstantFormat> GenConstants = Gen.Enum<ConstantStyle>()
        .Select(
            GenOverride,
            GenOverride,
            GenOverride,
            (style, trueText, falseText, unknownText) =>
                new ConstantFormat
                {
                    Style = style,
                    TrueText = trueText,
                    FalseText = falseText,
                    UnknownText = unknownText,
                }
        );

    private static readonly Gen<Expression> GenLeaf = Gen.OneOf(
        Gen.Enum<TruthValue>().Select(value => (Expression)new ConstantExpression(value)),
        GenText.Select(name => (Expression)new TermExpression(new TermIdentity(name, []))),
        GenText.Select(GenText, TermWithArgument)
    );

    private static readonly Gen<Expression> GenTree = Gen.Recursive<Expression>(
        (depth, self) =>
        {
            if (depth >= 4)
            {
                return GenLeaf;
            }

            return Gen.Frequency(
                (3, GenLeaf),
                (2, self.Array[2, 3].Select(o => (Expression)new AndExpression(new EquatableArray<Expression>(o)))),
                (2, self.Array[2, 3].Select(o => (Expression)new OrExpression(new EquatableArray<Expression>(o)))),
                (2, self.Select(o => (Expression)new NotExpression(o))),
                (1, self.Select(self, (l, r) => (Expression)new XorExpression(l, r))),
                (1, self.Select(self, (l, r) => (Expression)new EquivalentExpression(l, r))),
                (1, self.Select(self, (l, r) => (Expression)new ImpliesExpression(l, r))),
                (1, self.Select(self, (l, r) => (Expression)new NandExpression(l, r))),
                (1, self.Select(self, (l, r) => (Expression)new NorExpression(l, r))),
                (1, self.Array[3].Select(o => (Expression)new IfExpression(o[0], o[1], o[2]))),
                (1, self.Array[2, 3].Select(o => (Expression)new CoalesceExpression(new EquatableArray<Expression>(o))))
            );
        }
    );

    /// <summary>MathML wraps the equation in one <c>math</c> element, writes terms as <c>mtext</c> and operators as <c>mo</c>, and groups with <c>mo</c> parentheses.</summary>
    [Fact]
    public void PrintEquation_MathMlAndWithOrOperand_PrintsMoOperatorsAndParenthesesInOneMathElement_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string equation = rule.PrintEquation(MathMl);

        Assert.Equal(
            $"{MathOpen}<mtext>a</mtext> <mo>∧</mo> <mo>(</mo><mtext>b</mtext> <mo>∨</mo> <mtext>c</mtext><mo>)</mo></math>",
            equation
        );
    }

    /// <summary>Every binary connective prints as its MathML operator between its operands.</summary>
    [Theory]
    [InlineData("AND", "∧")]
    [InlineData("OR", "∨")]
    [InlineData("XOR", "⊕")]
    [InlineData("EQUIVALENT", "↔")]
    [InlineData("IMPLIES", "→")]
    [InlineData("NAND", "↑")]
    [InlineData("NOR", "↓")]
    public void PrintEquation_MathMlBinaryConnective_PrintsItsOperatorElementInfix_Test(string word, string symbol)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation(MathMl);

        Assert.Equal($"{MathOpen}<mtext>a</mtext> <mo>{symbol}</mo> <mtext>b</mtext></math>", equation);
    }

    /// <summary>MathML negation is a prefix <c>mo</c> directly before its operand.</summary>
    [Fact]
    public void PrintEquation_MathMlNot_PrintsThePrefixOperatorElement_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        string equation = rule.PrintEquation(MathMl);

        Assert.Equal($"{MathOpen}<mo>¬</mo><mtext>a</mtext></math>", equation);
    }

    /// <summary>MathML writes <c>If</c> and the cardinality operators in function-call form, with the count as an <c>mn</c> element.</summary>
    [Fact]
    public void PrintEquation_MathMlFunctionCallOperators_PrintTheCallWithMoDelimiters_Test()
    {
        CompiledRule<RuleTestContext> ifRule = Compile("If(a, b, c)");
        CompiledRule<RuleTestContext> atLeast = Compile("AtLeast(2, a, b)");

        Assert.Equal(
            $"{MathOpen}<mtext>If</mtext><mo>(</mo><mtext>a</mtext> <mo>,</mo> <mtext>b</mtext> <mo>,</mo> <mtext>c</mtext><mo>)</mo></math>",
            ifRule.PrintEquation(MathMl)
        );
        Assert.Equal(
            $"{MathOpen}<mtext>AtLeast</mtext><mo>(</mo><mn>2</mn> <mo>,</mo> <mtext>a</mtext> <mo>,</mo> <mtext>b</mtext><mo>)</mo></math>",
            atLeast.PrintEquation(MathMl)
        );
    }

    /// <summary>MathML escapes <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> in a term and leaves a quote alone.</summary>
    [Fact]
    public void PrintEquation_MathMlTermWithMarkupCharacters_EscapesThemAsEntities_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"a<b&c>\")");

        string equation = rule.PrintEquation(MathMl);

        Assert.Equal($"{MathOpen}<mtext>hasCrust(crust: \"a&lt;b&amp;c&gt;\")</mtext></math>", equation);
    }

    /// <summary>With argument values hidden, MathML prints the predicate name only.</summary>
    [Fact]
    public void PrintEquation_MathMlArgumentValuesHidden_PrintsThePredicateName_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\")");

        string equation = rule.PrintEquation(MathMl with { ShowArgumentValues = false });

        Assert.Equal($"{MathOpen}<mtext>hasCrust</mtext></math>", equation);
    }

    /// <summary>MathML writes a constant as <c>mtext</c> (letters by default, words on request), or as an <c>mo</c> symbol for the logic-symbol style.</summary>
    [Fact]
    public void PrintEquation_MathMlConstants_PrintTextOrLogicSymbols_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True AND False AND Unknown");
        ConstantFormat symbols = new() { Style = ConstantStyle.LogicSymbols };
        ConstantFormat words = new() { Style = ConstantStyle.Word };

        Assert.Equal(
            $"{MathOpen}<mtext>T</mtext> <mo>∧</mo> <mtext>F</mtext> <mo>∧</mo> <mtext>U</mtext></math>",
            rule.PrintEquation(MathMl)
        );
        Assert.Equal(
            $"{MathOpen}<mtext>True</mtext> <mo>∧</mo> <mtext>False</mtext> <mo>∧</mo> <mtext>Unknown</mtext></math>",
            rule.PrintEquation(MathMl with { Constants = words })
        );
        Assert.Equal(
            $"{MathOpen}<mo>⊤</mo> <mo>∧</mo> <mo>⊥</mo> <mo>∧</mo> <mtext>U</mtext></math>",
            rule.PrintEquation(MathMl with { Constants = symbols })
        );
    }

    /// <summary>A MathML override is escaped like a term, and a character that XML cannot hold becomes U+FFFD.</summary>
    [Fact]
    public void PrintEquation_MathMlOverrideWithInvalidXmlCharacter_ReplacesItWithTheReplacementCharacter_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True");
        ConstantFormat constants = new() { TrueText = "<x>\u0001\ud800" };

        string equation = rule.PrintEquation(MathMl with { Constants = constants });

        string replacement = Rune.ReplacementChar.ToString();
        Assert.Equal($"{MathOpen}<mtext>&lt;x&gt;{replacement}{replacement}</mtext></math>", equation);
    }

    /// <summary>A tab, line feed and carriage return in a MathML term become character references, so a legend line stays on one line.</summary>
    [Fact]
    public void PrintEquation_MathMlOverrideWithWhitespaceControls_WritesCharacterReferences_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"a\nb\tc\")");

        string legend = rule.PrintEquationWithLegend(MathMl).LegendText;

        Assert.DoesNotContain('\n', legend);
        Assert.Contains("a&#xA;b&#x9;c", legend, StringComparison.Ordinal);
    }

    /// <summary>A valid surrogate pair in a MathML term stays as one character.</summary>
    [Fact]
    public void PrintEquation_MathMlOverrideWithSurrogatePair_KeepsTheCharacter_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True");
        ConstantFormat constants = new() { TrueText = "😀" };

        string equation = rule.PrintEquation(MathMl with { Constants = constants });

        Assert.Equal($"{MathOpen}<mtext>😀</mtext></math>", equation);
    }

    /// <summary>The MathML equation and each MathML legend line parse as well-formed XML in the MathML namespace.</summary>
    [Fact]
    public void PrintEquationWithLegend_MathMlRuleWithSpecialCharacters_ParsesAsWellFormedXml_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"a<b&c>\") AND AtLeast(2, b, True, c)");

        EquationOptions options = MathMl with { Constants = new ConstantFormat { TrueText = "<&>" } };

        EquationWithLegend result = rule.PrintEquationWithLegend(options);

        XNamespace mathMl = "http://www.w3.org/1998/Math/MathML";
        Assert.Equal(mathMl + "math", XDocument.Parse(result.Equation).Root!.Name);
        Assert.Equal(mathMl + "math", XDocument.Parse(rule.PrintEquation(options)).Root!.Name);
        foreach (string line in result.LegendText.Split('\n'))
        {
            Assert.Equal(mathMl + "math", XDocument.Parse(line).Root!.Name);
        }
    }

    /// <summary>MathML simple variables are <c>mi</c> letters, the legend lines are MathML, and a twelfth term gets an <c>msub</c> subscript.</summary>
    [Fact]
    public void PrintEquationWithLegend_MathMl_WritesMiVariablesAndMathLegendLines_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"thin\")");
        CompiledRule<RuleTestContext> many = CompileManyTerms();

        EquationWithLegend result = rule.PrintEquationWithLegend(MathMl);
        EquationWithLegend twelve = many.PrintEquationWithLegend(MathMl);

        Assert.Equal($"{MathOpen}<mi>p</mi> <mo>∧</mo> <mi>q</mi></math>", result.Equation);
        Assert.Equal(
            $"{MathOpen}<mi>p</mi> <mo>=</mo> <mtext>a</mtext></math>\n{MathOpen}<mi>q</mi> <mo>=</mo> <mtext>hasCrust(crust: \"thin\")</mtext></math>",
            result.LegendText
        );
        Assert.Contains("<msub><mi>p</mi><mn>1</mn></msub>", twelve.Equation, StringComparison.Ordinal);
    }

    /// <summary>The simple-variable equation of a rule in MathML equals the equation of the legend call.</summary>
    [Fact]
    public void PrintEquation_MathMlSimpleVariables_PrintsLettersWithoutALegend_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a OR b");

        string equation = rule.PrintEquation(MathMl with { SimpleVariables = true });

        Assert.Equal($"{MathOpen}<mo>¬</mo><mi>p</mi> <mo>∨</mo> <mi>q</mi></math>", equation);
    }

    /// <summary>Typst writes terms as quoted strings, operators as Typst symbols and the whole equation between dollar signs.</summary>
    [Fact]
    public void PrintEquation_TypstAndWithOrOperand_PrintsQuotedTermsAndSymbolsBetweenDollars_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string equation = rule.PrintEquation(Typst);

        Assert.Equal("$\"a\" and (\"b\" or \"c\")$", equation);
    }

    /// <summary>Every binary connective prints as its Typst symbol between its operands.</summary>
    [Theory]
    [InlineData("AND", "and")]
    [InlineData("OR", "or")]
    [InlineData("XOR", "xor")]
    [InlineData("EQUIVALENT", "<=>")]
    [InlineData("IMPLIES", "=>")]
    [InlineData("NAND", "arrow.t")]
    [InlineData("NOR", "arrow.b")]
    public void PrintEquation_TypstBinaryConnective_PrintsItsSymbolInfix_Test(string word, string symbol)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation(Typst);

        Assert.Equal($"$\"a\" {symbol} \"b\"$", equation);
    }

    /// <summary>Typst negation is the <c>not</c> symbol before its operand, and a negated group keeps its parentheses.</summary>
    [Fact]
    public void PrintEquation_TypstNot_PrintsTheNotSymbolBeforeTheOperand_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT (a AND b)");

        string equation = rule.PrintEquation(Typst);

        Assert.Equal("$not (\"a\" and \"b\")$", equation);
    }

    /// <summary>Typst writes <c>If</c> and the cardinality operators in function-call form with a quoted name.</summary>
    [Fact]
    public void PrintEquation_TypstFunctionCallOperators_PrintTheCallWithAQuotedName_Test()
    {
        CompiledRule<RuleTestContext> ifRule = Compile("If(a, b, c)");
        CompiledRule<RuleTestContext> atLeast = Compile("AtLeast(2, a, b)");

        Assert.Equal("$\"If\"(\"a\", \"b\", \"c\")$", ifRule.PrintEquation(Typst));
        Assert.Equal("$\"AtLeast\"(2, \"a\", \"b\")$", atLeast.PrintEquation(Typst));
    }

    /// <summary>A double quote and a backslash in a Typst term or override get a backslash before them.</summary>
    [Fact]
    public void PrintEquation_TypstTermAndOverrideWithQuoteAndBackslash_EscapeThem_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND True");
        ConstantFormat constants = new() { TrueText = "a\\b\"c\td" };

        string equation = rule.PrintEquation(Typst with { Constants = constants });

        Assert.Equal("$\"hasCrust(crust: \\\"thin\\\")\" and \"a\\\\b\\\"c\\td\"$", equation);
    }

    /// <summary>Typst writes a constant as a quoted string (letters by default, words on request), and the logic-symbol style as the Typst symbols <c>top</c> and <c>bot</c>.</summary>
    [Fact]
    public void PrintEquation_TypstConstants_PrintQuotedTextOrTypstSymbols_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True AND False AND Unknown");
        ConstantFormat symbols = new() { Style = ConstantStyle.LogicSymbols };

        ConstantFormat words = new() { Style = ConstantStyle.Word };

        Assert.Equal("$\"T\" and \"F\" and \"U\"$", rule.PrintEquation(Typst));
        Assert.Equal("$\"True\" and \"False\" and \"Unknown\"$", rule.PrintEquation(Typst with { Constants = words }));
        Assert.Equal("$top and bot and \"U\"$", rule.PrintEquation(Typst with { Constants = symbols }));
    }

    /// <summary>Typst simple variables are letters, each legend line is its own math, and a twelfth term gets a subscript.</summary>
    [Fact]
    public void PrintEquationWithLegend_Typst_WritesLettersAndMathLegendLines_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"thin\")");
        CompiledRule<RuleTestContext> many = CompileManyTerms();

        EquationWithLegend result = rule.PrintEquationWithLegend(Typst);
        EquationWithLegend twelve = many.PrintEquationWithLegend(Typst);

        Assert.Equal("$p and q$", result.Equation);
        Assert.Equal("$p = \"a\"$\n$q = \"hasCrust(crust: \\\"thin\\\")\"$", result.LegendText);
        Assert.Contains("p_1", twelve.Equation, StringComparison.Ordinal);
        Assert.Equal("$p and q$", rule.PrintEquation(Typst with { SimpleVariables = true }));
    }

    /// <summary>Plain ASCII writes the connectives as <c>/\</c>, <c>\/</c> and <c>~</c>, with parentheses only around a nested connective.</summary>
    [Fact]
    public void PrintEquation_AsciiAndWithOrOperand_PrintsAsciiSymbols_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c) AND NOT d");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal(@"a /\ (b \/ c) /\ ~d", equation);
    }

    /// <summary>Every binary connective prints as its ASCII token between its operands; NAND and NOR keep their words.</summary>
    [Theory]
    [InlineData("AND", @"/\")]
    [InlineData("OR", @"\/")]
    [InlineData("XOR", "(+)")]
    [InlineData("EQUIVALENT", "<->")]
    [InlineData("IMPLIES", "->")]
    [InlineData("NAND", "NAND")]
    [InlineData("NOR", "NOR")]
    public void PrintEquation_AsciiBinaryConnective_PrintsItsTokenInfix_Test(string word, string token)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal($"a {token} b", equation);
    }

    /// <summary>Plain ASCII writes <c>If</c> and the cardinality operators in function-call form, and a negated group keeps its parentheses.</summary>
    [Fact]
    public void PrintEquation_AsciiFunctionCallOperatorsAndNegatedGroup_PrintTheDslSpelling_Test()
    {
        CompiledRule<RuleTestContext> ifRule = Compile("If(a, b, c)");
        CompiledRule<RuleTestContext> atLeast = Compile("AtLeast(2, a, b)");
        CompiledRule<RuleTestContext> notGroup = Compile("NOT (a AND b)");

        Assert.Equal("If(a, b, c)", ifRule.PrintEquation(Ascii));
        Assert.Equal("AtLeast(2, a, b)", atLeast.PrintEquation(Ascii));
        Assert.Equal(@"~(a /\ b)", notGroup.PrintEquation(Ascii));
    }

    /// <summary>Plain ASCII escapes a backslash and every non-ASCII character in an override as the documented rule says.</summary>
    [Fact]
    public void PrintEquation_AsciiOverrideWithNonAsciiAndBackslash_EscapesPerTheDocumentedRule_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True AND False");
        ConstantFormat constants = new() { TrueText = "é✓😀\\" };

        string equation = rule.PrintEquation(Ascii with { Constants = constants });

        Assert.Equal($"{Slash}u00E9{Slash}u2713{Slash}uD83D{Slash}uDE00{Slash}{Slash} /{Slash} False", equation);
    }

    /// <summary>Plain ASCII writes the logic-symbol style as the words <c>top</c> and <c>bot</c>, and the letter style as letters.</summary>
    [Fact]
    public void PrintEquation_AsciiConstantStyles_PrintAsciiText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True AND False AND Unknown");

        Assert.Equal(
            @"top /\ bot /\ U",
            rule.PrintEquation(Ascii with { Constants = new ConstantFormat { Style = ConstantStyle.LogicSymbols } })
        );
        Assert.Equal(
            @"T /\ F /\ U",
            rule.PrintEquation(Ascii with { Constants = new ConstantFormat { Style = ConstantStyle.Letters } })
        );
    }

    /// <summary>Plain ASCII simple variables are letters with an underscore subscript, and the legend lists the escaped terms.</summary>
    [Fact]
    public void PrintEquationWithLegend_Ascii_WritesLettersAndLegendLines_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"thin\")");
        CompiledRule<RuleTestContext> many = CompileManyTerms();

        EquationWithLegend result = rule.PrintEquationWithLegend(Ascii);
        EquationWithLegend twelve = many.PrintEquationWithLegend(Ascii);

        Assert.Equal(@"p /\ q", result.Equation);
        Assert.Equal("p = a\nq = hasCrust(crust: \"thin\")", result.LegendText);
        Assert.Contains("p_1", twelve.Equation, StringComparison.Ordinal);
        Assert.Equal(@"p /\ q", rule.PrintEquation(Ascii with { SimpleVariables = true }));
    }

    /// <summary>The examples in the MathML, Typst and Ascii sections of <c>docs/equations.md</c> print exactly the documented text.</summary>
    [Fact]
    public async Task PrintEquation_DocumentedExamplesOfTheNewDialects_PrintTheDocumentedText_Test()
    {
        string page = await File.ReadAllTextAsync(
            Path.Combine(DocExampleChecker.FindRepositoryRoot(), "docs", "equations.md"),
            TestContext.Current.CancellationToken
        );
        CompiledRule<RuleTestContext> group = Compile("a AND (b OR c)");
        CompiledRule<RuleTestContext> call = Compile("AtLeast(2, a, b)");
        CompiledRule<RuleTestContext> mixed = Compile("a AND (b OR c) AND NOT d");
        CompiledRule<RuleTestContext> accent = Compile("True");

        string[] documented =
        [
            group.PrintEquation(MathMl),
            call.PrintEquation(MathMl),
            group.PrintEquation(Typst),
            call.PrintEquation(Typst),
            mixed.PrintEquation(Ascii),
            call.PrintEquation(Ascii),
            accent.PrintEquation(Ascii with { Constants = new ConstantFormat { TrueText = "café" } }),
        ];

        Assert.All(documented, line => Assert.Contains(line, page, StringComparison.Ordinal));
        Assert.Equal("caf" + Slash + "u00E9", documented[^1]);
    }

    /// <summary>
    /// For any generated rule, any constant style and override (with arbitrary UTF-16 text), and either variable mode, the
    /// ASCII equation and legend hold only the characters U+0020 to U+007E and line feeds.
    /// </summary>
    [Fact]
    public void PrintEquation_AsciiAnyGeneratedRule_ContainsOnlyPrintableAsciiAndNewlines_Test()
    {
        GenTree
            .Select(
                GenConstants,
                Gen.Bool,
                Gen.Bool,
                (tree, constants, showArguments, simple) => (tree, constants, showArguments, simple)
            )
            .Sample(
                sample =>
                {
                    EquationOptions options = Ascii with
                    {
                        Constants = sample.constants,
                        ShowArgumentValues = sample.showArguments,
                        SimpleVariables = sample.simple,
                    };

                    EquationWithLegend legend = EquationPrinter.PrintWithLegend(sample.tree, options);
                    string[] outputs = [EquationPrinter.Print(sample.tree, options), legend.Equation, legend.LegendText];

                    foreach (string output in outputs)
                    {
                        Assert.All(output, c => Assert.True(c is '\n' or (>= ' ' and <= '~'), $"U+{(int)c:X4} in '{output}'"));
                    }
                },
                iter: SampleIterations
            );
    }

    /// <summary>For any generated rule and constant format, the MathML equation and each legend line parse as well-formed XML.</summary>
    [Fact]
    public void PrintEquation_MathMlAnyGeneratedRule_ParsesAsWellFormedXml_Test()
    {
        GenTree
            .Select(GenConstants, Gen.Bool, (tree, constants, showArguments) => (tree, constants, showArguments))
            .Sample(
                sample =>
                {
                    EquationOptions options = MathMl with
                    {
                        Constants = sample.constants,
                        ShowArgumentValues = sample.showArguments,
                    };

                    EquationWithLegend legend = EquationPrinter.PrintWithLegend(sample.tree, options);

                    Assert.NotNull(XDocument.Parse(EquationPrinter.Print(sample.tree, options)).Root);
                    Assert.NotNull(XDocument.Parse(legend.Equation).Root);
                    foreach (string line in legend.LegendText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        Assert.NotNull(XDocument.Parse(line).Root);
                    }
                },
                iter: SampleIterations
            );
    }

    private static Expression TermWithArgument(string name, string value)
    {
        return new TermExpression(
            new TermIdentity(name, [new KeyValuePair<string, LiteralValue>("arg", LiteralValue.OfString(value))])
        );
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        return BuildCompiler().Compile(dsl).CompiledRule!;
    }

    /// <summary>Compiles twelve distinct terms, one more than the eleven letters of simple-variable mode.</summary>
    private static CompiledRule<RuleTestContext> CompileManyTerms()
    {
        string dsl = string.Join(" AND ", Enumerable.Range(1, 12).Select(i => $"hasCrust(crust: \"c{i}\")"));
        return Compile(dsl);
    }

    private static RuleCompiler<RuleTestContext> BuildCompiler()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("d", true)
            .AddStringArgPredicate("hasCrust", "crust", "thin")
            .Build();
        return new RuleCompiler<RuleTestContext>(registry);
    }
}
