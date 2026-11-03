namespace TruthWeaver.Architecture.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// YamlDotNet reports most malformed YAML as <c>YamlException</c>, but its scanner throws <c>InvalidOperationException</c>,
/// with no position, for text such as an unclosed flow sequence that is followed by more keys. A reader that catches only
/// <c>YamlException</c> lets that exception escape instead of returning a diagnostic.
/// </summary>
public sealed partial class YamlScannerCatchTests
{
    /// <summary>A source file that catches <c>YamlException</c> also handles <c>InvalidOperationException</c>.</summary>
    [Fact]
    public void ShippedSource_FilesThatCatchYamlException_AlsoHandleTheScannerException_Test()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root?.EnumerateFiles("*.slnx").Any() == false)
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        string[] offenders =
        [
            .. root.EnumerateDirectories("src")
                .SelectMany(folder => folder.EnumerateFiles("*.cs", SearchOption.AllDirectories))
                .Where(file => !IsBuildOutput(file.FullName))
                .Select(file => (File: file, Text: File.ReadAllText(file.FullName)))
                .Where(entry => CatchesYamlException(entry.Text) && !HandlesScannerException(entry.Text))
                .Select(entry => Path.GetRelativePath(root.FullName, entry.File.FullName)),
        ];

        string message =
            "Add a catch for InvalidOperationException (the YamlDotNet scanner exception) beside the YamlException catch in:\n"
            + string.Join('\n', offenders);
        Assert.True(offenders.Length == 0, message);
    }

    private static bool CatchesYamlException(string text)
    {
        // Both forms: `catch (YamlException ...)` and `catch (Exception ex) when (ex is YamlException ...)`.
        return text.Contains("catch (YamlException", StringComparison.Ordinal)
            || text.Contains("ex is YamlException", StringComparison.Ordinal);
    }

    // A catch site, not a comment that names the type: `catch (InvalidOperationException` or a `when (ex is ... InvalidOperationException` filter.
    private static bool HandlesScannerException(string text)
    {
        return ScannerCatch().IsMatch(text);
    }

    [GeneratedRegex(
        @"catch\s*\(\s*InvalidOperationException|ex is [^)]*\bInvalidOperationException\b",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex ScannerCatch();

    private static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
