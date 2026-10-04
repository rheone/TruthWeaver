namespace TruthWeaver.Tests.ReferenceDocs;

using System.Text.RegularExpressions;

/// <summary>
/// Enforces the documentation standard in <c>CLAUDE.md</c> on the in-scope Markdown files, so a reference document that
/// carries project history or points into a history document fails the build with a <c>file:line</c> message.
/// </summary>
/// <remarks>
/// <para>
/// The in-scope set is every <c>*.md</c> in the repository root except <c>CHANGELOG.md</c>, every <c>README.md</c> outside the
/// stop list, and every Markdown file those link to, followed recursively. The stop list is <c>docs/adr/</c>,
/// <c>.scratch/</c>, <c>.agents/</c>, <c>.claude/</c> and <c>CHANGELOG.md</c>: history, work-tracking, tool and third-party
/// files that describe the past by design. A line <c>&lt;!-- docs-lint: on --&gt;</c> opts a file in wherever it is, and
/// <c>&lt;!-- docs-lint: off --&gt;</c> opts a file out.
/// </para>
/// <para>
/// Rules, applied outside fenced blocks: no em dash anywhere; outside the agent files (<c>CLAUDE.md</c>, <c>AGENTS.md</c>) no
/// "ticket", "ADR-" or "open question" outside code spans, and no link into the stop list except from the four root
/// navigation files; a file under <c>docs/strong-k3/</c> links only to files under <c>docs/strong-k3/</c>.
/// </para>
/// <para>
/// <see cref="DocumentationLintBaseline"/> lists in-scope files that are not yet clean. A baseline file is skipped, and it
/// fails the check once it is clean, so the list can only shrink.
/// </para>
/// </remarks>
internal static partial class DocumentationLint
{
    private const string ReferencePrefix = "docs/strong-k3/";

    private static readonly string[] StopList = ["docs/adr/", ".scratch/", ".agents/", ".claude/"];

    // Directories that hold build output, caches or tool state, never project documentation.
    private static readonly string[] NonSourceSegments = ["bin", "obj", "node_modules", ".git", ".codegraph", ".vs"];

    // Root files that describe the project and its tooling; they may point at ADRs and tickets.
    private static readonly string[] NavigationFiles = ["README.md", "CONTEXT.md", "CLAUDE.md", "AGENTS.md"];

    // Files written for coding agents; the history-word rules do not apply to them.
    private static readonly string[] AgentFiles = ["CLAUDE.md", "AGENTS.md"];

