namespace TruthWeaver.Tests;

using CsCheck;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket: property-based round-trip testing for the DSL printer/parser. Generalizes
/// <see cref="CanonicalPrinterTests"/>'s hand-picked examples across the whole
/// <see cref="Expression"/> shape space: for any randomly generated valid tree,
/// <c>parse(print(tree))</c> must be structurally equal to <c>tree</c>. Uses CsCheck for generation
/// and shrinking (see <c>Directory.Packages.props</c> for why CsCheck was chosen).
/// </summary>
public sealed class DslRoundTripPropertyTests
{
    /// <summary>
    /// The generator's recursion cutoff. Bounded well under <see cref="CompilerOptions"/>'s default
    /// 32/512 depth/node limits so every generated tree is one the default compiler accepts outright
    /// - this suite is about round-tripping, not about exercising the resource-limit diagnostics.
    /// </summary>
    private const int MaxTreeDepth = 4;

    /// <summary>The bounded sample size - enough to exercise every shape below, with no unbounded/flaky runtime.</summary>
    private const int SampleIterations = 300;

    private static readonly IReadOnlyList<TermSpec> TermSpecs =
    [
        new TermSpec("flag", null, null),
        new TermSpec("termString", "value", LiteralKind.String),
        new TermSpec("termInt64", "value", LiteralKind.Int64),
        new TermSpec("termDecimal", "value", LiteralKind.Decimal),
        new TermSpec("termBoolean", "value", LiteralKind.Boolean),
        new TermSpec("termDateTimeOffset", "value", LiteralKind.DateTimeOffset),
        new TermSpec("termGuid", "value", LiteralKind.Guid),
        new TermSpec("termStringArray", "value", LiteralKind.StringArray),
        new TermSpec("termInt64Array", "value", LiteralKind.Int64Array),
        new TermSpec("termDecimalArray", "value", LiteralKind.DecimalArray),
        new TermSpec("termBooleanArray", "value", LiteralKind.BooleanArray),
        new TermSpec("termDateTimeOffsetArray", "value", LiteralKind.DateTimeOffsetArray),
        new TermSpec("termGuidArray", "value", LiteralKind.GuidArray),
    ];

    /// <summary>
    /// A "safe-ish" DSL string character set that still exercises the printer/lexer's quote and
    /// backslash escaping (<see cref="LiteralValue"/>'s <c>EscapeForDsl</c> and <c>Lexer.ReadString</c>),
    /// plus a raw newline/tab (valid, unescaped, inside a DSL quoted string).
    /// </summary>
    private static readonly Gen<char> GenStringChar = Gen.Frequency(
        (85, Gen.Char.AlphaNumeric),
        (3, Gen.Const(' ')),
        (3, Gen.Const('"')),
        (3, Gen.Const('\\')),
        (3, Gen.Const('\n')),
        (3, Gen.Const('\t'))
    );

    private static readonly Gen<string> GenString = Gen.String[GenStringChar, 0, 10];

    private static readonly Gen<Expression> GenConstant = Gen.Enum<TruthValue>()
        .Select(value => (Expression)new ConstantExpression(value));

    private static readonly Gen<Expression> GenTerm = Gen.OneOf([.. TermSpecs.Select(BuildTermGen)]);

    private static readonly Gen<Expression> GenLeaf = Gen.OneOf(GenConstant, GenTerm);

