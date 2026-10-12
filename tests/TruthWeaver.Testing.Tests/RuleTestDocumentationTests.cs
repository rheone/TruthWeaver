namespace TruthWeaver.Testing.Tests;

using System.Text.RegularExpressions;

public sealed partial class RuleTestDocumentationTests
{
    /// <summary>
    /// Each YAML example of docs/rule-tests.md is a usable rule test file whose cases all pass, so the page cannot show a
    /// format or a result that the runner does not give.
    /// </summary>
    [Fact]
    public async Task RunAsync_DocumentedExamples_EveryCasePasses_Test()
    {
        string docs = Path.Combine(FindRepositoryRoot(), "docs");
        string page = (
            await File.ReadAllTextAsync(Path.Combine(docs, "rule-tests.md"), TestContext.Current.CancellationToken)
        ).ReplaceLineEndings("\n");

        List<string> examples = [.. YamlBlock().Matches(page).Select(m => m.Groups["yaml"].Value)];

        Assert.Equal(2, examples.Count);
        foreach (string yaml in examples)
        {
            Assert.True(
                RuleTestFile.TryParse(yaml, docs, out RuleTestFile? file, out IReadOnlyList<RuleTestFileError> errors),
                string.Join("\n", errors)
            );
            RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);
            report.ShouldPass();
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TruthWeaver.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("The repository root (TruthWeaver.slnx) was not found.");
    }

    [GeneratedRegex("^```yaml\\n(?<yaml>.*?)^```$", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex YamlBlock();
}
