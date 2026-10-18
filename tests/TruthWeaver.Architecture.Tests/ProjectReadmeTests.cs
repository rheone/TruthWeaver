namespace TruthWeaver.Architecture.Tests;

using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// Keeps the <c>README.md</c> of every project under <c>src/</c> and <c>tests/</c> in step with its project file. A
/// project needs a README, and the <c>## Depends on</c> bullets of that README must name the same projects as the direct
/// <c>ProjectReference</c> items of the project file.
/// </summary>
public sealed partial class ProjectReadmeTests
{
    /// <summary>The single line that a README with no project references holds under <c>## Depends on</c>.</summary>
    private const string NoDependenciesLine = "No TruthWeaver packages.";

    /// <summary>The folder paths of the projects under <c>src/</c> and <c>tests/</c>, one theory row for each.</summary>
    public static TheoryData<string> ProjectFolders()
    {
        TheoryData<string> data = [];
        foreach (string folder in FindProjectFolders())
        {
            data.Add(folder);
        }

        return data;
    }

    /// <summary>
    /// A project has a README whose <c>## Depends on</c> list equals the direct project references of its project file.
    /// The failure message names the project.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProjectFolders))]
    public void ProjectReadme_DependsOnList_MatchesTheProjectReferences_Test(string folder)
    {
        string projectName = Path.GetFileName(folder);
        string readmePath = Path.Combine(folder, "README.md");
        string? readme = File.Exists(readmePath) ? File.ReadAllText(readmePath) : null;

        HashSet<string> references = ReadProjectReferences(Path.Combine(folder, projectName + ".csproj"));
        string[] problems = [.. CheckReadme(readme, references)];

        Assert.True(problems.Length == 0, $"{projectName}: " + string.Join(" ", problems));
    }

    /// <summary>The repository holds the 27 projects that the README rule covers, so a missing folder cannot hide a gap.</summary>
    [Fact]
    public void FindProjectFolders_Repository_ListsEveryProject_Test()
    {
        string[] folders = [.. FindProjectFolders()];

        Assert.Equal(27, folders.Length);
    }

    /// <summary>A project with no README file is a problem.</summary>
    [Fact]
    public void CheckReadme_MissingReadme_ReportsIt_Test()
    {
        string[] problems = [.. CheckReadme(null, [])];

        Assert.Contains(problems, p => p.Contains("no README.md", StringComparison.Ordinal));
    }

    /// <summary>A README that lists a dependency the project file lacks fails and names the extra project.</summary>
    [Fact]
    public void CheckReadme_ListsAReferenceTheProjectLacks_ReportsTheExtra_Test()
    {
        const string Readme =
            "# P\n\n## Depends on\n\n- [TruthWeaver](../TruthWeaver/README.md) for compiling.\n- [TruthWeaver.Yaml](../TruthWeaver.Yaml/README.md) for YAML.\n";

        string[] problems = [.. CheckReadme(Readme, ["TruthWeaver"])];

        string problem = Assert.Single(problems);
        Assert.Contains("TruthWeaver.Yaml", problem, StringComparison.Ordinal);
    }

    /// <summary>A README that omits a reference the project file has fails and names the missing project.</summary>
    [Fact]
    public void CheckReadme_OmitsAReferenceTheProjectHas_ReportsTheMissing_Test()
    {
        const string Readme = "# P\n\n## Depends on\n\n- [TruthWeaver](../TruthWeaver/README.md) for compiling.\n";

        string[] problems = [.. CheckReadme(Readme, ["TruthWeaver", "TruthWeaver.Abstractions"])];

        string problem = Assert.Single(problems);
        Assert.Contains("TruthWeaver.Abstractions", problem, StringComparison.Ordinal);
    }

    /// <summary>A bullet names its project by link text or by link target, and a plain-text bullet by its first project name.</summary>
    [Fact]
    public void CheckReadme_MatchingBullets_AcceptsLinkTextTargetAndPlainName_Test()
    {
        const string Readme =
            "# P\n\n## Depends on\n\n- [`TruthWeaver`](../TruthWeaver/README.md) for compiling.\n"
            + "- [the abstractions](../TruthWeaver.Abstractions/README.md) for the shared types.\n"
            + "- TruthWeaver.Yaml for YAML.\n\n## Notes\n";

        string[] problems = [.. CheckReadme(Readme, ["TruthWeaver", "TruthWeaver.Abstractions", "TruthWeaver.Yaml"])];

        Assert.Empty(problems);
    }

    /// <summary>A project with no references needs exactly the no-dependencies line.</summary>
    [Fact]
    public void CheckReadme_NoReferences_RequiresTheNoDependenciesLine_Test()
    {
        const string Good = "# P\n\n## Depends on\n\nNo TruthWeaver packages.\n";
        const string Bad = "# P\n\n## Depends on\n\nNothing.\n";

        Assert.Empty(CheckReadme(Good, []));
        Assert.NotEmpty(CheckReadme(Bad, []));
    }

