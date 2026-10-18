namespace TruthWeaver.Tool.Tui.Tests.Sessions;

using TruthWeaver.Abstractions;
using TruthWeaver.Testing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The rule test file of the workbench with no view: saving the assignment as a case, running the file on the open rule,
/// loading a case back and removing it. The files that the workbench writes are read by the rule test runner of
/// <c>truthweaver test</c>.
/// </summary>
public sealed class RuleTestSessionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    public RuleTestSessionTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>Saving the first case writes a new file that the rule test runner reads and passes.</summary>
    [Fact]
    public async Task SaveCase_NewFile_WritesAFileThatPassesTheRunner_Test()
    {
        Workbench workbench = Create("isActive AND isAdmin");
        workbench.Assignment.Set("isActive", TruthValue.True);
        workbench.Assignment.Set("isAdmin", TruthValue.False);
        string path = this.PathOf("access.test.yaml");
        workbench.RuleTests.FilePath = path;
        workbench.RuleTests.CaseName = "an admin check fails";

        workbench.SaveCase();

        Assert.Contains("Saved the case 'an admin check fails'", workbench.Status, StringComparison.Ordinal);
        RuleTestReport report = await RuleTestFile.RunAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        report.ShouldPass();
        Assert.Equal(["an admin check fails"], report.Results.Select(r => r.Case.Name));
        Assert.Equal(TruthValue.False, report.Results[0].Case.Expect.Result);
    }

    /// <summary>Saving a second case keeps the first case and the rule of the file.</summary>
    [Fact]
    public async Task SaveCase_ExistingFile_AddsTheCaseAndKeepsTheOthers_Test()
    {
        Workbench workbench = Create("isActive");
        string path = this.PathOf("access.test.yaml");
        workbench.RuleTests.FilePath = path;
        workbench.Assignment.Set("isActive", TruthValue.True);
        workbench.RuleTests.CaseName = "active";
        workbench.SaveCase();
        workbench.Assignment.Set("isActive", TruthValue.False);
        workbench.RuleTests.CaseName = "inactive";

        workbench.SaveCase();

        RuleTestReport report = await RuleTestFile.RunAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        report.ShouldPass();
        Assert.Equal(["active", "inactive"], report.Results.Select(r => r.Case.Name));
        Assert.Equal(["active", "inactive"], workbench.RuleTests.Rows.Select(r => r.Name));
    }

    /// <summary>Saving under an existing name replaces that case with the new assignment.</summary>
    [Fact]
    public async Task SaveCase_SameName_ReplacesTheCase_Test()
    {
        Workbench workbench = Create("isActive");
        string path = this.PathOf("access.test.yaml");
        workbench.RuleTests.FilePath = path;
        workbench.RuleTests.CaseName = "case";
        workbench.Assignment.Set("isActive", TruthValue.True);
        workbench.SaveCase();
        workbench.Assignment.Set("isActive", TruthValue.False);

        workbench.SaveCase();

        RuleTestReport report = await RuleTestFile.RunAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        report.ShouldPass();
        RuleTestResult only = Assert.Single(report.Results);
        Assert.Equal(TruthValue.False, only.Case.Expect.Result);
    }

    /// <summary>A case with data writes the variables, and the runner passes it.</summary>
    [Fact]
    public async Task SaveCase_DataTerm_WritesTheVariables_Test()
    {
        Workbench workbench = Create("inRegion(region: from(\"user\", \"$.region\"))");
        _ = workbench.Assignment.SetVariables("{\"user\": {\"region\": \"EU\"}}");
        workbench.Assignment.Set("inRegion(region: \"EU\")", TruthValue.True);
        string path = this.PathOf("region.test.yaml");
        workbench.RuleTests.FilePath = path;
        workbench.RuleTests.CaseName = "an EU user";

        workbench.SaveCase();

        RuleTestReport report = await RuleTestFile.RunAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        report.ShouldPass();
        Assert.Equal(TruthValue.True, report.Results[0].Decision!.Result);
    }

    /// <summary>A rule with errors cannot be saved as a case, and no file is written.</summary>
    [Fact]
    public void SaveCase_RuleWithErrors_WritesNothing_Test()
    {
        Workbench workbench = Create("isActive");
        workbench.Document.SetText("isActive AND (");
        string path = this.PathOf("bad.test.yaml");
        workbench.RuleTests.FilePath = path;
        workbench.RuleTests.CaseName = "case";

        workbench.SaveCase();

        Assert.False(File.Exists(path));
        Assert.Contains("Fix the errors", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A save with no path or no name says what is missing.</summary>
    [Fact]
    public void SaveCase_NoPathOrName_SaysWhatIsMissing_Test()
    {
        Workbench workbench = Create("isActive");

        workbench.SaveCase();
        string noPath = workbench.Status;
        workbench.RuleTests.FilePath = this.PathOf("a.test.yaml");
        workbench.SaveCase();

        Assert.Contains("path", noPath, StringComparison.Ordinal);
        Assert.Contains("name", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Running the file lists each case as pass or fail on the open rule, with expected and actual values.</summary>
    [Fact]
    public async Task RunRuleTestsAsync_RuleChangedSinceTheCase_ListsTheFailureWithExpectedAndActual_Test()
    {
        Workbench workbench = Create("isActive");
        workbench.RuleTests.FilePath = this.PathOf("access.test.yaml");
        workbench.RuleTests.CaseName = "active";
        workbench.Assignment.Set("isActive", TruthValue.True);
        workbench.SaveCase();
        await workbench.RunRuleTestsAsync();
        string[] before = [.. workbench.RuleTests.Rows.Select(r => r.Line)];

        // The case says that isActive True gives True; the changed rule negates it, so the case fails on the open rule.
        workbench.Document.SetText("NOT isActive");
        await workbench.RunRuleTestsAsync();

        Assert.Equal(["PASS  active  (True, 0 fault(s))"], before);
        RuleTestRow row = Assert.Single(workbench.RuleTests.Rows);
        Assert.False(row.Passed);
        Assert.Equal("FAIL  active  expected True, 0 fault(s); got False, 0 fault(s)", row.Line);
        Assert.Equal("0 of 1 case(s) passed.", workbench.Status);
    }

    /// <summary>Running needs a rule that compiles.</summary>
    [Fact]
    public async Task RunRuleTestsAsync_RuleWithErrors_DoesNotRun_Test()
    {
        Workbench workbench = Create("isActive");
        workbench.RuleTests.FilePath = this.PathOf("access.test.yaml");
        workbench.RuleTests.CaseName = "active";
        workbench.SaveCase();
        workbench.Document.SetText("isActive AND (");

        await workbench.RunRuleTestsAsync();

        Assert.Contains("Fix the errors", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Loading a failing case puts its values and variables in the assignment, and the decision follows.</summary>
    [Fact]
    public async Task LoadCase_FailingCase_LoadsItsAssignment_Test()
    {
        string path = this.PathOf("region.test.yaml");
        await File.WriteAllTextAsync(
            path,
            "rule: |\n  inRegion(region: from(\"user\", \"$.region\"))\ncases:\n  - name: a US user\n    given:\n      'inRegion(region: \"US\")': True\n    variables:\n      user:\n        region: US\n    expect:\n      result: False\n",
            TestContext.Current.CancellationToken
        );
        Workbench workbench = Create("inRegion(region: from(\"user\", \"$.region\"))");
        workbench.RuleTests.FilePath = path;
        await workbench.RunRuleTestsAsync();
        RuleTestRow failing = Assert.Single(workbench.RuleTests.Rows);

        workbench.LoadCase(0);

        Assert.False(failing.Passed);
        Assert.Equal(TruthValue.True, workbench.Assignment.ValueOf("inRegion(region: \"US\")"));
        Assert.Contains("US", workbench.Assignment.VariablesText, StringComparison.Ordinal);
        Assert.Equal(TruthValue.True, workbench.Outcome.Decision!.Result);
        Assert.Equal("a US user", workbench.RuleTests.CaseName);
    }

    /// <summary>Removing a case rewrites the file without it.</summary>
    [Fact]
    public async Task DeleteCase_TwoCases_RemovesTheSelectedOne_Test()
    {
        Workbench workbench = Create("isActive");
        string path = this.PathOf("access.test.yaml");
        workbench.RuleTests.FilePath = path;
        workbench.RuleTests.CaseName = "a";
        workbench.SaveCase();
        workbench.RuleTests.CaseName = "b";
        workbench.SaveCase();

        workbench.DeleteCase(0);

        RuleTestReport report = await RuleTestFile.RunAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(["b"], report.Results.Select(r => r.Case.Name));
    }

    /// <summary>A file that cannot be read says so and lists no case.</summary>
    [Fact]
    public void OpenRuleTestFile_MissingFile_SaysSo_Test()
    {
        Workbench workbench = Create("isActive");
        workbench.RuleTests.FilePath = this.PathOf("missing.test.yaml");

        workbench.OpenRuleTestFile();

        Assert.Empty(workbench.RuleTests.Rows);
        Assert.NotEmpty(workbench.Status);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private static Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
    }

    private string PathOf(string name)
    {
        return Path.Combine(this.directory, name);
    }
}
