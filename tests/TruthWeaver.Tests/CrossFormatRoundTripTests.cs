namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Tool-platform L3: rule text, JSON and YAML convert to each other in all six directions and the result compiles to an
/// equal tree with equal notes, for generated rules that use every operator, every literal kind, variable references
/// and notes. The conversions go through the public helpers <c>Convert</c>, <c>CompileAs</c> and <c>PrintAs</c>.
/// </summary>
public sealed class CrossFormatRoundTripTests
{
    private const int Seeds = 300;

    // Characters that need escaping or quoting in at least one format: quotes, backslash, line feed, tab, YAML indicators.
    private const string StringCharacters = "abcXYZ019 \"\\\n\t\r\u0085\u2028\u0001#:{}[],'é-*&!%@`|>";

    private const string QueryCharacters = "abc01.[]*' \"\\";

    // The extremes of each number kind, where a reader that goes through a narrower type would lose digits.
    private static readonly long[] Int64Edges = [long.MinValue, long.MaxValue, 0, -1, 9_007_199_254_740_993];

    private static readonly decimal[] DecimalEdges =
    [
        decimal.MaxValue,
        decimal.MinValue,
        0.0000000000000000000000000001m,
        -0.0000000000000000000000000001m,
        1.5000m,
        0m,
        12345678901234567890.123456789m,
    ];

    private static readonly RuleFormat[] Formats = [RuleFormat.Text, RuleFormat.Json, RuleFormat.Yaml];

    private static readonly LiteralKind[] Kinds =
    [
        LiteralKind.String,
        LiteralKind.Int64,
        LiteralKind.Decimal,
        LiteralKind.Boolean,
        LiteralKind.DateTimeOffset,
        LiteralKind.Guid,
        LiteralKind.StringArray,
        LiteralKind.Int64Array,
        LiteralKind.DecimalArray,
        LiteralKind.BooleanArray,
        LiteralKind.DateTimeOffsetArray,
        LiteralKind.GuidArray,
    ];

    /// <summary>
    /// For generated rules over many seeds, each of the six conversions between text, JSON and YAML gives a document that
    /// compiles to a tree equal to the original and carries the same notes. The run also checks that the generated rules
    /// used every operator, every literal kind, a variable reference and a note, so the guarantee covers all of them.
    /// </summary>
    [Fact]
    public void Convert_GeneratedRulesInAllSixDirections_CompileToEqualTreesAndNotes_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        HashSet<string> seen = [];

        for (int seed = 0; seed < Seeds; seed++)
        {
            Random random = new(seed);
            NotedRule sample = NotedRuleGenerator.Next(random, NextLeaf);
            Tally(sample.Tree, seen);
            seen.UnionWith(sample.Notes.Count > 0 ? ["note"] : []);
            CompiledRule<RuleTestContext> original = Compile(compiler, sample.NotedText, RuleFormat.Text, seed);
            Assert.Equal(sample.Tree, original.Root);
            Assert.Equal(sample.Notes, original.Notes);

            foreach (RuleFormat from in Formats)
            {
                string source = original.PrintAs(from);
                foreach (RuleFormat to in Formats.Where(format => format != from))
                {
                    RuleConversionResult conversion = compiler.Convert(source, from, to);
                    Assert.True(
                        conversion.Succeeded,
                        $"Seed {seed}, {from} -> {to}: {string.Join("; ", conversion.Diagnostics.Select(d => d.Message))}\n{source}"
                    );

                    CompiledRule<RuleTestContext> converted = Compile(compiler, conversion.Text, to, seed);
                    Assert.True(
                        sample.Tree.Equals(converted.Root),
                        $"Seed {seed}, {from} -> {to}: the tree changed.\n{source}"
                    );
                    Assert.Equal(sample.Notes, converted.Notes);
                }
            }
        }