    /// <summary>
    /// The full recursive expression generator - every operator (<c>AND</c>/<c>OR</c>/<c>NOT</c>/
    /// <c>XOR</c>/<c>EQUIVALENT</c>/<c>ExactlyOne</c>/the threshold family) plus leaves, arity/threshold-range
    /// constrained to mirror <c>RuleNodeCompiler</c>'s own validation exactly, so no generated tree is
    /// ever rejected for a reason unrelated to round-tripping.
    /// </summary>
    private static readonly Gen<Expression> GenExpressionTree = Gen.Recursive<Expression>(
        (depth, self) =>
        {
            if (depth >= MaxTreeDepth)
            {
                return GenLeaf;
            }

            Gen<Expression> genAnd = self.Array[2, 4]
                .Select(operands => (Expression)new AndExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genOr = self.Array[2, 4]
                .Select(operands => (Expression)new OrExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genNot = self.Select(operand => (Expression)new NotExpression(operand));
            Gen<Expression> genXor = self.Select(self, (left, right) => (Expression)new XorExpression(left, right));
            Gen<Expression> genXnor = self.Select(self, (left, right) => (Expression)new EquivalentExpression(left, right));
            Gen<Expression> genImplies = self.Select(self, (left, right) => (Expression)new ImpliesExpression(left, right));
            Gen<Expression> genNand = self.Select(self, (left, right) => (Expression)new NandExpression(left, right));
            Gen<Expression> genNor = self.Select(self, (left, right) => (Expression)new NorExpression(left, right));
            Gen<Expression> genParity = self.Array[2, 4]
                .Select(operands => (Expression)new ParityExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genAny = self.Array[2, 4]
                .Select(operands => (Expression)new AnyExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genAll = self.Array[2, 4]
                .Select(operands => (Expression)new AllExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genNone = self.Array[2, 4]
                .Select(operands => (Expression)new NoneExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genExactlyOne = self.Array[2, 4]
                .Select(operands => (Expression)new ExactlyOneExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genThreshold = BuildThresholdGen(self);
            Gen<Expression> genBetween = BuildBetweenGen(self);
            Gen<Expression> genCoalesce = self.Array[2, 4]
                .Select(operands => (Expression)new CoalesceExpression(new EquatableArray<Expression>(operands)));

            Gen<Expression> genInspection = Gen.Enum<InspectionKind>()
                .Select(self, (kind, operand) => (Expression)new InspectionExpression(kind, operand));
            Gen<Expression> genIf = self.Array[3]
                .Select(operands => (Expression)new IfExpression(operands[0], operands[1], operands[2]));

            return Gen.Frequency(
                (3, GenLeaf),
                (2, genAnd),
                (2, genOr),
                (2, genNot),
                (1, genXor),
                (1, genXnor),
                (1, genImplies),
                (1, genNand),
                (1, genNor),
                (1, genParity),
                (1, genAny),
                (1, genAll),
                (1, genNone),
                (1, genExactlyOne),
                (1, genThreshold),
                (1, genBetween),
                (1, genCoalesce),
                (1, genIf),
                (1, genInspection)
            );
        }
    );

    /// <summary>Parsing the printed form of a generated tree reproduces a structurally equal tree.</summary>
    [Fact]
    public void Parse_PrintedGeneratedTree_ReproducesEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree.Sample(
            tree =>
            {
                string printed = CanonicalPrinter.Print(tree);
                CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));
                string failureMessage = $"Expected '{printed}' to compile cleanly but got: {diagnosticMessages}";

                Assert.True(result.Succeeded, failureMessage);
                Assert.Equal(tree, result.CompiledRule!.Root);
            },
            iter: SampleIterations
        );
    }

    /// <summary>
    /// Ticket 06: <c>PrintRuleText(operatorStyle: OperatorStyle.Symbolic)</c> parses back to the same tree as the
    /// word form, for any generated tree - the symbols it writes for <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/
    /// <c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c> are exactly the ones the DSL lexer reads back for them.
    /// </summary>
    [Fact]
    public void Parse_SymbolicPrintedGeneratedTree_ReproducesEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree.Sample(
            tree =>
            {
                string printed = CanonicalPrinter.Print(tree, GroupingStyle.Parentheses, OperatorStyle.Symbolic);
                CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));
                string failureMessage = $"Expected '{printed}' to compile cleanly but got: {diagnosticMessages}";

                Assert.True(result.Succeeded, failureMessage);
                Assert.Equal(tree, result.CompiledRule!.Root);
            },
            iter: SampleIterations
        );
    }

    /// <summary>
    /// Followups C: printing a generated tree with <see cref="ConstantStyle.Letters"/> (T, F, U) compiles back to an equal
    /// tree, in both operator styles.
    /// </summary>
    [Fact]
    public void Parse_LettersPrintedGeneratedTree_ReproducesEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());
        ConstantFormat letters = new() { Style = ConstantStyle.Letters };

