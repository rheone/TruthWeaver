namespace TruthWeaver.Tool.Core.Tests.Assignment;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class StubAssignmentTests
{
    /// <summary>A YAML stub file keys each value by the canonical text of its term.</summary>
    [Fact]
    public void Load_YamlFixture_KeysValuesByCanonicalTerm_Test()
    {
        ToolResult<StubAssignment> result = StubAssignment.Load(Fixtures.Path("stubs.yaml"));

        Assert.True(result.Succeeded);
        Assert.Equal(["hasRole(role: \"editor\")", "isActive"], result.Value.Terms);
        Assert.Equal(TruthValue.True, result.Value.ValueOf("isActive"));
        Assert.Equal(TruthValue.Unknown, result.Value.ValueOf("hasRole(role: \"editor\")"));
    }

    /// <summary>A JSON stub file accepts JSON booleans and letters, and a key with other spacing names the same term.</summary>
    [Fact]
    public void Load_JsonFixture_NormalizesKeysAndReadsBooleans_Test()
    {
        ToolResult<StubAssignment> result = StubAssignment.Load(Fixtures.Path("stubs.json"));

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.True, result.Value.ValueOf("isActive"));
        Assert.Equal(TruthValue.False, result.Value.ValueOf("hasRole(role: \"editor\")"));
    }

    /// <summary>A term with no entry is Unknown.</summary>
    [Fact]
    public void ValueOf_TermWithNoEntry_IsUnknown_Test()
    {
        StubAssignment stubs = StubAssignment.Parse("isActive: false", RuleFormat.Yaml).Value!;

        Assert.False(stubs.TryGetValue("isAdmin", out TruthValue value));
        Assert.Equal(TruthValue.Unknown, value);
        Assert.Equal(TruthValue.False, stubs.ValueOf("isActive"));
    }

    /// <summary>Keys that are not one term, values that are not truth values, and two keys for one term are errors.</summary>
    [Fact]
    public void Parse_InvalidEntries_ReportsEachWithItsPath_Test()
    {
        const string yaml = """
            isActive AND isAdmin: true
            isBanned: maybe
            isOwner: T
            ' isOwner': F
            """;

        ToolResult<StubAssignment> result = StubAssignment.Parse(yaml, RuleFormat.Yaml);

        Assert.False(result.Succeeded);
        Assert.All(result.Diagnostics, d => Assert.Equal(ToolDiagnosticCodes.InvalidStubValue, d.Code));
        Assert.Equal(["$['isActive AND isAdmin']", "$['isBanned']", "$[' isOwner']"], result.Diagnostics.Select(d => d.Path));
    }

    /// <summary>A stub document that is not a mapping, or not well formed, is an error.</summary>
    /// <param name="text">The document.</param>
    /// <param name="format">Its format.</param>
    /// <param name="code">The expected code.</param>
    [Theory]
    [InlineData("[true]", RuleFormat.Json, ToolDiagnosticCodes.InvalidStubValue)]
    [InlineData("- true", RuleFormat.Yaml, ToolDiagnosticCodes.InvalidStubValue)]
    [InlineData("{", RuleFormat.Json, ToolDiagnosticCodes.MalformedDocument)]
    [InlineData("a: [", RuleFormat.Yaml, ToolDiagnosticCodes.MalformedDocument)]
    public void Parse_NotAMapping_ReportsError_Test(string text, RuleFormat format, string code)
    {
        ToolResult<StubAssignment> result = StubAssignment.Parse(text, format);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(code, diagnostic.Code);
    }
}
