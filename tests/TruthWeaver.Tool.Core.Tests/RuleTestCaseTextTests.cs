namespace TruthWeaver.Tool.Core.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Abstractions;

public sealed class RuleTestCaseTextTests
{
    private const string AccessFile = """
        # The access rule.
        name: access
        rule: |
          isActive AND hasRole(role: "editor")
        cases:
          - name: an active editor is let in
            given:
              isActive: True
              'hasRole(role: "editor")': True
            expect:
              result: True
          # The second case.
          - name: an inactive user is kept out
            given:
              isActive: False
            expect:
              result: False
              faults: 0
        """;

    /// <summary>A new file has the inline rule, a cases list and the first case in the format that the runner reads.</summary>
    [Fact]
    public void NewFile_OneCase_WritesTheRuleAndTheCase_Test()
    {
        RuleTestCaseSpec spec = Case("both true", new() { ["isActive"] = TruthValue.True }, TruthValue.True, 0);

        string text = RuleTestCaseText.NewFile("isActive", spec);

        Assert.Equal(
            "rule: |\n  isActive\ncases:\n  - name: \"both true\"\n    given:\n      isActive: True\n    expect:\n      result: True\n      faults: 0\n",
            text
        );
    }

    /// <summary>A term with arguments is a single-quoted key, and a quote inside it is doubled.</summary>
    [Fact]
    public void NewFile_TermWithArguments_QuotesTheKey_Test()
    {
        RuleTestCaseSpec spec = Case(
            "quoted",
            new() { ["hasRole(role: \"o'brien\")"] = TruthValue.False },
            TruthValue.False,
            null
        );

        string text = RuleTestCaseText.NewFile("hasRole(role: \"o'brien\")", spec);
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(text);

        Assert.Contains("'hasRole(role: \"o''brien\")': False", text, StringComparison.Ordinal);
        Assert.Equal(TruthValue.False, Assert.Single(read.Value!).Given["hasRole(role: \"o'brien\")"]);
    }