        string[] expected =
        [
            "note",
            "variable",
            .. OperatorDefinitions.All.Select(definition => definition.OpName),
            .. Kinds.Select(kind => kind.ToString()),
        ];
        Assert.Empty(expected.Except(seen));
    }

    /// <summary><c>Convert</c> of a source that does not compile returns no text and the compile diagnostics.</summary>
    [Fact]
    public void Convert_SourceThatDoesNotCompile_ReturnsNoTextAndTheDiagnostics_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleConversionResult result = compiler.Convert("flag AND (", RuleFormat.Text, RuleFormat.Json);

        Assert.False(result.Succeeded);
        Assert.Null(result.Text);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary><c>TryConvert</c> reports success through its return value and gives the converted text.</summary>
    [Fact]
    public void TryConvert_YamlToText_ReturnsTrueAndTheRuleText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        bool converted = compiler.TryConvert(
            "op: not\nnote: never\noperands:\n  - predicate: flag\n",
            RuleFormat.Yaml,
            RuleFormat.Text,
            out string? text,
            out EquatableArray<Diagnostic> diagnostics
        );

        Assert.True(converted);
        Assert.Equal("/* never */ NOT flag", text);
        Assert.Empty(diagnostics);
    }

    /// <summary><c>TryConvert</c> of a source that does not compile returns false, no text and the diagnostics.</summary>
    [Fact]
    public void TryConvert_SourceThatDoesNotCompile_ReturnsFalse_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        bool converted = compiler.TryConvert(
            """{"op":"not"}""",
            RuleFormat.Json,
            RuleFormat.Yaml,
            out string? text,
            out EquatableArray<Diagnostic> diagnostics
        );

        Assert.False(converted);
        Assert.Null(text);
        Assert.NotEmpty(diagnostics);
    }

    /// <summary><c>Convert</c> with <c>includeNotes: false</c> leaves the notes out of the converted document.</summary>
    [Fact]
    public void Convert_IncludeNotesFalse_LeavesTheNotesOut_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleConversionResult result = compiler.Convert("/* why */ flag", RuleFormat.Text, RuleFormat.Json, includeNotes: false);

        Assert.Equal("""{"predicate":"flag"}""", result.Text);
    }

    /// <summary><c>CompileAs</c> and <c>TryCompileAs</c> read each format the same way as the format's own compile method.</summary>
    [Theory]
    [InlineData(RuleFormat.Text, "NOT flag")]
    [InlineData(RuleFormat.Json, """{"op":"not","operands":[{"predicate":"flag"}]}""")]
    [InlineData(RuleFormat.Yaml, "op: not\noperands:\n  - predicate: flag\n")]
    public void CompileAs_EachFormat_CompilesTheSameRule_Test(RuleFormat format, string source)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.CompileAs(source, format).GetRuleOrThrow();
        bool compiled = compiler.TryCompileAs(source, format, out CompiledRule<RuleTestContext>? tried, out _);

        Assert.Equal("NOT flag", rule.CanonicalText);
        Assert.True(compiled);
        Assert.Equal("NOT flag", tried!.CanonicalText);
    }

    /// <summary><c>PrintAs</c> gives exactly the text of the format's own printer.</summary>
    [Fact]
    public void PrintAs_EachFormat_MatchesTheFormatsOwnPrinter_Test()
    {
        CompiledRule<RuleTestContext> rule = CreateCompiler().Compile("/* why */ (flag AND NOT flag)").GetRuleOrThrow();

        Assert.Equal(rule.PrintRuleText(), rule.PrintAs(RuleFormat.Text));
        Assert.Equal(rule.PrintJson(), rule.PrintAs(RuleFormat.Json));
        Assert.Equal(rule.PrintYaml(), rule.PrintAs(RuleFormat.Yaml));
        Assert.Equal(rule.CanonicalText, rule.PrintAs(RuleFormat.Text, includeNotes: false));
    }

    /// <summary>A format value outside the enum is a programming error, so the helpers throw for it.</summary>
    [Fact]
    public void PrintAsAndCompileAs_UndefinedFormat_ThrowArgumentOutOfRangeException_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = compiler.Compile("flag").GetRuleOrThrow();

        Assert.Throws<ArgumentOutOfRangeException>(() => rule.PrintAs((RuleFormat)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => compiler.CompileAs("flag", (RuleFormat)42));
    }

    private static CompiledRule<RuleTestContext> Compile(
        RuleCompiler<RuleTestContext> compiler,
        string source,
        RuleFormat format,
        int seed
    )
    {
        CompilationResult<RuleTestContext> result = compiler.CompileAs(source, format);
        Assert.True(
            result.Succeeded,
            $"Seed {seed}: {format} did not compile: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}\n{source}"
        );
        return result.Rule;
    }

    /// <summary>Records the operators, literal kinds and variable references that <paramref name="node"/> uses.</summary>
    private static void Tally(Expression node, HashSet<string> seen)
    {
        if (node is TermExpression term)
        {
            seen.UnionWith(term.Identity.Arguments.Select(argument => argument.Value.Kind.ToString()));
            seen.UnionWith(term.Identity.Variables.Count > 0 ? ["variable"] : []);
            return;
        }

        if (node is ConstantExpression)
        {
            return;
        }

        NodeShape shape = ExpressionShape.Of(node);
        seen.Add(shape.OpName);
        foreach (Expression operand in shape.Operands)
        {
            Tally(operand, seen);
        }
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder.Add(
            PredicateSchema.NoArguments("flag", "flag", "A predicate with no arguments."),
            (_, _, _) => ValueTask.FromResult(TruthValue.True)
        );
        foreach (LiteralKind kind in Kinds)
        {
            builder.Add(
                new PredicateSchema(
                    TermName(kind),
                    TermName(kind),
                    $"A predicate with one argument of kind {kind}.",
                    [new PredicateArgumentSchema("value", "The generated value.", kind)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            );
        }

        return new RuleCompiler<RuleTestContext>(builder.Build(), new CompilerOptions(DataSources: ["user", "request"]));
    }

    private static string TermName(LiteralKind kind)
    {
        return $"term{kind}";
    }

    /// <summary>A constant, the argument-free term, a term with a literal of a random kind, or a term with a variable.</summary>
    private static Expression NextLeaf(Random random)
    {
        LiteralKind kind = Kinds[random.Next(Kinds.Length)];
        return random.Next(6) switch
        {
            0 => new ConstantExpression((TruthValue)random.Next(3)),
            1 => new TermExpression(new TermIdentity("flag", [])),
            2 => new TermExpression(
                new TermIdentity(
                    TermName(kind),
                    [],
                    [
                        new KeyValuePair<string, VariableReference>(
                            "value",
                            new VariableReference(random.Next(2) == 0 ? "user" : "request", NextQuery(random))
                        ),
                    ]
                )
            ),
            _ => new TermExpression(
                new TermIdentity(TermName(kind), [new KeyValuePair<string, LiteralValue>("value", NextLiteral(random, kind))])
            ),
        };
    }

    private static string NextQuery(Random random)
    {
        return "$." + NextText(random, QueryCharacters, 6);
    }

    private static string NextText(Random random, string characters, int maxLength)
    {
        return string.Concat(
            Enumerable.Range(0, random.Next(maxLength + 1)).Select(_ => characters[random.Next(characters.Length)])
        );
    }

    private static LiteralValue NextLiteral(Random random, LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String => LiteralValue.OfString(NextText(random, StringCharacters, 8)),
            LiteralKind.Int64 => LiteralValue.OfInt64(
                random.Next(5) == 0
                    ? Int64Edges[random.Next(Int64Edges.Length)]
                    : random.NextInt64(-1_000_000_000_000, 1_000_000_000_000)
            ),
            LiteralKind.Decimal => LiteralValue.OfDecimal(
                random.Next(5) == 0
                    ? DecimalEdges[random.Next(DecimalEdges.Length)]
                    : decimal.Divide(random.NextInt64(-10_000_000, 10_000_000), (decimal)Math.Pow(10, random.Next(5)))
            ),
            LiteralKind.Boolean => LiteralValue.OfBoolean(random.Next(2) == 0),
            LiteralKind.DateTimeOffset => LiteralValue.OfDateTimeOffset(
                new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromMinutes(15 * random.Next(-56, 57))).AddTicks(
                    random.NextInt64(0, TimeSpan.TicksPerDay * 365 * 50)
                )
            ),
            LiteralKind.Guid => LiteralValue.OfGuid(NextGuid(random)),
            LiteralKind.StringArray => NextArray(random, LiteralKind.String),
            LiteralKind.Int64Array => NextArray(random, LiteralKind.Int64),
            LiteralKind.DecimalArray => NextArray(random, LiteralKind.Decimal),
            LiteralKind.BooleanArray => NextArray(random, LiteralKind.Boolean),
            LiteralKind.DateTimeOffsetArray => NextArray(random, LiteralKind.DateTimeOffset),
            _ => NextArray(random, LiteralKind.Guid),
        };
    }

    private static LiteralValue NextArray(Random random, LiteralKind elementKind)
    {
        return LiteralValue.OfArray(
            elementKind,
            [.. Enumerable.Range(0, random.Next(4)).Select(_ => NextLiteral(random, elementKind))]
        );
    }

    private static Guid NextGuid(Random random)
    {
        byte[] bytes = new byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }
}
