namespace TruthWeaver.Tool.Core.Tests.RuleCases;

using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.RuleCases;

/// <summary>The cases that <see cref="RuleTestCaseText.ReadCases"/> refuses.</summary>
public sealed class RuleTestCaseTextReadTests
{
    /// <summary>A case with no expected result is an error that names the case and its position.</summary>
    [Fact]
    public void ReadCases_CaseWithoutExpectedResult_NamesTheCase_Test()
    {
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(
            "rule: a\ncases:\n  - name: no result\n    given:\n      a: True\n"
        );

        Assert.False(read.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.InvalidRuleTestFile, read.Diagnostics[0].Code);
        Assert.Contains("'no result'", read.Diagnostics[0].Message, StringComparison.Ordinal);
        Assert.Equal("$.cases[0]", read.Diagnostics[0].Path);
    }

    /// <summary>A case with no name is an error, and the other cases are still checked.</summary>
    [Fact]
    public void ReadCases_CaseWithoutName_IsAnErrorAndTheNextCaseIsStillChecked_Test()
    {
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(
            "rule: a\ncases:\n  - expect:\n      result: True\n  - name: second\n    expect:\n      result: maybe\n"
        );

        Assert.False(read.Succeeded);
        Assert.Equal(["$.cases[0]", "$.cases[1]"], read.Diagnostics.Select(d => d.Path));
    }

    /// <summary>A given value that is not a truth value is an error with the path of the given block.</summary>
    [Fact]
    public void ReadCases_GivenValueNotATruthValue_ReportsTheGivenPath_Test()
    {
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(
            "rule: a\ncases:\n  - name: bad\n    given:\n      a: perhaps\n    expect:\n      result: True\n"
        );

        Assert.False(read.Succeeded);
        Assert.Equal("$.cases[0].given", read.Diagnostics[0].Path);
    }

    /// <summary>A variable file that does not exist is an error and does not throw.</summary>
    [Fact]
    public void ReadCases_MissingVariableFile_IsAnError_Test()
    {
        ToolResult<IReadOnlyList<RuleTestCaseSpec>> read = RuleTestCaseText.ReadCases(
            "rule: a\ncases:\n  - name: data\n    variableFiles:\n      user: nowhere/user.json\n    expect:\n      result: True\n",
            Path.GetTempPath()
        );

        Assert.False(read.Succeeded);
        Assert.Contains("cannot be read", read.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    /// <summary>Removing a case by a name that the file does not have is an error that names the missing case.</summary>
    [Fact]
    public void RemoveCase_UnknownName_NamesTheMissingCase_Test()
    {
        ToolResult<string> removed = RuleTestCaseText.RemoveCase(
            "rule: a\ncases:\n  - name: one\n    expect:\n      result: True\n",
            "two"
        );

        Assert.False(removed.Succeeded);
        Assert.Contains("'two'", removed.Diagnostics[0].Message, StringComparison.Ordinal);
    }
}