    /// <summary>Reading a file gives each case with its values and its expectation.</summary>
    [Fact]
    public void ReadCases_AccessFile_ReadsEachCase_Test()
    {
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(AccessFile);

        Assert.True(read.Succeeded, string.Join("; ", read.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["an active editor is let in", "an inactive user is kept out"], read.Value.Select(c => c.Name));
        Assert.Equal(TruthValue.True, read.Value[0].Given["hasRole(role: \"editor\")"]);
        Assert.Equal((TruthValue.False, 0), (read.Value[1].ExpectedResult, read.Value[1].ExpectedFaults));
    }

    /// <summary>Adding a case keeps the comments, the other keys and the other cases as they were.</summary>
    [Fact]
    public void AddOrReplaceCase_NewName_AppendsTheCaseAndKeepsTheRest_Test()
    {
        RuleTestCaseSpec spec = Case("unknown role", new() { ["isActive"] = TruthValue.True }, TruthValue.Unknown, 0);

        ToolResult<string> added = RuleTestCaseText.AddOrReplaceCase(AccessFile, spec);

        Assert.True(added.Succeeded);
        Assert.StartsWith("# The access rule.\nname: access\n", added.Value, StringComparison.Ordinal);
        Assert.Contains("  # The second case.\n", added.Value, StringComparison.Ordinal);
        Assert.EndsWith(
            "  - name: \"unknown role\"\n    given:\n      isActive: True\n    expect:\n      result: Unknown\n      faults: 0\n",
            added.Value,
            StringComparison.Ordinal
        );
        Assert.Equal(3, RuleTestCaseText.ReadCases(added.Value).Value!.Count);
    }

    /// <summary>A case with a name that is already in the file replaces that case, in place.</summary>
    [Fact]
    public void AddOrReplaceCase_SameName_ReplacesTheCaseInPlace_Test()
    {
        RuleTestCaseSpec spec = Case(
            "an inactive user is kept out",
            new() { ["isActive"] = TruthValue.Unknown },
            TruthValue.Unknown,
            0
        );

        ToolResult<string> replaced = RuleTestCaseText.AddOrReplaceCase(AccessFile, spec);

        IReadOnlyList<RuleTestCaseSpec> cases = RuleTestCaseText.ReadCases(replaced.Value!).Value!;
        Assert.Equal(2, cases.Count);
        Assert.Equal(TruthValue.Unknown, cases[1].ExpectedResult);
        Assert.Equal("an active editor is let in", cases[0].Name);
    }

    /// <summary>Variables of a case are written as block YAML and read back as the same data.</summary>
    [Fact]
    public void AddOrReplaceCase_WithVariables_RoundTripsTheData_Test()
    {
        JsonObject variables = JsonNode.Parse("{\"user\":{\"region\":\"EU\",\"tags\":[\"a\"]}}")!.AsObject();
        RuleTestCaseSpec spec = new(
            "eu user",
            new Dictionary<string, TruthValue> { ["inRegion(region: \"EU\")"] = TruthValue.True },
            variables,
            TruthValue.True,
            0
        );

        ToolResult<string> added = RuleTestCaseText.AddOrReplaceCase(AccessFile, spec);

        RuleTestCaseSpec read = RuleTestCaseText.ReadCases(added.Value!).Value!.Single(c => c.Name == "eu user");
        Assert.Equal(variables.ToJsonString(), read.Variables.ToJsonString());
    }

    /// <summary>Removing a case removes only its lines.</summary>
    [Fact]
    public void RemoveCase_ExistingName_RemovesThatCase_Test()
    {
        ToolResult<string> removed = RuleTestCaseText.RemoveCase(AccessFile, "an active editor is let in");

        Assert.True(removed.Succeeded);
        Assert.Equal(["an inactive user is kept out"], RuleTestCaseText.ReadCases(removed.Value).Value!.Select(c => c.Name));
    }

    /// <summary>A file keeps at least one case, so the last case cannot be removed.</summary>
    [Fact]
    public void RemoveCase_LastCase_IsAnError_Test()
    {
        string single = RuleTestCaseText.NewFile("a", Case("only", [], TruthValue.Unknown, null));

        ToolResult<string> removed = RuleTestCaseText.RemoveCase(single, "only");

        Assert.False(removed.Succeeded);
    }

    /// <summary>The rule of a file can be swapped for another rule, with the cases and the other keys unchanged.</summary>
    [Fact]
    public void WithRule_NewRule_ReplacesTheRuleAndKeepsTheCases_Test()
    {
        ToolResult<string> swapped = RuleTestCaseText.WithRule(AccessFile, "isActive OR isAdmin");

        Assert.True(swapped.Succeeded);
        Assert.StartsWith("rule: |\n  isActive OR isAdmin\n", swapped.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("hasRole(role: \"editor\")\n", swapped.Value.Split("cases:")[0], StringComparison.Ordinal);
        Assert.Equal(2, RuleTestCaseText.ReadCases(swapped.Value).Value!.Count);
    }

    /// <summary>A file that names its rule in a separate file loses that key when the rule is swapped.</summary>
    [Fact]
    public void WithRule_RuleFile_RemovesTheRuleFileKey_Test()
    {
        const string File = "ruleFile: access.rule\ncases:\n  - name: a\n    expect:\n      result: Unknown\n";

        ToolResult<string> swapped = RuleTestCaseText.WithRule(File, "x");

        Assert.Equal("rule: |\n  x\ncases:\n  - name: a\n    expect:\n      result: Unknown\n", swapped.Value);
    }

    /// <summary>A flow list of cases is not supported, and the message says so.</summary>
    [Fact]
    public void ReadCases_FlowList_IsAnError_Test()
    {
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(
            "rule: a\ncases: [{name: a, expect: {result: True}}]\n"
        );

        Assert.False(read.Succeeded);
        Assert.Contains("block list", read.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    /// <summary>YAML that is not well-formed is an error with a position, not an exception.</summary>
    [Fact]
    public void AddOrReplaceCase_MalformedYaml_IsAnError_Test()
    {
        ToolResult<string> added = RuleTestCaseText.AddOrReplaceCase("rule: [\n", Case("a", [], TruthValue.True, null));

        Assert.False(added.Succeeded);
    }

    private static RuleTestCaseSpec Case(string name, Dictionary<string, TruthValue> given, TruthValue result, int? faults)
    {
        return new RuleTestCaseSpec(name, given, [], result, faults);
    }
}
