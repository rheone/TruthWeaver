#!/usr/bin/env dotnet

// Prints the covered, uncovered and partly covered branch lines of source files from a Cobertura coverage report.
//
// Make the report first, one run per test project (Release matches CI):
//   dotnet test tests/TruthWeaver.Tool.Core.Tests -c Release --coverlet --coverlet-output-format cobertura --results-directory <dir>
// The first argument is the report file, or a folder (the newest *.xml in it is used). Every other argument is the end
// of a source path, for example "Documents/RuleDocument.cs". The output lists the line numbers of the file as it was when
// the report was made, so make the report again after an edit to the source. Run it before and after a test slice to
// show the gain.
//
// Usage: dotnet run scripts/coverage-lines.cs -- <report.xml|folder> <path-end>...
using System.Xml.Linq;

if (args.Length < 2)
{
    await Console.Error.WriteLineAsync("Usage: dotnet run scripts/coverage-lines.cs -- <report.xml|folder> <path-end>...");
    return 2;
}

string report = args[0];
if (Directory.Exists(report))
{
    string? newest = Directory
        .EnumerateFiles(report, "*.xml", SearchOption.AllDirectories)
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .FirstOrDefault();
    if (newest is null)
    {
        await Console.Error.WriteLineAsync($"No *.xml report in '{report}'.");
        return 2;
    }

    report = newest;
}

XDocument document = XDocument.Load(report);
foreach (string end in args.Skip(1))
{
    var lines = document
        .Descendants("class")
        .Where(c =>
            ((string?)c.Attribute("filename") ?? string.Empty).Replace('\\', '/').EndsWith(end, StringComparison.Ordinal)
        )
        .SelectMany(c => c.Element("lines")?.Elements("line") ?? [])
        .GroupBy(l => (int)l.Attribute("number")!)
        .Select(g =>
            (
                Number: g.Key,
                Hits: g.Sum(l => (int)l.Attribute("hits")!),
                Condition: g.Select(l => (string?)l.Attribute("condition-coverage")).FirstOrDefault(c => c is not null)
            )
        )
        .OrderBy(l => l.Number)
        .ToList();

    int covered = lines.Count(l => l.Hits > 0);
    string uncovered = string.Join(",", lines.Where(l => l.Hits == 0).Select(l => l.Number));
    string partial = string.Join(
        ",",
        lines
            .Where(l => l.Hits > 0 && l.Condition is { } c && !c.StartsWith("100%", StringComparison.Ordinal))
            .Select(l => l.Number)
    );
    await Console.Out.WriteLineAsync($"{end}: lines {covered}/{lines.Count}");
    await Console.Out.WriteLineAsync($"  uncovered: {uncovered}");
    await Console.Out.WriteLineAsync($"  partial-branch: {partial}");
}

return 0;