    /// <summary>Finds the in-scope Markdown files of the repository at <paramref name="repositoryRoot"/>.</summary>
    /// <param name="repositoryRoot">The directory holding <c>TruthWeaver.slnx</c>.</param>
    /// <returns>Repository-relative paths with <c>/</c> separators, in order.</returns>
    internal static IReadOnlyList<string> InScope(string repositoryRoot)
    {
        Dictionary<string, string> all = [];
        foreach (string file in Directory.EnumerateFiles(repositoryRoot, "*.md", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
            if (!relative.Split('/').Any(segment => NonSourceSegments.Contains(segment, StringComparer.Ordinal)))
            {
                all[relative] = file;
            }
        }

        HashSet<string> off =
        [
            .. all.Where(entry => HasMarker(File.ReadAllText(entry.Value), "off")).Select(entry => entry.Key),
        ];
        SortedSet<string> inScope = new(StringComparer.Ordinal);
        Queue<string> pending = [];

        void Add(string path)
        {
            if (!off.Contains(path) && inScope.Add(path))
            {
                pending.Enqueue(path);
            }
        }

        foreach (string path in all.Keys)
        {
            bool root = !path.Contains('/', StringComparison.Ordinal);
            bool seed =
                (root && path != "CHANGELOG.md")
                || (Path.GetFileName(path) == "README.md" && !InStopList(path))
                || HasMarker(File.ReadAllText(all[path]), "on");
            if (seed)
            {
                Add(path);
            }
        }

        while (pending.Count > 0)
        {
            string path = pending.Dequeue();
            foreach (string target in LinkedFiles(path, File.ReadAllText(all[path])))
            {
                if (all.ContainsKey(target) && !InStopList(target) && target != "CHANGELOG.md")
                {
                    Add(target);
                }
            }
        }

        return [.. inScope];
    }

    /// <summary>Checks every in-scope file, honoring the baseline.</summary>
    /// <param name="repositoryRoot">The directory holding <c>TruthWeaver.slnx</c>.</param>
    /// <param name="baseline">In-scope files that are not yet clean.</param>
    /// <returns>One message per failure; empty when the documentation meets the standard.</returns>
    internal static IReadOnlyList<string> CheckTree(string repositoryRoot, IReadOnlySet<string> baseline)
    {
        IReadOnlyList<string> inScope = InScope(repositoryRoot);

        bool Exists(string path)
        {
            string full = Path.Combine(repositoryRoot, path);
            return File.Exists(full) || Directory.Exists(full);
        }

        string? Read(string path)
        {
            string full = Path.Combine(repositoryRoot, path);
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }

        List<string> failures = [];
        foreach (string path in inScope)
        {
            IReadOnlyList<string> violations = Check(path, File.ReadAllText(Path.Combine(repositoryRoot, path)), Exists, Read);
            if (!baseline.Contains(path))
            {
                failures.AddRange(violations);
            }
            else if (violations.Count == 0)
            {
                failures.Add($"{path}: the file meets the standard, so remove it from {nameof(DocumentationLintBaseline)}");
            }
        }

        failures.AddRange(
            baseline
                .Where(path => !inScope.Contains(path, StringComparer.Ordinal))
                .Order(StringComparer.Ordinal)
                .Select(path =>
                    $"{path}: the baseline entry is not an in-scope file, so remove it from {nameof(DocumentationLintBaseline)}"
                )
        );
        return failures;
    }

    /// <summary>Checks one Markdown document against the standard.</summary>
    /// <param name="path">The document's repository-relative path, with <c>/</c> separators.</param>
    /// <param name="markdown">The document text.</param>
    /// <param name="exists">Whether a repository-relative path names an existing file or directory.</param>
    /// <param name="read">Reads a repository-relative Markdown file, or returns <see langword="null"/> when it does not exist.</param>
    /// <returns>One <c>path:line: message</c> per violation; empty when the document is clean.</returns>
    internal static IReadOnlyList<string> Check(
        string path,
        string markdown,
        Func<string, bool> exists,
        Func<string, string?> read
    )
    {
        List<string> failures = [];
        string[] lines = SplitLines(markdown);
        bool[] inFence = K3ReferenceChecker.FenceMap(lines);
        bool agentFile = AgentFiles.Contains(path, StringComparer.Ordinal);
        bool navigationFile = NavigationFiles.Contains(path, StringComparer.Ordinal);
        string directory = DirectoryOf(path);

        K3ReferenceChecker.CheckLinks(
            path,
            lines,
            inFence,
            exists,
            read,
            (line, message) => failures.Add($"{path}:{line}: {message}")
        );

        for (int i = 0; i < lines.Length; i++)
        {
            if (inFence[i])
            {
                continue;
            }

            int number = i + 1;
            if (lines[i].Contains('—', StringComparison.Ordinal))
            {
                failures.Add($"{path}:{number}: em dash; use a period, comma, colon or parentheses");
            }

            string prose = CodeSpanRegex().Replace(lines[i], static m => new string(' ', m.Length));
            if (!agentFile && HistoryWordRegex().Match(prose) is { Success: true } word)
            {
                failures.Add($"{path}:{number}: '{word.Value}' is project history; state the resulting behavior instead");
            }

            foreach (Match match in LinkRegex().Matches(prose))
            {
                string target = Uri.UnescapeDataString(match.Groups["target"].Value);
                if (SchemeRegex().IsMatch(target))
                {
                    continue;
                }

                int hash = target.IndexOf('#', StringComparison.Ordinal);
                string filePart = hash < 0 ? target : target[..hash];
                string? resolved = filePart.Length == 0 ? path : K3ReferenceChecker.Resolve(directory, filePart);

                if (!agentFile && !navigationFile && resolved is not null && InStopList(resolved))
                {
                    failures.Add(
                        $"{path}:{number}: link '{target}' points into a history document; state the behavior instead"
                    );
                }

                bool inside = resolved?.StartsWith(ReferencePrefix, StringComparison.Ordinal) ?? false;
                if (path.StartsWith(ReferencePrefix, StringComparison.Ordinal) && !inside)
                {
                    failures.Add(
                        $"{path}:{number}: link '{target}' leaves docs/strong-k3; reference pages link only to reference pages"
                    );
                }
            }
        }

        return failures;
    }

    /// <summary>Checks one Markdown document with every relative link assumed to resolve; the link rules need the repository.</summary>
    internal static IReadOnlyList<string> Check(string path, string markdown)
    {
        return Check(path, markdown, static _ => true, static _ => null);
    }

    private static bool InStopList(string path)
    {
        return path == "CHANGELOG.md" || StopList.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static string DirectoryOf(string path)
    {
        return path.Contains('/', StringComparison.Ordinal) ? path[..path.LastIndexOf('/')] : string.Empty;
    }

    private static string[] SplitLines(string markdown)
    {
        return [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];
    }

    private static bool HasMarker(string markdown, string state)
    {
        string[] lines = SplitLines(markdown);
        bool[] inFence = K3ReferenceChecker.FenceMap(lines);
        return lines.Where((_, i) => !inFence[i]).Any(line => line.Trim() == $"<!-- docs-lint: {state} -->");
    }

    /// <summary>The repository-relative Markdown files a document links to.</summary>
    private static IEnumerable<string> LinkedFiles(string path, string markdown)
    {
        string[] lines = SplitLines(markdown);
        bool[] inFence = K3ReferenceChecker.FenceMap(lines);
        string directory = DirectoryOf(path);
        for (int i = 0; i < lines.Length; i++)
        {
            if (inFence[i])
            {
                continue;
            }

            string prose = CodeSpanRegex().Replace(lines[i], static m => new string(' ', m.Length));
            foreach (Match match in LinkRegex().Matches(prose))
            {
                string target = Uri.UnescapeDataString(match.Groups["target"].Value);
                if (SchemeRegex().IsMatch(target))
                {
                    continue;
                }

                int hash = target.IndexOf('#', StringComparison.Ordinal);
                string filePart = hash < 0 ? target : target[..hash];
                if (
                    filePart.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    && K3ReferenceChecker.Resolve(directory, filePart) is { } resolved
                )
                {
                    yield return resolved;
                }
            }
        }
    }

    [GeneratedRegex(@"\[[^\]]*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"`[^`]*`")]
    private static partial Regex CodeSpanRegex();

    [GeneratedRegex(@"^(?:[A-Za-z][A-Za-z0-9+.\-]*:|//)")]
    private static partial Regex SchemeRegex();

    [GeneratedRegex(@"\bADR-\d|\bopen questions?\b|\btickets?\b", RegexOptions.IgnoreCase)]
    private static partial Regex HistoryWordRegex();
}
