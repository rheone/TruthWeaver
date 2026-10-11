namespace TruthWeaver.Tests;

using System.Text.Json;
using global::Json.Schema;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Tool-platform L1: an optional note on any node, written as a <c>//</c> or <c>/* */</c> comment in rule text and as a
/// <c>note</c> property in JSON and YAML. Notes ride beside the compiled tree: they never change the tree, the compile
/// result or evaluation, rewrites drop them, and the text, JSON and YAML printers write them unless asked not to.
/// </summary>
public sealed class NodeNoteTests
{
    /// <summary>A block comment before a node is that node's note, keyed by the node's tree path.</summary>
    [Fact]
    public void Compile_LeadingBlockComment_AttachesNoteToFollowingNode_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND /* why b */ b");

        Assert.Equal(new Dictionary<string, string> { ["$.operands[1]"] = "why b" }, rule.Notes);
    }

    /// <summary>A line comment before a group is the note of the grouped node, here the root.</summary>
    [Fact]
    public void Compile_LeadingLineCommentBeforeGroup_AttachesNoteToGroupedNode_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("// the whole rule\n(a AND b)");

        Assert.Equal(new Dictionary<string, string> { ["$"] = "the whole rule" }, rule.Notes);
    }

    /// <summary>A leading comment attaches to the smallest node that starts at the next token, not to its parent chain.</summary>
    [Fact]
    public void Compile_LeadingCommentBeforeFirstOperand_AttachesNoteToTheOperand_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("/* about a */ a AND b");

        Assert.Equal(new Dictionary<string, string> { ["$.operands[0]"] = "about a" }, rule.Notes);
    }

    /// <summary>A comment after a node on the same line is that node's note.</summary>
    [Theory]
    [InlineData("a AND b // about b", "$.operands[1]")]
    [InlineData("(a AND b) // about the group", "$")]
    [InlineData("a /* about a */ AND b", "$.operands[0]")]
    [InlineData("a // about a\nAND b", "$.operands[0]")]
    public void Compile_TrailingComment_AttachesNoteToNodeOnTheSameLine_Test(string text, string path)
    {
        CompiledRule<RuleTestContext> rule = Compile(text);

        Assert.Equal([path], rule.Notes.Keys);
    }

    /// <summary>Several comments on one node join, in source order, with a line feed.</summary>
    [Fact]
    public void Compile_SeveralCommentsOnOneNode_JoinsThemWithLineFeeds_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("// first\n// second\na");

        Assert.Equal("first\nsecond", rule.Notes["$"]);
    }

    /// <summary>A comment that neither precedes nor follows a node is the note of the node that encloses it.</summary>
    [Fact]
    public void Compile_CommentInsideACallBeforeItsBound_AttachesNoteToTheCall_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("AtLeast(/* two of three */ 2, a, b, c)");

        Assert.Equal(new Dictionary<string, string> { ["$"] = "two of three" }, rule.Notes);
    }

    /// <summary>A block comment with no closing <c>*/</c> is a syntax error.</summary>
    [Fact]
    public void Compile_UnterminatedBlockComment_ReportsSyntaxError_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("a AND /* never closed b");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>A rule with notes compiles to the same tree and canonical text as the rule without them.</summary>
    [Fact]
    public void Compile_RuleWithAndWithoutNotes_GivesEqualTreesAndCanonicalText_Test()
    {
        CompiledRule<RuleTestContext> plain = Compile("a AND (b OR c)");

        CompiledRule<RuleTestContext> noted = Compile("// root\n(a AND /* group */ (b OR /* c */ c))");

        Assert.Equal(plain.Root, noted.Root);
        Assert.Equal(plain.CanonicalText, noted.CanonicalText);
        Assert.Empty(plain.Notes);
        Assert.Equal(3, noted.Notes.Count);
    }

    /// <summary>The JSON <c>note</c> property is read onto the node it sits on.</summary>
    [Fact]
    public void CompileJson_NoteProperty_AttachesNoteToTheNode_Test()
    {
        CompiledRule<RuleTestContext> rule = CreateCompiler()
            .CompileJson("""{"op":"and","note":"root","operands":[{"predicate":"a"},{"predicate":"b","note":"about b"}]}""")
            .GetRuleOrThrow();

        Assert.Equal(new Dictionary<string, string> { ["$"] = "root", ["$.operands[1]"] = "about b" }, rule.Notes);
    }

    /// <summary>A JSON <c>note</c> that is not a string is a malformed tree.</summary>
    [Fact]
    public void CompileJson_NonStringNote_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().CompileJson("""{"predicate":"a","note":42}""");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$.note", diagnostic.Path);
    }

    /// <summary>
    /// Every format normalizes a note the same way: line endings become a line feed, lines lose trailing whitespace, the
    /// note is trimmed, and a note of only whitespace is no note.
    /// </summary>
    [Fact]
    public void CompileJson_NoteWithUntidyWhitespace_IsNormalizedAndBlankNoteIsDropped_Test()
    {
        CompiledRule<RuleTestContext> rule = CreateCompiler()
            .CompileJson(
                """{"op":"or","operands":[{"predicate":"a","note":"  first  \r\n  second  "},{"predicate":"b","note":" \n "}]}"""
            )
            .GetRuleOrThrow();

        Assert.Equal(new Dictionary<string, string> { ["$.operands[0]"] = "first\n  second" }, rule.Notes);
    }

    /// <summary><see cref="RuleBuilder.WithNote"/> rejects a null note.</summary>
    [Fact]
    public void WithNote_NullNote_ThrowsArgumentNullException_Test()
    {
        RuleBuilder builder = RuleBuilder.Predicate("a");

        Assert.Throws<ArgumentNullException>(() => builder.WithNote(null!));
    }

    /// <summary>The YAML <c>note</c> key is read onto the node it sits on.</summary>
    [Fact]
    public void CompileYaml_NoteKey_AttachesNoteToTheNode_Test()
    {
        const string Yaml = """
            op: or
            operands:
              - const: false
                note: never true
              - predicate: a
            """;

        CompiledRule<RuleTestContext> rule = CreateCompiler().CompileYaml(Yaml).GetRuleOrThrow();

        Assert.Equal(new Dictionary<string, string> { ["$.operands[0]"] = "never true" }, rule.Notes);
    }

    /// <summary>A note survives text to JSON to YAML and back to text, on every node it was written on.</summary>
    [Fact]
    public void Print_NotesThroughTextJsonYamlAndBack_KeepsEveryNote_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> original = Compile(
            "// root\n(/* a */ a AND NOT /* b */ b AND ANY(/* inner */ (c OR d), /* e */ e))"
        );

        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(original.PrintJson()).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromYaml = compiler.CompileYaml(fromJson.PrintYaml()).GetRuleOrThrow();
        CompiledRule<RuleTestContext> fromText = compiler.Compile(fromYaml.PrintRuleText()).GetRuleOrThrow();

        Assert.Equal(5, original.Notes.Count);
        Assert.Equal(original.Notes, fromJson.Notes);
        Assert.Equal(original.Notes, fromYaml.Notes);
        Assert.Equal(original.Notes, fromText.Notes);
        Assert.Equal(original.Root, fromText.Root);
    }

    /// <summary>
    /// <c>PrintRuleText</c> writes notes as leading block comments and puts parentheses around a noted infix node, so the
    /// comment attaches to that node when the text is read back; <c>CanonicalText</c> never carries notes.
    /// </summary>
    [Fact]
    public void PrintRuleText_RuleWithNotes_WritesLeadingCommentsAndKeepsCanonicalTextPlain_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("// root\n(a AND /* about b */ b)");

        Assert.Equal("/* root */ (a AND /* about b */ b)", rule.PrintRuleText());
        Assert.Equal("a AND b", rule.CanonicalText);
    }

    /// <summary><see cref="RuleTextOptions.IncludeNotes"/> set to false prints the rule without its notes.</summary>
    [Fact]
    public void PrintRuleText_IncludeNotesFalse_OmitsNotes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND /* about b */ b");

        string printed = rule.PrintRuleText(RuleTextOptions.Default with { IncludeNotes = false });

        Assert.Equal("a AND b", printed);
    }

    /// <summary>A note that contains <c>*/</c> prints as line comments, one per line, and still reads back unchanged.</summary>
    [Fact]
    public void PrintRuleText_NoteContainingBlockCommentEnd_PrintsLineCommentsAndRoundTrips_Test()
    {
        CompiledRule<RuleTestContext> rule = CreateCompiler()
            .CompileJson("""{"op":"and","operands":[{"predicate":"a","note":"x */ y\n  second line"},{"predicate":"b"}]}""")
            .GetRuleOrThrow();

        string printed = rule.PrintRuleText();
        CompiledRule<RuleTestContext> reparsed = Compile(printed);

        Assert.Equal("// x */ y\n//   second line\na AND b", printed);
        Assert.Equal(rule.Notes, reparsed.Notes);
    }

    /// <summary>Width-aware printing keeps the notes and still reads back to the same tree and notes.</summary>
    [Fact]
    public void PrintRuleText_WidthAwareWithNotes_RoundTripsTreeAndNotes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "// root\n(a AND /* group */ (b OR /* c */ c OR d) AND AtLeast(2, a, /* b again */ b, c))"
        );

        string printed = rule.PrintRuleText(RuleTextOptions.Default with { Width = 10 });
        CompiledRule<RuleTestContext> reparsed = Compile(printed);

        Assert.Equal(rule.Root, reparsed.Root);
        Assert.Equal(rule.Notes, reparsed.Notes);
    }

    /// <summary><c>PrintJson</c> and <c>PrintYaml</c> write notes by default and leave them out when asked.</summary>
    [Fact]
    public void PrintJsonAndPrintYaml_IncludeNotesOption_ControlsTheNoteProperty_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND /* about b */ b");

        Assert.Contains("\"note\":\"about b\"", rule.PrintJson(), StringComparison.Ordinal);
        Assert.DoesNotContain("note", rule.PrintJson(includeNotes: false), StringComparison.Ordinal);
        Assert.Contains("note: \"about b\"", rule.PrintYaml(), StringComparison.Ordinal);
        Assert.DoesNotContain("note", rule.PrintYaml(includeNotes: false), StringComparison.Ordinal);
    }

    /// <summary>Rewrites return rules without notes: the rewritten tree has different nodes, so the notes are dropped.</summary>
    [Fact]
    public void Rewrite_RuleWithNotes_DropsTheNotes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("// root\n(a AND /* about b */ b)");

        Assert.Empty(rule.Canonicalize().GetRuleOrThrow().Notes);
        Assert.Empty(rule.Simplify().GetRuleOrThrow().Notes);
        Assert.Empty(rule.ExpandToPrimitives().GetRuleOrThrow().Notes);
        Assert.Empty(rule.OptimizeOrder().GetRuleOrThrow().Notes);
    }

    /// <summary><see cref="RuleBuilder.WithNote"/> attaches a note to the builder's node in the compiled rule.</summary>
    [Fact]
    public void WithNote_BuilderNodes_AttachNotesInTheCompiledRule_Test()
    {
        RuleBuilder builder = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b").WithNote("about b"))
            .WithNote("root");

        CompiledRule<RuleTestContext> rule = builder.Compile(CreateCompiler()).GetRuleOrThrow();

        Assert.Equal(new Dictionary<string, string> { ["$"] = "root", ["$.operands[1]"] = "about b" }, rule.Notes);
    }

    /// <summary><see cref="RuleBuilder.FromCompiled{TContext}"/> keeps the notes of the compiled rule it holds.</summary>
    [Fact]
    public void FromCompiled_RuleWithNotes_KeepsItsNotesUnderTheNewPath_Test()
    {
        CompiledRule<RuleTestContext> source = Compile("a AND /* about b */ b");

        CompiledRule<RuleTestContext> joined = RuleBuilder
            .Or(RuleBuilder.Predicate("c"), RuleBuilder.FromCompiled(source))
            .Compile(CreateCompiler())
            .GetRuleOrThrow();

        Assert.Equal(new Dictionary<string, string> { ["$.operands[1].operands[1]"] = "about b" }, joined.Notes);
    }

    /// <summary>The published schema accepts a <c>note</c> string on every node kind.</summary>
    [Fact]
    public void Schema_NoteOnEveryNodeKind_Validates_Test()
    {
        const string Json = """
            {"op":"atLeast","k":1,"note":"n","operands":[
              {"const":true,"note":"n"},
              {"predicate":"a","note":"n"},
              {"op":"not","note":"n","operands":[{"predicate":"b"}]},
              {"op":"between","min":0,"max":1,"note":"n","operands":[{"predicate":"a"},{"predicate":"b"}]}
            ]}
            """;
        using JsonDocument document = JsonDocument.Parse(Json);

        EvaluationResults results = RuleTreeSchemaTests.Schema.Evaluate(document.RootElement);

        Assert.True(results.IsValid);
        Assert.True(CreateCompiler().CompileJson(Json).Succeeded);
    }

    /// <summary><c>NormalizeWhitespace</c> keeps comments and ends a line comment with a line feed, so the text keeps its meaning.</summary>
    [Fact]
    public void NormalizeWhitespace_TextWithComments_KeepsCommentsAndLineBreaks_Test()
    {
        string normalized = RuleText.NormalizeWhitespace("a   AND // about b\n   b  /*  tail  */");

        Assert.Equal("a AND // about b\nb /*  tail  */", normalized);
        Assert.Equal(Compile("a AND b").Root, Compile(normalized).Root);
    }

    /// <summary>
    /// Property test: for generated rules with notes on random nodes, the printed text compiles to the same tree as the
    /// rule without notes, carries exactly the generated notes, and evaluates to the same result.
    /// </summary>
    [Fact]
    public async Task Compile_GeneratedRulesWithAndWithoutNotes_GiveEqualTreesAndEqualResults_Test()
    {
        TruthValue[] inputs = new TruthValue[NotedRuleGenerator.InputNames.Length];
        RuleCompiler<RuleTestContext> compiler = NotedRuleGenerator.CreateCompiler(inputs);
        Random random = new(20261011);

        for (int iteration = 0; iteration < 200; iteration++)
        {
            NotedRule sample = NotedRuleGenerator.Next(random);
            CompiledRule<RuleTestContext> plain = compiler.Compile(sample.PlainText).GetRuleOrThrow();
            CompiledRule<RuleTestContext> noted = compiler.Compile(sample.NotedText).GetRuleOrThrow();

            Assert.Equal(plain.Root, noted.Root);
            Assert.Equal(sample.Notes, noted.Notes);
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = (TruthValue)random.Next(3);
            }

            Decision plainDecision = await plain.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
            Decision notedDecision = await noted.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
            Assert.Equal(plainDecision.Result, notedDecision.Result);
        }
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string name in (string[])["a", "b", "c", "d", "e"])
        {
            builder = builder.AddConstant(name, true);
        }

        return new RuleCompiler<RuleTestContext>(builder.Build());
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        return CreateCompiler().Compile(text).GetRuleOrThrow();
    }
}