    /// <summary>A README without a <c>## Depends on</c> section is a problem.</summary>
    [Fact]
    public void CheckReadme_NoDependsOnSection_ReportsIt_Test()
    {
        string[] problems = [.. CheckReadme("# P\n\n## Purpose\n\nText.\n", [])];

        Assert.Contains(problems, p => p.Contains("## Depends on", StringComparison.Ordinal));
    }

    /// <summary>Compares a README with the project references and returns one message for each problem found.</summary>
    /// <param name="readme">The README text, or <see langword="null"/> when the file does not exist.</param>
    /// <param name="references">The project names that the project file references directly.</param>
    private static List<string> CheckReadme(string? readme, HashSet<string> references)
    {
        List<string> problems = [];
        if (readme is null)
        {
            problems.Add("no README.md.");
            return problems;
        }

        string[]? section = ReadDependsOnSection(readme);
        if (section is null)
        {
            problems.Add("README.md has no '## Depends on' section.");
            return problems;
        }

        string[] bullets = [.. section.Where(l => l.StartsWith("- ", StringComparison.Ordinal))];
        if (references.Count == 0)
        {
            if (!section.Contains(NoDependenciesLine, StringComparer.Ordinal) || bullets.Length > 0)
            {
                problems.Add(
                    $"the project has no ProjectReference, so '## Depends on' must hold only the line '{NoDependenciesLine}'."
                );
            }

            return problems;
        }

        HashSet<string> listed = [];
        foreach (string bullet in bullets)
        {
            string? name = BulletProjectName(bullet);
            if (name is null)
            {
                problems.Add($"the bullet '{bullet}' names no project.");
            }
            else
            {
                listed.Add(name);
            }
        }

        foreach (string extra in listed.Except(references).Order(StringComparer.Ordinal))
        {
            problems.Add($"README.md lists {extra}, which the project file does not reference.");
        }

        foreach (string missing in references.Except(listed).Order(StringComparer.Ordinal))
        {
            problems.Add($"README.md omits {missing}, which the project file references.");
        }

        return problems;
    }

    /// <summary>Returns the non-blank lines under <c>## Depends on</c>, or <see langword="null"/> when the heading is absent.</summary>
    private static string[]? ReadDependsOnSection(string readme)
    {
        string[] lines = readme.ReplaceLineEndings("\n").Split('\n');
        int start = Array.FindIndex(lines, l => l.TrimEnd() == "## Depends on");
        if (start < 0)
        {
            return null;
        }

        return [.. lines.Skip(start + 1).TakeWhile(l => !l.StartsWith('#')).Select(l => l.TrimEnd()).Where(l => l.Length > 0)];
    }

    /// <summary>
    /// Names the project of one bullet: the link text when it is a project name, else the folder in the link target, else
    /// the first project-like word of the bullet.
    /// </summary>
    private static string? BulletProjectName(string bullet)
    {
        Match link = MarkdownLink().Match(bullet);
        if (link.Success)
        {
            string text = link.Groups["text"].Value.Trim('`', ' ');
            if (ProjectName().IsMatch(text))
            {
                return text;
            }

            string[] segments = link.Groups["target"].Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? folder = segments.LastOrDefault(s => ProjectName().IsMatch(s));
            if (folder is not null)
            {
                return folder;
            }
        }

        Match word = ProjectWord().Match(bullet);
        return word.Success ? word.Value : null;
    }

    /// <summary>Reads the project names of the direct <c>ProjectReference</c> items of a project file.</summary>
    private static HashSet<string> ReadProjectReferences(string projectFile)
    {
        XDocument document = XDocument.Load(projectFile);
        return
        [
            .. document
                .Descendants("ProjectReference")
                .Select(r => (string?)r.Attribute("Include"))
                .OfType<string>()
                .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/'))),
        ];
    }

    /// <summary>Finds the project folders below <c>src/</c> and <c>tests/</c>: those that hold a project file of the same name.</summary>
    private static IEnumerable<string> FindProjectFolders()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root?.EnumerateFiles("*.slnx").Any() == false)
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root.EnumerateDirectories("src")
            .Concat(root.EnumerateDirectories("tests"))
            .SelectMany(parent => parent.EnumerateDirectories())
            .Where(folder => File.Exists(Path.Combine(folder.FullName, folder.Name + ".csproj")))
            .Select(folder => folder.FullName)
            .Order(StringComparer.Ordinal);
    }

    /// <summary>A Markdown inline link with its text and target.</summary>
    [GeneratedRegex(@"\[(?<text>[^\]]*)\]\((?<target>[^)]*)\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLink();

    /// <summary>A whole string that is a project name such as <c>TruthWeaver.Yaml</c>.</summary>
    [GeneratedRegex(@"^TruthWeaver(\.[A-Za-z]\w*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ProjectName();

    /// <summary>The first project name in a plain-text bullet: a TruthWeaver project or a sample test project such as <c>Consumer.Tests</c>.</summary>
    [GeneratedRegex(@"TruthWeaver(\.[A-Za-z]\w*)*|[A-Z]\w*\.Tests", RegexOptions.CultureInvariant)]
    private static partial Regex ProjectWord();
}