        GenExpressionTree.Sample(
            tree =>
            {
                foreach (OperatorStyle style in (OperatorStyle[])[OperatorStyle.Word, OperatorStyle.Symbolic])
                {
                    string printed = CanonicalPrinter.Print(tree, GroupingStyle.Parentheses, style, letters);
                    CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                    string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));

                    Assert.True(result.Succeeded, $"Expected '{printed}' to compile cleanly but got: {diagnosticMessages}");
                    Assert.Equal(tree, result.CompiledRule!.Root);
                }
            },
            iter: SampleIterations
        );
    }

    /// <summary>
    /// Ticket 21: the depth-cycling rendering, which mixes <c>()</c>, <c>[]</c> and <c>{}</c>, parses back to the same
    /// tree as the parentheses-only form for any generated tree (delimiters never change the compiled tree).
    /// </summary>
    [Fact]
    public void Parse_DepthCycledPrintOfGeneratedTree_ReproducesStructurallyEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree.Sample(
            tree =>
            {
                string printed = CanonicalPrinter.Print(tree, GroupingStyle.DepthCycling);
                CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));

                Assert.True(result.Succeeded, $"Expected '{printed}' to compile cleanly but got: {diagnosticMessages}");
                Assert.Equal(tree, result.CompiledRule!.Root);
            },
            iter: SampleIterations
        );
    }

    /// <summary>
    /// Ticket 22: padding the printed text of a generated tree with random whitespace (including tabs and newlines) around
    /// its punctuation never changes the compiled tree, and normalising the padded text gives the same text as normalising
    /// the tidy print.
    /// </summary>
    [Fact]
    public void Normalise_WhitespacePaddedPrintOfGeneratedTree_MatchesTidyPrintAndReparsesEqual_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree
            .Select(Gen.Int, (tree, seed) => (Tree: tree, Seed: seed))
            .Sample(
                sample =>
                {
                    string tidy = CanonicalPrinter.Print(sample.Tree);
                    string padded = PadWhitespace(tidy, new Random(sample.Seed));
                    string normalized = RuleText.NormalizeWhitespace(padded);

                    Assert.Equal(RuleText.NormalizeWhitespace(tidy), normalized);
                    CompilationResult<RuleTestContext> result = compiler.Compile(padded);
                    Assert.True(result.Succeeded, $"Expected padded '{padded}' to compile cleanly.");
                    Assert.Equal(sample.Tree, result.CompiledRule!.Root);
                    Assert.Equal(sample.Tree, compiler.Compile(normalized).CompiledRule!.Root);
                },
                iter: SampleIterations
            );
    }

    /// <summary>
    /// Ticket 10: for a generated tree and every width in {1, 10, 20, 40, 80, null}, <c>PrintRuleText(RuleTextOptions)</c>
    /// compiles back to a tree equal to the original - the same round-trip guarantee as the one-line printer, now
    /// checked across widths small enough to force breaking and large enough that nothing breaks.
    /// </summary>
    [Fact]
    public void PrintRuleText_WidthAwareOptionsAtSeveralWidths_CompilesBackToAnEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());
        int?[] widths = [1, 10, 20, 40, 80, null];

        GenExpressionTree.Sample(
            tree =>
            {
                CompiledRule<RuleTestContext> rule = compiler.Compile(CanonicalPrinter.Print(tree)).CompiledRule!;
                foreach (int? width in widths)
                {
                    string printed = rule.PrintRuleText(RuleTextOptions.Default with { Width = width });
                    CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                    string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));

                    Assert.True(
                        result.Succeeded,
                        $"Width {width}: expected '{printed}' to compile cleanly but got: {diagnosticMessages}"
                    );
                    Assert.Equal(tree, result.CompiledRule!.Root);
                }
            },
            iter: SampleIterations
        );
    }

    /// <summary>
    /// Ticket 10: a width of 1 - too small to fit even the shortest token - breaks every group it can but never loops,
    /// throws, or splits a token; the output still compiles back to the original tree.
    /// </summary>
    [Fact]
    public void PrintRuleText_WidthOfOne_DoesNotLoopOrThrowAndStillRoundTrips_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());
        CompilationResult<RuleTestContext> compiled = compiler.Compile(
            "flag AND (termString(value: \"x\") OR (termInt64(value: 1) AND termBoolean(value: true))) "
                + "AND AtLeast(2, flag, termBoolean(value: true), termInt64(value: 1))"
        );
        Assert.True(compiled.Succeeded, string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
        CompiledRule<RuleTestContext> rule = compiled.CompiledRule!;

        string printed = rule.PrintRuleText(RuleTextOptions.Default with { Width = 1 });
        CompilationResult<RuleTestContext> result = compiler.Compile(printed);

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(rule.Root, result.CompiledRule!.Root);
    }

    /// <summary>
    /// Ticket 10: a term name longer than the requested width is printed whole, on its own line, rather than split -
    /// the unbreakable-token guarantee that keeps a small width from looping or throwing.
    /// </summary>
    [Fact]
    public void PrintRuleText_TermLongerThanWidth_PrintsTheWholeTermOnOneLine_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        const string LongName = "thisPredicateNameIsDeliberatelyMuchLongerThanAnySensibleWidth";
        builder.Add(
            PredicateSchema.NoArguments(LongName, LongName, "A deliberately long name."),
            (_, _, _) => ValueTask.FromResult(TruthValue.True)
        );
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());
        CompiledRule<RuleTestContext> rule = compiler.Compile($"{LongName} AND {LongName}").CompiledRule!;

        string printed = rule.PrintRuleText(RuleTextOptions.Default with { Width = 5 });

        Assert.Contains(LongName, printed, StringComparison.Ordinal);
        foreach (string line in printed.Split('\n'))
        {
            Assert.DoesNotContain("...", line, StringComparison.Ordinal);
        }

        CompilationResult<RuleTestContext> result = compiler.Compile(printed);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(rule.Root, result.CompiledRule!.Root);
    }

    /// <summary>
    /// Ticket 10: printing the already-broken text's recompiled tree again at the same width gives the identical text -
    /// the pretty printer is idempotent, not just round-trippable.
    /// </summary>
    [Fact]
    public void PrintRuleText_WidthAwareOutputRecompiledAndReprinted_ProducesIdenticalText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());
        CompilationResult<RuleTestContext> compiled = compiler.Compile(
            "flag AND (termString(value: \"x\") OR (termInt64(value: 1) AND termBoolean(value: true))) "
                + "AND AtLeast(2, flag, termBoolean(value: true), termInt64(value: 1))"
        );
        Assert.True(compiled.Succeeded, string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
        CompiledRule<RuleTestContext> rule = compiled.CompiledRule!;

        RuleTextOptions options = RuleTextOptions.Default with { Width = 20 };
        string firstPrint = rule.PrintRuleText(options);
        CompiledRule<RuleTestContext> reparsed = compiler.Compile(firstPrint).CompiledRule!;
        string secondPrint = reparsed.PrintRuleText(options);

        Assert.Equal(firstPrint, secondPrint);
    }

    /// <summary>
    /// Surrounds every punctuation character outside a string literal, and replaces every space outside one, with a random
    /// run of whitespace. String literal contents are copied untouched because whitespace inside them is data.
    /// </summary>
    private static string PadWhitespace(string text, Random random)
    {
        const string Whitespace = " \t\r\n";
        string RandomRun()
        {
            return string.Concat(
                Enumerable.Range(0, random.Next(0, 4)).Select(_ => Whitespace[random.Next(Whitespace.Length)])
            );
        }

        System.Text.StringBuilder builder = new();
        bool inString = false;
        bool escaped = false;
        foreach (char c in text)
        {
            if (inString)
            {
                builder.Append(c);

                // A backslash escapes the next character, so an escaped quote does not end the literal.
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == ' ')
            {
                builder.Append(' ').Append(RandomRun());
            }
            else if ("(),:[]".Contains(c))
            {
                builder.Append(RandomRun()).Append(c).Append(RandomRun());
            }
            else
            {
                builder.Append(c);
                inString = c == '"';
            }
        }

        return builder.ToString();
    }

    private static Gen<LiteralValue> GenLiteralValue(LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String => GenString.Select(LiteralValue.OfString),
            LiteralKind.Int64 => Gen.Long[-1_000_000, 1_000_000].Select(LiteralValue.OfInt64),
            LiteralKind.Decimal => Gen.Decimal[-100_000m, 100_000m].Select(LiteralValue.OfDecimal),
            LiteralKind.Boolean => Gen.Bool.Select(LiteralValue.OfBoolean),
            LiteralKind.DateTimeOffset => Gen.DateTimeOffset.Select(LiteralValue.OfDateTimeOffset),
            LiteralKind.Guid => Gen.Guid.Select(LiteralValue.OfGuid),
            LiteralKind.StringArray => GenLiteralArray(LiteralKind.String),
            LiteralKind.Int64Array => GenLiteralArray(LiteralKind.Int64),
            LiteralKind.DecimalArray => GenLiteralArray(LiteralKind.Decimal),
            LiteralKind.BooleanArray => GenLiteralArray(LiteralKind.Boolean),
            LiteralKind.DateTimeOffsetArray => GenLiteralArray(LiteralKind.DateTimeOffset),
            LiteralKind.GuidArray => GenLiteralArray(LiteralKind.Guid),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled literal kind."),
        };
    }

    private static Gen<LiteralValue> GenLiteralArray(LiteralKind elementKind)
    {
        return GenLiteralValue(elementKind).Array[0, 4].Select(items => LiteralValue.OfArray(elementKind, items));
    }

    private static Gen<Expression> BuildTermGen(TermSpec spec)
    {
        if (spec.ArgumentKind is not { } kind)
        {
            return Gen.Const((Expression)new TermExpression(new TermIdentity(spec.PredicateName, [])));
        }

        return GenLiteralValue(kind)
            .Select(value =>
            {
                TermIdentity identity = new(
                    spec.PredicateName,
                    [new KeyValuePair<string, LiteralValue>(spec.ArgumentName!, value)]
                );
                return (Expression)new TermExpression(identity);
            });
    }

    /// <summary>
    /// The threshold value range that keeps <c>k</c> compile-valid for a given comparison and operand
    /// count - deliberately mirrors <c>RuleNodeCompiler.ValidThresholdRange</c> (private there), kept
    /// in sync intentionally rather than shared, since this is generator-side test scaffolding rather
    /// than production logic.
    /// </summary>
    private static (int MinK, int MaxK) ValidThresholdRange(ThresholdComparison comparison, int operandCount)
    {
        return comparison switch
        {
            ThresholdComparison.AtLeast => (1, operandCount),
            ThresholdComparison.AtMost => (0, operandCount - 1),
            ThresholdComparison.GreaterThan => (0, operandCount - 1),
            ThresholdComparison.LessThan => (1, operandCount),
            ThresholdComparison.Exactly => (0, operandCount),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unhandled threshold comparison."),
        };
    }

    private static Gen<Expression> BuildThresholdGen(Gen<Expression> operandGen)
    {
        return Gen.Enum<ThresholdComparison>()
            .SelectMany(comparison =>
                Gen.Int[1, 4]
                    .SelectMany(operandCount =>
                    {
                        (int minK, int maxK) = ValidThresholdRange(comparison, operandCount);
                        return Gen.Int[minK, maxK]
                            .Select(
                                operandGen.Array[operandCount],
                                (k, operands) =>
                                    (Expression)new ThresholdExpression(comparison, k, new EquatableArray<Expression>(operands))
                            );
                    })
            );
    }

    /// <summary>Generates BETWEEN nodes whose bounds satisfy <c>0 &lt;= min &lt;= max &lt;= n</c>, excluding the rejected full range.</summary>
    private static Gen<Expression> BuildBetweenGen(Gen<Expression> operandGen)
    {
        return Gen.Int[2, 4]
            .SelectMany(operandCount =>
                Gen.Int[0, operandCount]
                    .SelectMany(min =>
                        Gen.Int[min, operandCount]
                            .Where(max => !(min == 0 && max == operandCount))
                            .Select(
                                operandGen.Array[operandCount],
                                (max, operands) =>
                                    (Expression)new BetweenExpression(min, max, new EquatableArray<Expression>(operands))
                            )
                    )
            );
    }

    private static PredicateRegistry<RuleTestContext> BuildRegistry()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (TermSpec spec in TermSpecs)
        {
            PredicateSchema schema;
            if (spec.ArgumentKind is { } kind)
            {
                schema = new PredicateSchema(
                    spec.PredicateName,
                    spec.PredicateName,
                    $"Property-test predicate '{spec.PredicateName}', argument of kind '{kind}'.",
                    [new PredicateArgumentSchema(spec.ArgumentName!, "Generated argument.", kind)]
                );
            }
            else
            {
                schema = PredicateSchema.NoArguments(
                    spec.PredicateName,
                    spec.PredicateName,
                    $"Property-test predicate '{spec.PredicateName}'."
                );
            }

            builder.Add(schema, (_, _, _) => ValueTask.FromResult(true ? TruthValue.True : TruthValue.False));
        }

        return builder.Build();
    }

    private sealed record TermSpec(string PredicateName, string? ArgumentName, LiteralKind? ArgumentKind);
}
