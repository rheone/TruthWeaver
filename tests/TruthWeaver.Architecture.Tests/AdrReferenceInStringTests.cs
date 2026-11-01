namespace TruthWeaver.Architecture.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// A string a shipped assembly returns or logs reaches the library consumer at run time, so it explains behavior in
/// its own words. It must not cite an ADR, which is a history file that a consumer cannot open.
/// </summary>
public sealed partial class AdrReferenceInStringTests
{
    /// <summary>No string literal under <c>src/</c> cites an ADR.</summary>
    [Fact]
    public void ShippedSource_StringLiterals_CiteNoAdr_Test()
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
                .Where(file =>
                    !file.FullName.Contains(
                        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal
                    )
                    && !file.FullName.Contains(
                        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal
                    )
                )
                .SelectMany(file =>
                    File.ReadLines(file.FullName)
                        .Select((line, index) => (Line: line, Number: index + 1))
                        .Where(entry =>
                            !entry.Line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                            && AdrInStringLiteral().IsMatch(entry.Line)
                        )
                        .Select(entry => $"{Path.GetRelativePath(root.FullName, file.FullName)}:{entry.Number}")
                ),
        ];

        Assert.True(
            offenders.Length == 0,
            "Remove the ADR citation from these string literals:\n" + string.Join('\n', offenders)
        );
    }

    /// <summary>An <c>ADR-</c> citation between a pair of double quotes on a line that is not a comment line.</summary>
    [GeneratedRegex("\"[^\"]*ADR-[0-9][^\"]*\"", RegexOptions.CultureInvariant)]
    private static partial Regex AdrInStringLiteral();
}
