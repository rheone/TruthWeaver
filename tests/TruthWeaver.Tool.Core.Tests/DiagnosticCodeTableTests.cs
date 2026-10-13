namespace TruthWeaver.Tool.Core.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>
/// The diagnostic code table has one row for each code constant, and its rows match the code tables of the
/// documentation, so the SARIF rule ids come from one table.
/// </summary>
public sealed partial class DiagnosticCodeTableTests
{
    /// <summary>Every constant of the compiler codes and the tool codes has a row with its name, and there is no other row.</summary>
    [Fact]
    public void All_Rows_MatchTheCodeConstants_Test()
    {
        IEnumerable<(string Code, string Name)> constants = Constants(typeof(DiagnosticCodes))
            .Concat(Constants(typeof(ToolDiagnosticCodes)));

        Assert.Equal(
            constants.OrderBy(c => c.Code, StringComparer.Ordinal),
            DiagnosticCodeTable.All.Select(row => (row.Code, row.Name))
        );
    }

    /// <summary>Each row has the name and the severity of its row in the code catalog or in the tool core page.</summary>
    [Fact]
    public void All_Rows_MatchTheDocumentedTables_Test()
    {
        Dictionary<string, (string Name, string Severity)> documented = [];
        foreach (string page in new[] { "docs/strong-k3/specification/diagnostics.md", "docs/tool-core.md" })
        {
            foreach (Match row in TableRow().Matches(File.ReadAllText(Repository.Path(page.Split('/')))))
            {
                documented[row.Groups["code"].Value] = (row.Groups["name"].Value, row.Groups["severity"].Value);
            }
        }

        Assert.Equal(
            documented.OrderBy(d => d.Key, StringComparer.Ordinal).Select(d => (d.Key, d.Value.Name, d.Value.Severity)),
            DiagnosticCodeTable.All.Select(row => (row.Code, row.Name, row.DefaultSeverity.ToString()))
        );
    }

    /// <summary>A code is found by its text, and an unknown code is not found.</summary>
    [Fact]
    public void TryGet_KnownAndUnknownCode_FindsOnlyTheKnownCode_Test()
    {
        Assert.True(DiagnosticCodeTable.TryGet(ToolDiagnosticCodes.TreeSchemaViolation, out DiagnosticCodeInfo? info));
        Assert.Equal(nameof(ToolDiagnosticCodes.TreeSchemaViolation), info.Name);
        Assert.False(DiagnosticCodeTable.TryGet("TRE9999", out _));
    }

    private static IEnumerable<(string Code, string Name)> Constants(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => ((string)f.GetRawConstantValue()!, f.Name));
    }

    [GeneratedRegex(
        @"^\| `(?<code>(?:TRE|TWT)\d{4})` \| `(?<name>\w+)` \| (?<severity>Error|Warning|Info) \|",
        RegexOptions.Multiline | RegexOptions.CultureInvariant
    )]
    private static partial Regex TableRow();
}
