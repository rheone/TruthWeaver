#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs

// Checks the packages that dotnet pack wrote: the expected set, a README in each, symbol packages where expected,
// and no unexpected files.
//
// CI runs this after `dotnet pack`. The script only reads the .nupkg and .snupkg files in the package directory
// (the first argument, required). It never pushes or publishes anything. It exits with code 1 when a check fails.
using System.IO.Compression;
using System.Text.RegularExpressions;

if (args.Length != 1)
{
    await Console.Error.WriteLineAsync("check-packages: usage: dotnet run scripts/check-packages.cs -- <package-directory>");
    return 1;
}

string packageDirectory = args[0];
if (!Directory.Exists(packageDirectory))
{
    await Console.Error.WriteLineAsync($"check-packages: the package directory does not exist: {packageDirectory}");
    return 1;
}

// Every package that dotnet pack makes. TruthWeaver.Generators is an analyzer-only package and has no symbol package.
// TruthWeaver.Tool is the .NET tool of the command line; a release publishes it with the other packages.
string[] packages =
[
    "TruthWeaver",
    "TruthWeaver.Abstractions",
    "TruthWeaver.AspNetCore",
    "TruthWeaver.DataSources.Json",
    "TruthWeaver.Extensions.Configuration",
    "TruthWeaver.Generators",
    "TruthWeaver.Policies",
    "TruthWeaver.Predicates",
    "TruthWeaver.Testing",
    "TruthWeaver.Yaml",
    "TruthWeaver.Tool",
    "TruthWeaver.Tool.Tui",
];
string[] noSymbols = ["TruthWeaver.Generators"];

// TruthWeaver.Tool.Tui is the .NET tool of the terminal UI; it is packed, checked and published like the other packages.
// .NET tool packages carry the tool and its dependencies under tools/<tfm>/any/, not lib/.
string[] toolPackages = ["TruthWeaver.Tool", "TruthWeaver.Tool.Tui"];
const string ToolFolder = "tools/net11.0/any/";

// NuGet adds the first four entries to every package. The package id decides the others.
string[] commonEntries =
[
    "_rels/.rels",
    "[Content_Types].xml",
    "package/services/metadata/core-properties/nuget.psmdcp",
    "README.md",
];

// Entry names are compared ignoring case.
StringComparer comparer = StringComparer.OrdinalIgnoreCase;
TimeSpan timeout = TimeSpan.FromSeconds(5);
List<string> failures = [];

Task FailAsync(string message)
{
    failures.Add(message);
    return Console.Error.WriteLineAsync($"check-packages: {message}");
}

static List<string> GetEntries(string path)
{
    using ZipArchive zip = ZipFile.OpenRead(path);
    return [.. zip.Entries.Select(entry => entry.FullName)];
}

List<FileInfo> files =
[
    .. new DirectoryInfo(packageDirectory)
        .EnumerateFiles()
        .Where(file =>
            file.Extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase)
            || file.Extension.Equals(".snupkg", StringComparison.OrdinalIgnoreCase)
        )
        .OrderBy(file => file.Name, comparer),
];

foreach (string id in packages)
{
    // The version part starts with a digit, so TruthWeaver does not match TruthWeaver.Yaml.1.0.0.
    Regex pattern = new($@"^{Regex.Escape(id)}\.(\d.*)\.(nupkg|snupkg)$", RegexOptions.IgnoreCase, timeout);
    List<FileInfo> nupkg =
    [
        .. files.Where(file =>
            pattern.IsMatch(file.Name) && file.Extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase)
        ),
    ];
    List<FileInfo> snupkg =
    [
        .. files.Where(file =>
            pattern.IsMatch(file.Name) && file.Extension.Equals(".snupkg", StringComparison.OrdinalIgnoreCase)
        ),
    ];

    if (nupkg.Count != 1)
    {
        await FailAsync($"{id} : expected one .nupkg, found {nupkg.Count}.");
        continue;
    }

    List<string> entries = GetEntries(nupkg[0].FullName);
    await Console.Out.WriteLineAsync($"==> {nupkg[0].Name}");

    foreach (string required in commonEntries.Where(required => !entries.Contains(required, comparer)))
    {
        await FailAsync($"{id} : the package has no {required}.");
    }

    List<string> allowed;
    if (toolPackages.Contains(id, comparer))
    {
        // Every file of the tool lives in the tool folder; the tool settings and the tool assembly must be there.
        allowed =
        [
            .. commonEntries,
            $"{id}.nuspec",
            .. entries.Where(entry => entry.StartsWith(ToolFolder, StringComparison.OrdinalIgnoreCase)),
        ];
        string[] toolEntries = [$"{ToolFolder}DotnetToolSettings.xml", $"{ToolFolder}{id}.dll"];
        foreach (string required in toolEntries.Where(required => !entries.Contains(required, comparer)))
        {
            await FailAsync($"{id} : the tool package has no {required}.");
        }

        if (snupkg.Count != 1)
        {
            await FailAsync($"{id} : expected one .snupkg, found {snupkg.Count}.");
        }
        else if (!GetEntries(snupkg[0].FullName).Contains($"{ToolFolder}{id}.pdb", comparer))
        {
            await FailAsync($"{id} : the symbol package has no {ToolFolder}{id}.pdb.");
        }
    }
    else if (noSymbols.Contains(id, comparer))
    {
        // An analyzer-only package carries analyzers/ and no lib/ assembly.
        allowed = [.. commonEntries, $"{id}.nuspec", $"analyzers/dotnet/cs/{id}.dll"];
        if (snupkg.Count != 0)
        {
            await FailAsync($"{id} : a symbol package exists, but this package should have none.");
        }
    }
    else
    {
        allowed = [.. commonEntries, $"{id}.nuspec", $"lib/net11.0/{id}.dll", $"lib/net11.0/{id}.xml"];

        // The compiler package ships the rule-tree and decision-record JSON schemas as content.
        if (id.Equals("TruthWeaver", StringComparison.OrdinalIgnoreCase))
        {
            allowed.AddRange(["content/rule-tree.schema.json", "content/decision-record.schema.json"]);
        }

        // The testing package ships the JSON schema of a rule test file as content.
        if (id.Equals("TruthWeaver.Testing", StringComparison.OrdinalIgnoreCase))
        {
            allowed.Add("content/rule-test.schema.json");
        }

        if (snupkg.Count != 1)
        {
            await FailAsync($"{id} : expected one .snupkg, found {snupkg.Count}.");
        }
        else if (!GetEntries(snupkg[0].FullName).Contains($"lib/net11.0/{id}.pdb", comparer))
        {
            await FailAsync($"{id} : the symbol package has no lib/net11.0/{id}.pdb.");
        }
    }

    foreach (string entry in entries.Where(entry => !allowed.Contains(entry, comparer)))
    {
        await FailAsync($"{id} : unexpected file in the package: {entry}");
    }
}

// Any other package in the directory is a project that should not pack.
foreach (
    string name in files
        .Select(file => file.Name)
        .Where(name => !packages.Any(id => Regex.IsMatch(name, $@"^{Regex.Escape(id)}\.\d", RegexOptions.IgnoreCase, timeout)))
)
{
    await FailAsync($"unexpected package file: {name}");
}

if (failures.Count > 0)
{
    await Console.Error.WriteLineAsync($"check-packages: {failures.Count} problem(s).");
    return 1;
}

await Console.Out.WriteLineAsync("check-packages: all packages pass.");
return 0;
