namespace TruthWeaver.Tool.Core.Tests.Loading;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class FormatDetectorTests
{
    /// <summary>The content of each format fixture, with no file name, is detected as its own format.</summary>
    /// <param name="fixture">The fixture file.</param>
    /// <param name="expected">The format of the fixture.</param>
    [Theory]
    [InlineData("access.rule", RuleFormat.Text)]
    [InlineData("access.json", RuleFormat.Json)]
    [InlineData("access.yaml", RuleFormat.Yaml)]
    [InlineData("policies.yaml", RuleFormat.Yaml)]
    [InlineData("policies.json", RuleFormat.Json)]
    public void Detect_FixtureContent_FindsFormat_Test(string fixture, RuleFormat expected)
    {
        FormatDetection detection = FormatDetector.Detect(Fixtures.Read(fixture));

        Assert.Equal(new FormatDetection(expected, FormatEvidence.Content), detection);
    }

    /// <summary>Content rules for inputs that look alike: rule text is never taken for a document.</summary>
    /// <param name="text">The input.</param>
    /// <param name="expected">The format the content decides.</param>
    [Theory]
    [InlineData("isActive", RuleFormat.Text)]
    [InlineData("hasRole(\n  role: \"editor\")", RuleFormat.Text)]
    [InlineData("isA ? isB : isC", RuleFormat.Text)]
    [InlineData("// note\nisActive OR isAdmin", RuleFormat.Text)]
    [InlineData("{ isActive AND isAdmin }", RuleFormat.Text)]
    [InlineData("", RuleFormat.Text)]
    [InlineData("{\"op\": \"and\", ", RuleFormat.Json)]
    [InlineData("---\npredicate: isActive", RuleFormat.Yaml)]
    [InlineData("# comment\npredicate: isActive", RuleFormat.Yaml)]
    [InlineData("\"predicate\": isActive", RuleFormat.Yaml)]
    [InlineData("- predicate: isActive", RuleFormat.Yaml)]
    public void Detect_LookAlikeContent_FindsFormat_Test(string text, RuleFormat expected)
    {
        Assert.Equal(expected, FormatDetector.Detect(text).Format);
    }

    /// <summary>A known extension wins over the content, ignoring case.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="expected">The format the extension names.</param>
    [Theory]
    [InlineData("rule.JSON", RuleFormat.Json)]
    [InlineData("rule.yml", RuleFormat.Yaml)]
    [InlineData("rule.yaml", RuleFormat.Yaml)]
    [InlineData("rule.rule", RuleFormat.Text)]
    [InlineData("rule.tw", RuleFormat.Text)]
    [InlineData("rule.txt", RuleFormat.Text)]
    public void Detect_KnownExtension_WinsOverContent_Test(string fileName, RuleFormat expected)
    {
        FormatDetection detection = FormatDetector.Detect("predicate: isActive", fileName);

        Assert.Equal(new FormatDetection(expected, FormatEvidence.Extension), detection);
    }

    /// <summary>An unknown extension leaves the decision to the content.</summary>
    [Fact]
    public void Detect_UnknownExtension_UsesContent_Test()
    {
        FormatDetection detection = FormatDetector.Detect("predicate: isActive", "rule.policy");

        Assert.Equal(new FormatDetection(RuleFormat.Yaml, FormatEvidence.Content), detection);
    }

    /// <summary>An override wins over the extension and the content.</summary>
    [Fact]
    public void Detect_Override_WinsOverExtensionAndContent_Test()
    {
        FormatDetection detection = FormatDetector.Detect("predicate: isActive", "rule.json", RuleFormat.Text);

        Assert.Equal(new FormatDetection(RuleFormat.Text, FormatEvidence.Override), detection);
    }
}
