namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class RuleLoaderTests
{
    /// <summary>Each format fixture loads whole, in its own format, with the extension as the evidence.</summary>
    /// <param name="fixture">The fixture file.</param>
    /// <param name="expected">The format of the fixture.</param>
    [Theory]
    [InlineData("access.rule", RuleFormat.Text)]
    [InlineData("access.json", RuleFormat.Json)]
    [InlineData("access.yaml", RuleFormat.Yaml)]
    public void LoadFile_FormatFixture_LoadsWholeInputInItsFormat_Test(string fixture, RuleFormat expected)
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(Fixtures.Path(fixture));

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.Value.Format);
        Assert.Equal(new FormatDetection(expected, FormatEvidence.Extension), result.Value.Detection);
        Assert.Null(result.Value.Path);
        Assert.Equal(Fixtures.Read(fixture), result.Value.Text);
    }

    /// <summary>A rule file with a UTF-8 byte order mark loads as the same rule as the file without the mark.</summary>
    [Fact]
    public void LoadFile_BomFixture_LoadsWithoutMark_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(Fixtures.Path("access-bom.rule"));

        Assert.True(result.Succeeded);
        Assert.Equal(SourceEncoding.Utf8WithBom, result.Value.Source.Encoding);
        Assert.Equal(Fixtures.Read("access.rule"), result.Value.Text);
        Assert.True(RuleConverter.Compile(result.Value).Succeeded);
    }

    /// <summary>A path selects a YAML mapping inside a commented document as a YAML rule with its note.</summary>
    [Fact]
    public void LoadFile_YamlPathToMapping_SelectsEmbeddedYamlRule_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.yaml"),
            new RuleLoadOptions { Path = "rules.publish" }
        );

        Assert.True(result.Succeeded);
        Assert.Equal("$.rules.publish", result.Value.Path);
        Assert.Equal(RuleFormat.Yaml, result.Value.Format);
        AssertCompilesLikeAccessFixture(result.Value);
    }

    /// <summary>A path selects a JSON object inside a document as a JSON rule with its note.</summary>
    [Fact]
    public void LoadFile_JsonPathToObject_SelectsEmbeddedJsonRule_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.json"),
            new RuleLoadOptions { Path = "$['rules'].publish" }
        );

        Assert.True(result.Succeeded);
        Assert.Equal("$.rules.publish", result.Value.Path);
        Assert.Equal(RuleFormat.Json, result.Value.Format);
        AssertCompilesLikeAccessFixture(result.Value);
    }

    /// <summary>A path that selects a string selects rule text, in a JSON or a YAML document.</summary>
    /// <param name="fixture">The document fixture.</param>
    [Theory]
    [InlineData("policies.yaml")]
    [InlineData("policies.json")]
    public void LoadFile_PathToString_SelectsRuleText_Test(string fixture)
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path(fixture),
            new RuleLoadOptions { Path = "$.rules.review" }
        );

        Assert.True(result.Succeeded);
        Assert.Equal(RuleFormat.Text, result.Value.Format);
        Assert.Equal("isActive AND hasRole(role: \"reviewer\")", result.Value.Text);
    }

    /// <summary>An array index selects an item of a YAML sequence.</summary>
    [Fact]
    public void LoadFile_YamlPathWithIndex_SelectsSequenceItem_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.yaml"),
            new RuleLoadOptions { Path = "$.rules.list[0]" }
        );

        Assert.True(result.Succeeded);
        Assert.Equal("NOT isBanned", RuleConverter.Compile(result.Value).CompiledRule?.CanonicalText);
    }

    /// <summary>A flow-style YAML mapping inside a flow collection is selected up to its own closing brace.</summary>
    [Fact]
    public void LoadString_YamlFlowMapping_SelectsWholeMapping_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadString(
            "rules: [ {op: not, operands: [{predicate: isBanned}]}, {predicate: other} ]\n",
            "doc.yaml",
            new RuleLoadOptions { Path = "rules[0]" }
        );

        Assert.True(result.Succeeded);
        Assert.Equal("NOT isBanned", RuleConverter.Compile(result.Value).CompiledRule?.CanonicalText);
    }

    /// <summary>The text of an embedded YAML rule is its own source, so a comment inside it stays.</summary>
    [Fact]
    public void LoadFile_YamlPathToMapping_KeepsCommentsInsideTheRule_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.yaml"),
            new RuleLoadOptions { Path = "rules.publish" }
        );

        Assert.Contains("# checked first", result.Value?.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("service:", result.Value?.Text, StringComparison.Ordinal);
    }

    /// <summary>A path that selects nothing names the first segment that is missing.</summary>
    [Fact]
    public void LoadFile_PathNotFound_ReportsMissingSegment_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.yaml"),
            new RuleLoadOptions { Path = "rules.delete.op" }
        );

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.PathNotFound, diagnostic.Code);
        Assert.Equal("$.rules.delete.op", diagnostic.Path);
        Assert.Contains("'$.rules' has no '.delete'", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A path that selects a number is not a rule.</summary>
    [Fact]
    public void LoadFile_PathToNumber_ReportsNotARule_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.json"),
            new RuleLoadOptions { Path = "rules.count" }
        );

        Assert.Equal(ToolDiagnosticCodes.NotARule, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A path on rule text is not a rule: rule text has no paths.</summary>
    [Fact]
    public void LoadFile_PathOnRuleText_ReportsNotARule_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("access.rule"),
            new RuleLoadOptions { Path = "rules" }
        );

        Assert.Equal(ToolDiagnosticCodes.NotARule, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A path with a syntax error is reported with the character where it fails.</summary>
    /// <param name="path">The invalid path.</param>
    [Theory]
    [InlineData("rules..publish")]
    [InlineData("rules[x]")]
    [InlineData("rules['open")]
    [InlineData("$rules")]
    public void LoadFile_InvalidPath_ReportsInvalidPath_Test(string path)
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadFile(
            Fixtures.Path("policies.yaml"),
            new RuleLoadOptions { Path = path }
        );

        Assert.Equal(ToolDiagnosticCodes.InvalidPath, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A malformed document is reported with its position when a path needs to read it.</summary>
    [Fact]
    public void LoadString_MalformedYamlWithPath_ReportsMalformedDocument_Test()
    {
        ToolResult<LoadedRule> result = RuleLoader.LoadString(
            "rules:\n  a: 1\n  a: 2\n",
            "doc.yaml",
            new RuleLoadOptions { Path = "rules.a" }
        );

        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>Standard input has no file name, so its content decides the format; the format override wins over it.</summary>
    [Fact]
    public void LoadStream_StandardInput_DetectsFromContentOrUsesOverride_Test()
    {
        using MemoryStream yaml = new(Encoding.UTF8.GetBytes(Fixtures.Read("access.yaml")));
        using MemoryStream forced = new(Encoding.UTF8.GetBytes("isActive"));

        ToolResult<LoadedRule> detected = RuleLoader.LoadStream(yaml);
        ToolResult<LoadedRule> overridden = RuleLoader.LoadStream(
            forced,
            options: new RuleLoadOptions { Format = RuleFormat.Text }
        );

        Assert.Equal(new FormatDetection(RuleFormat.Yaml, FormatEvidence.Content), detected.Value?.Detection);
        Assert.Equal("-", detected.Value?.Source.Origin);
        Assert.Equal(new FormatDetection(RuleFormat.Text, FormatEvidence.Override), overridden.Value?.Detection);
    }

    private static void AssertCompilesLikeAccessFixture(LoadedRule rule)
    {
        CompilationResult<StubAssignment> expected = RuleConverter.Compile(
            RuleLoader.LoadFile(Fixtures.Path("access.rule")).Value!
        );
        CompilationResult<StubAssignment> actual = RuleConverter.Compile(rule);

        Assert.True(actual.Succeeded);
        Assert.Equal(expected.CompiledRule!.CanonicalText, actual.CompiledRule!.CanonicalText);
        Assert.Equal(expected.CompiledRule.Notes, actual.CompiledRule.Notes);
    }
}
