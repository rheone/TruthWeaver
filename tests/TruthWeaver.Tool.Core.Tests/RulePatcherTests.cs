namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class RulePatcherTests
{
    private const string YamlFixture = "patch/commented.yaml";
    private const string JsonFixture = "patch/notes.json";

    /// <summary>A renamed predicate in a commented YAML rule changes that one value; every other byte, comments included, stays.</summary>
    [Fact]
    public void Patch_YamlPredicateRenamed_ChangesOnlyThatValue_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.publish");
        CompiledRule<StubAssignment> edited = Compile(
            "/* Editors who are active. */ (/* Checked first. */ isEnabled AND hasRole(role: \"editor\"))"
        );

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        string original = Fixtures.Read(YamlFixture);
        Assert.Equal(
            original.Replace("- predicate: isActive  #", "- predicate: isEnabled  #", StringComparison.Ordinal),
            result.Value.Text
        );
        RulePatchOutcome outcome = Assert.Single(result.Value.Rules);
        Assert.Equal(new RulePatchOutcome("$.rules.publish", RulePatchKind.Patched), outcome);
    }

    /// <summary>A changed note in a commented YAML rule is patched in place like any other value, in the plain style it had.</summary>
    [Fact]
    public void Patch_YamlNoteChanged_PatchesTheNoteInPlace_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.publish");
        CompiledRule<StubAssignment> edited = Compile(
            "/* Editors only. */ (/* Checked first. */ isActive AND hasRole(role: \"editor\"))"
        );

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        string original = Fixtures.Read(YamlFixture);
        Assert.Equal(
            original.Replace("note: Editors who are active.", "note: Editors only.", StringComparison.Ordinal),
            result.Value.Text
        );
    }

    /// <summary>A changed operator and a changed note in a JSON rule with notes change only those two values.</summary>
    [Fact]
    public void Patch_JsonOperatorAndNoteChanged_ChangesOnlyThoseValues_Test()
    {
        LoadedRule rule = Load(JsonFixture, "$.rules.publish");
        CompiledRule<StubAssignment> edited = Compile(
            "/* Editors who are active. */ (/* Checked before the role. */ isActive OR hasRole(role: \"editor\"))"
        );

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        string expected = Fixtures
            .Read(JsonFixture)
            .Replace("\"op\": \"and\"", "\"op\": \"or\"", StringComparison.Ordinal)
            .Replace("\"Checked first.\"", "\"Checked before the role.\"", StringComparison.Ordinal);
        Assert.Equal(expected, result.Value.Text);
        Assert.Equal(RulePatchKind.Patched, Assert.Single(result.Value.Rules).Kind);
    }

    /// <summary>An edit that compiles to the same tree and notes leaves the document as it is.</summary>
    [Fact]
    public void Patch_SameRule_LeavesDocumentUnchanged_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.publish");
        CompiledRule<StubAssignment> edited = RuleConverter.Compile(rule).Rule!;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(Fixtures.Read(YamlFixture), result.Value.Text);
        Assert.Equal(RulePatchKind.Unchanged, Assert.Single(result.Value.Rules).Kind);
    }

    /// <summary>Several rules in one YAML document patch independently: each rule changes in place and nothing else moves.</summary>
    [Fact]
    public void Patch_SeveralYamlRules_PatchIndependently_Test()
    {
        SourceText document = Load(YamlFixture, null).Source;
        RuleEdit<StubAssignment>[] edits =
        [
            new(
                "rules.publish",
                Compile("/* Editors who are active. */ (/* Checked first. */ isActive AND hasRole(role: \"admin\"))")
            ),
            new("rules.list[0]", Compile("NOT isSuspended")),
        ];

        ToolResult<DocumentPatch> result = RulePatcher.Patch(document, RuleFormat.Yaml, edits);

        Assert.True(result.Succeeded);
        string expected = Fixtures
            .Read(YamlFixture)
            .Replace("role: editor   #", "role: admin   #", StringComparison.Ordinal)
            .Replace("- predicate: isBanned  #", "- predicate: isSuspended  #", StringComparison.Ordinal);
        Assert.Equal(expected, result.Value.Text);
        Assert.Equal(
            [
                new RulePatchOutcome("$.rules.publish", RulePatchKind.Patched),
                new RulePatchOutcome("$.rules.list[0]", RulePatchKind.Patched),
            ],
            result.Value.Rules
        );
    }

    /// <summary>Several rules in one JSON document patch independently.</summary>
    [Fact]
    public void Patch_SeveralJsonRules_PatchIndependently_Test()
    {
        SourceText document = Load(JsonFixture, null).Source;
        RuleEdit<StubAssignment>[] edits =
        [
            new("$.rules.audit", Compile("NOT isSuspended")),
            new(
                "$.rules.publish",
                Compile("/* Editors who are active. */ (/* Checked first. */ isActive AND hasRole(role: \"admin\"))")
            ),
        ];

        ToolResult<DocumentPatch> result = RulePatcher.Patch(document, RuleFormat.Json, edits);

        Assert.True(result.Succeeded);
        string expected = Fixtures
            .Read(JsonFixture)
            .Replace("\"isBanned\"", "\"isSuspended\"", StringComparison.Ordinal)
            .Replace("\"role\": \"editor\"", "\"role\": \"admin\"", StringComparison.Ordinal);
        Assert.Equal(expected, result.Value.Text);
        Assert.All(result.Value.Rules, outcome => Assert.Equal(RulePatchKind.Patched, outcome.Kind));
    }

    /// <summary>
    /// An added operand does not fit the old structure, so the YAML rule is rewritten in full and the result reports it with
    /// a warning; the text before and after the rule stays, and the document reads back as the edited rule.
    /// </summary>
    [Fact]
    public void Patch_YamlOperandAdded_RewritesRuleAndWarns_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.publish");
        CompiledRule<StubAssignment> edited = Compile(
            "/* Editors who are active. */ (/* Checked first. */ isActive AND hasRole(role: \"editor\") AND isVerified)"
        );

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.RuleRewritten, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("$.rules.publish", warning.Path);
        Assert.Equal(RulePatchKind.RuleRewritten, Assert.Single(result.Value.Rules).Kind);
        AssertOutsideUnchanged(Fixtures.Read(YamlFixture), result.Value.Text, "op: and  #", "\n  review:");
        AssertReadsBackAs(result.Value.Text, RuleFormat.Yaml, "rules.publish", edited);
    }

    /// <summary>A structural change in a JSON rule rewrites that rule, indented like the document, and keeps the rest.</summary>
    [Fact]
    public void Patch_JsonOperandRemoved_RewritesRuleAndWarns_Test()
    {
        LoadedRule rule = Load(JsonFixture, "rules.publish");
        CompiledRule<StubAssignment> edited = Compile("/* Editors who are active. */ (isActive AND isVerified)");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.RuleRewritten, Assert.Single(result.Diagnostics).Code);
        AssertOutsideUnchanged(Fixtures.Read(JsonFixture), result.Value.Text, "{\n      \"note\"", ",\n    \"audit\"");
        Assert.Contains("\n      \"operands\": [\n", result.Value.Text, StringComparison.Ordinal);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Json, "rules.publish", edited);
    }

    /// <summary>A structural change in a YAML flow mapping rewrites the rule in flow style on its own line.</summary>
    [Fact]
    public void Patch_YamlFlowMappingRestructured_RewritesInFlowStyle_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.inline");
        CompiledRule<StubAssignment> edited = Compile("NOT (isBanned OR isSuspended)");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Equal(RulePatchKind.RuleRewritten, Assert.Single(result.Value.Rules).Kind);
        string expected = Fixtures
            .Read(YamlFixture)
            .Replace(
                "inline: {op: not, operands: [{predicate: isBanned}]}",
                "inline: {\"op\": \"not\", \"operands\": [{\"op\": \"or\", \"operands\": [{\"predicate\": \"isBanned\"}, {\"predicate\": \"isSuspended\"}]}]}",
                StringComparison.Ordinal
            );
        Assert.Equal(expected, result.Value.Text);
    }

    /// <summary>A value with a YAML anchor is shared with its aliases, so it is not patched in place: the rule is rewritten.</summary>
    [Fact]
    public void Patch_YamlAnchoredValue_RewritesRule_Test()
    {
        const string document = "roles:\n  main: &role editor\nrule:\n  predicate: hasRole\n  args:\n    role: *role\n";
        LoadedRule rule = RuleLoader.LoadString(document, "doc.yaml", new RuleLoadOptions { Path = "rule" }).Value!;
        CompiledRule<StubAssignment> edited = Compile("hasRole(role: \"admin\")");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Equal(RulePatchKind.RuleRewritten, Assert.Single(result.Value.Rules).Kind);
        Assert.StartsWith("roles:\n  main: &role editor\nrule:\n", result.Value.Text, StringComparison.Ordinal);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Yaml, "rule", edited);
    }

    /// <summary>A rule that is itself anchored is shared with its aliases, so the patch fails with an error and gives no text.</summary>
    [Fact]
    public void Patch_AnchoredRule_FailsWithWriteBackError_Test()
    {
        const string document = "rule: &shared\n  predicate: isActive\ncopy: *shared\n";
        LoadedRule rule = RuleLoader.LoadString(document, "doc.yaml", new RuleLoadOptions { Path = "rule" }).Value!;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, Compile("isVerified"));

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.WriteBackFailed, error.Code);
        Assert.Equal(ToolExitCode.Unusable, SeverityPolicy.ExitCodeFor(result.Diagnostics, strict: false));
    }

    /// <summary>A rule written as a string of rule text is printed again inside its quotes, in the quote style it had, with a warning.</summary>
    [Fact]
    public void Patch_YamlRuleTextString_RewritesStringInItsStyle_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.review");
        CompiledRule<StubAssignment> edited = Compile("isActive AND hasRole(role: \"auditor\")");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.RuleRewritten, Assert.Single(result.Diagnostics).Code);
        string expected = Fixtures
            .Read(YamlFixture)
            .Replace(
                "review: 'isActive AND hasRole(role: \"reviewer\")'",
                "review: 'isActive AND hasRole(role: \"auditor\")'",
                StringComparison.Ordinal
            );
        Assert.Equal(expected, result.Value.Text);
    }

    /// <summary>A rule text string in a JSON document is printed again as a JSON string.</summary>
    [Fact]
    public void Patch_JsonRuleTextString_RewritesString_Test()
    {
        LoadedRule rule = Load(JsonFixture, "rules.review");
        CompiledRule<StubAssignment> edited = Compile("isActive AND hasRole(role: \"auditor\")");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited);

        Assert.True(result.Succeeded);
        string expected = Fixtures
            .Read(JsonFixture)
            .Replace(
                "\"review\": \"isActive AND hasRole(role: \\\"reviewer\\\")\"",
                "\"review\": \"isActive AND hasRole(role: \\\"auditor\\\")\"",
                StringComparison.Ordinal
            );
        Assert.Equal(expected, result.Value.Text);
    }

    /// <summary>A rule text string in a YAML literal block keeps its block header and indentation.</summary>
    [Fact]
    public void Patch_YamlLiteralBlockRuleText_KeepsBlockStyle_Test()
    {
        const string document = "rule: |\n  isActive\n    AND isVerified\nnext: 1\n";
        LoadedRule rule = RuleLoader.LoadString(document, "doc.yaml", new RuleLoadOptions { Path = "rule" }).Value!;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, Compile("isActive OR isVerified"));

        Assert.True(result.Succeeded);
        Assert.Equal("rule: |\n  isActive OR isVerified\nnext: 1\n", result.Value.Text);
    }

    /// <summary>A whole rule text file is printed again between its leading and trailing white space.</summary>
    [Fact]
    public void Patch_RuleTextFile_RewritesTextKeepingFinalNewline_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("isActive  AND\n  isVerified\n", "access.rule").Value!;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, Compile("isActive OR isVerified"));

        Assert.True(result.Succeeded);
        Assert.Equal("isActive OR isVerified\n", result.Value.Text);
        Assert.Equal(new RulePatchOutcome(null, RulePatchKind.RuleRewritten), Assert.Single(result.Value.Rules));
    }

    /// <summary>A whole YAML rule file patches in place and keeps its leading comment.</summary>
    [Fact]
    public void Patch_WholeYamlFile_PatchesInPlace_Test()
    {
        const string document =
            "# The rule.\nop: and\noperands:\n  - predicate: isActive  # first\n  - predicate: isVerified\n";
        LoadedRule rule = RuleLoader.LoadString(document, "access.yaml").Value!;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, Compile("isActive OR isVerified"));

        Assert.True(result.Succeeded);
        Assert.Equal(document.Replace("op: and", "op: or", StringComparison.Ordinal), result.Value.Text);
    }

    /// <summary>The round-trip mode writes the whole document again, warns that comments and layout may be lost, and reads back as the edit.</summary>
    [Fact]
    public void Patch_RoundTripMode_RewritesDocumentAndWarns_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.publish");
        CompiledRule<StubAssignment> edited = Compile("isActive OR isVerified");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited, WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.RoundTripWriteBack, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(RulePatchKind.DocumentRewritten, Assert.Single(result.Value.Rules).Kind);
        Assert.DoesNotContain("#", result.Value.Text, StringComparison.Ordinal);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Yaml, "rules.publish", edited);
        AssertReadsBackAs(
            result.Value.Text,
            RuleFormat.Yaml,
            "rules.review",
            Compile("isActive AND hasRole(role: \"reviewer\")")
        );
    }

    /// <summary>The round-trip mode over JSON writes the whole document indented, with the edit.</summary>
    [Fact]
    public void Patch_JsonRoundTripMode_RewritesDocument_Test()
    {
        LoadedRule rule = Load(JsonFixture, "rules.audit");
        CompiledRule<StubAssignment> edited = Compile("NOT isSuspended");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited, WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.RoundTripWriteBack, Assert.Single(result.Diagnostics).Code);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Json, "rules.audit", edited);
        Assert.Contains("\"owner\": \"platform-team\"", result.Value.Text, StringComparison.Ordinal);
    }

    /// <summary>The round-trip mode replaces a whole JSON file when the rule is the document itself.</summary>
    [Fact]
    public void Patch_JsonWholeFileRoundTrip_ReplacesTheDocument_Test()
    {
        LoadedRule rule = RuleLoader
            .LoadString("{\"op\":\"and\",\"operands\":[{\"predicate\":\"a\"},{\"predicate\":\"b\"}]}", "rule.json")
            .Value!;
        CompiledRule<StubAssignment> edited = Compile("a OR b");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited, WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        Assert.Equal(RulePatchKind.DocumentRewritten, Assert.Single(result.Value.Rules).Kind);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Json, null, edited);
    }

    /// <summary>The round-trip mode puts a rule at an array position of a JSON document and keeps the other items.</summary>
    [Fact]
    public void Patch_JsonArrayItemRoundTrip_ReplacesOnlyThatItem_Test()
    {
        LoadedRule rule = RuleLoader
            .LoadString(
                "{\"list\":[{\"predicate\":\"a\"},{\"predicate\":\"b\"}]}",
                "doc.json",
                new RuleLoadOptions { Path = "list[1]" }
            )
            .Value!;
        CompiledRule<StubAssignment> edited = Compile("NOT c");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited, WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Json, "list[1]", edited);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Json, "list[0]", Compile("a"));
    }

    /// <summary>The round-trip mode replaces a whole YAML file when the rule is the document itself.</summary>
    [Fact]
    public void Patch_YamlWholeFileRoundTrip_ReplacesTheDocument_Test()
    {
        LoadedRule rule = RuleLoader
            .LoadString("# c\nop: and\noperands:\n  - predicate: a\n  - predicate: b\n", "rule.yaml")
            .Value!;
        CompiledRule<StubAssignment> edited = Compile("a OR b");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited, WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        Assert.Equal(RulePatchKind.DocumentRewritten, Assert.Single(result.Value.Rules).Kind);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Yaml, null, edited);
    }

    /// <summary>The round-trip mode puts a rule at an array position of a YAML document and keeps the other items.</summary>
    [Fact]
    public void Patch_YamlArrayItemRoundTrip_ReplacesOnlyThatItem_Test()
    {
        LoadedRule rule = Load(YamlFixture, "rules.list[0]");
        CompiledRule<StubAssignment> edited = Compile("NOT isSuspended");

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, edited, WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        AssertReadsBackAs(result.Value.Text, RuleFormat.Yaml, "rules.list[0]", edited);
        AssertReadsBackAs(
            result.Value.Text,
            RuleFormat.Yaml,
            "rules.review",
            Compile("isActive AND hasRole(role: \"reviewer\")")
        );
    }

    /// <summary>The round-trip mode over a rule text file writes the edited text.</summary>
    [Fact]
    public void Patch_RuleTextFileRoundTrip_WritesTheEditedText_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("isActive AND isVerified\n", "access.rule").Value!;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(rule, Compile("isActive OR isVerified"), WriteBackMode.RoundTrip);

        Assert.True(result.Succeeded);
        Assert.Equal("isActive OR isVerified\n", result.Value.Text);
        Assert.Equal(RulePatchKind.DocumentRewritten, Assert.Single(result.Value.Rules).Kind);
    }

    /// <summary>The round-trip mode fails with the loader's diagnostic when an edit's path selects nothing.</summary>
    [Fact]
    public void Patch_RoundTripPathNotFound_Fails_Test()
    {
        SourceText document = Load(YamlFixture, null).Source;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(
            document,
            RuleFormat.Yaml,
            [new RuleEdit<StubAssignment>("rules.missing", Compile("isActive"))],
            WriteBackMode.RoundTrip
        );

        Assert.False(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.PathNotFound, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>An undefined format or mode is a programming error and throws.</summary>
    [Fact]
    public void Patch_UndefinedFormatOrMode_ThrowsArgumentOutOfRangeException_Test()
    {
        SourceText document = Load(YamlFixture, null).Source;
        RuleEdit<StubAssignment>[] edits = [new("rules.publish", Compile("isActive"))];

        ArgumentOutOfRangeException format = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RulePatcher.Patch(document, (RuleFormat)99, edits)
        );
        ArgumentOutOfRangeException mode = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RulePatcher.Patch(document, RuleFormat.Yaml, edits, (WriteBackMode)99)
        );

        Assert.Equal("format", format.ParamName);
        Assert.Equal("mode", mode.ParamName);
    }

    /// <summary>A null edit in the list is a programming error and throws.</summary>
    [Fact]
    public void Patch_NullEdit_ThrowsArgumentNullException_Test()
    {
        SourceText document = Load(YamlFixture, null).Source;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            RulePatcher.Patch(document, RuleFormat.Yaml, new RuleEdit<StubAssignment>[] { null! })
        );

        Assert.Equal("edits", exception.ParamName);
    }

    /// <summary>Encoding a patch writes the byte order mark of each encoding, then the text in that encoding.</summary>
    [Theory]
    [InlineData(SourceEncoding.Utf8, new byte[0])]
    [InlineData(SourceEncoding.Utf8WithBom, new byte[] { 0xEF, 0xBB, 0xBF })]
    [InlineData(SourceEncoding.Utf16LittleEndian, new byte[] { 0xFF, 0xFE })]
    [InlineData(SourceEncoding.Utf16BigEndian, new byte[] { 0xFE, 0xFF })]
    [InlineData(SourceEncoding.Utf32LittleEndian, new byte[] { 0xFF, 0xFE, 0x00, 0x00 })]
    [InlineData(SourceEncoding.Utf32BigEndian, new byte[] { 0x00, 0x00, 0xFE, 0xFF })]
    public void Encode_EachEncoding_StartsWithItsByteOrderMark_Test(SourceEncoding encoding, byte[] mark)
    {
        DocumentPatch patch = new("a", encoding, []);

        byte[] bytes = RulePatcher.Encode(patch);

        Assert.True(bytes.AsSpan().StartsWith(mark));
        Assert.True(bytes.Length > mark.Length);
    }

    /// <summary>An edit whose path selects nothing fails with the loader's diagnostic and gives no text.</summary>
    [Fact]
    public void Patch_PathNotFound_Fails_Test()
    {
        SourceText document = Load(YamlFixture, null).Source;

        ToolResult<DocumentPatch> result = RulePatcher.Patch(
            document,
            RuleFormat.Yaml,
            [new RuleEdit<StubAssignment>("rules.missing", Compile("isActive"))]
        );

        Assert.False(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.PathNotFound, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>Writing a patch keeps the byte order mark and the line endings of the document.</summary>
    [Fact]
    public void WriteFile_DocumentWithBomAndCrlf_KeepsBothBytes_Test()
    {
        using TemporaryDirectory directory = new();
        string file = directory.File("rule.yaml");
        byte[] original =
        [
            0xEF,
            0xBB,
            0xBF,
            .. Encoding.UTF8.GetBytes("# c\r\nop: and\r\noperands:\r\n  - predicate: a\r\n  - predicate: b\r\n"),
        ];
        File.WriteAllBytes(file, original);
        LoadedRule rule = RuleLoader.LoadFile(file).Value!;
        ToolResult<DocumentPatch> patch = RulePatcher.Patch(rule, Compile("a OR b"));

        ToolResult<string> written = RulePatcher.WriteFile(file, patch.Value!);

        Assert.True(written.Succeeded);
        byte[] expected =
        [
            0xEF,
            0xBB,
            0xBF,
            .. Encoding.UTF8.GetBytes("# c\r\nop: or\r\noperands:\r\n  - predicate: a\r\n  - predicate: b\r\n"),
        ];
        Assert.Equal(expected, File.ReadAllBytes(file));
    }

    private static LoadedRule Load(string fixture, string? path)
    {
        return RuleLoader.LoadFile(Fixtures.Path(fixture), new RuleLoadOptions { Path = path }).Value!;
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        CompilationResult<StubAssignment> compiled = RuleConverter.CreateCompiler().Compile(text);
        Assert.True(compiled.Succeeded, string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
        return compiled.Rule;
    }

    // The text before the first marker and from the second marker on is the same in both documents.
    private static void AssertOutsideUnchanged(string original, string patched, string ruleStart, string afterRule)
    {
        int start = original.IndexOf(ruleStart, StringComparison.Ordinal);
        int after = original.IndexOf(afterRule, StringComparison.Ordinal);
        Assert.True(start > 0 && after > start);
        Assert.StartsWith(original[..start], patched, StringComparison.Ordinal);
        Assert.EndsWith(original[after..], patched, StringComparison.Ordinal);
    }

    private static void AssertReadsBackAs(
        string document,
        RuleFormat format,
        string? path,
        CompiledRule<StubAssignment> expected
    )
    {
        ToolResult<LoadedRule> loaded = RuleLoader.LoadString(
            document,
            "patched",
            new RuleLoadOptions { Format = format, Path = path }
        );
        Assert.True(loaded.Succeeded);
        CompiledRule<StubAssignment> actual = RuleConverter.Compile(loaded.Value).Rule!;
        Assert.Equal(expected.PrintJson(), actual.PrintJson());
    }
}
