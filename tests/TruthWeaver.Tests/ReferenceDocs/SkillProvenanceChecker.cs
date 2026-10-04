namespace TruthWeaver.Tests.ReferenceDocs;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Keeps <c>.claude/skills</c>, its provenance table (<c>.claude/skills/README.md</c>) and <c>skills-lock.json</c> in step.
/// Every skill directory has exactly one table row, every row names an existing directory, every lock entry names an existing
/// directory, every skill the table classes as vendored has a lock entry, and every lock entry's <c>computedHash</c> equals
/// the hash of the skill folder.
/// </summary>
/// <remarks>
/// The folder hash is the one <c>npx skills</c> computes: SHA-256 over each file's relative path (forward slashes) followed
/// by its raw bytes, files sorted by relative path with a culture-aware comparison (JavaScript <c>localeCompare</c>, so
/// <c>agents/x</c> sorts before <c>AGENTS.md</c>), <c>.git</c> and <c>node_modules</c> excluded. Failures are returned, not
/// thrown, so fixtures can prove the check fails.
/// </remarks>
internal static partial class SkillProvenanceChecker
{
    private const string SkillsDirectory = ".claude/skills";

    private static readonly JsonSerializerOptions LockOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Checks the repository at <paramref name="repositoryRoot"/> with the real folder hash.</summary>
    /// <param name="repositoryRoot">The directory holding <c>TruthWeaver.slnx</c>.</param>
    /// <returns>One message per failure; empty when the folder, the table and the lock agree.</returns>
    internal static IReadOnlyList<string> CheckTree(string repositoryRoot)
    {
        string skillsPath = Path.Combine(repositoryRoot, ".claude", "skills");
        string readmePath = Path.Combine(skillsPath, "README.md");
        string lockPath = Path.Combine(repositoryRoot, "skills-lock.json");
        if (!Directory.Exists(skillsPath) || !File.Exists(readmePath) || !File.Exists(lockPath))
        {
            return [$"{SkillsDirectory}: the skills folder, its README.md or skills-lock.json does not exist"];
        }

        string[] skills = [.. Directory.EnumerateDirectories(skillsPath).Select(d => Path.GetFileName(d))];
        return Check(
            skills,
            File.ReadAllText(readmePath),
            File.ReadAllText(lockPath),
            name => ComputeHash(Path.Combine(skillsPath, name))
        );
    }

    /// <summary>Compares the skill directories, the table and the lock.</summary>
    /// <param name="skillDirectories">The names of the directories under the skills folder.</param>
    /// <param name="readme">The text of <c>.claude/skills/README.md</c>.</param>
    /// <param name="lockJson">The text of <c>skills-lock.json</c>.</param>
    /// <param name="hashOf">Returns the folder hash of the named skill directory.</param>
    /// <returns>One message per failure, each naming the skill and the fix.</returns>
    internal static IReadOnlyList<string> Check(
        IReadOnlyCollection<string> skillDirectories,
        string readme,
        string lockJson,
        Func<string, string> hashOf
    )
    {
        List<string> failures = [];
        Dictionary<string, string> rows = ParseRows(readme);
        Dictionary<string, string> locked = ParseLock(lockJson);

        foreach (string skill in skillDirectories.Order(StringComparer.Ordinal).Where(s => !rows.ContainsKey(s)))
        {
            failures.Add(
                $"{SkillsDirectory}/{skill}: skill '{skill}' has no provenance row; add a row to {SkillsDirectory}/README.md"
            );
        }

        foreach (string skill in rows.Keys.Order(StringComparer.Ordinal).Where(s => !skillDirectories.Contains(s)))
        {
            failures.Add(
                $"{SkillsDirectory}/README.md: the row for '{skill}' names no skill directory; remove the row or restore the skill"
            );
        }

        foreach (string skill in locked.Keys.Order(StringComparer.Ordinal).Where(s => !skillDirectories.Contains(s)))
        {
            failures.Add($"skills-lock.json: the entry for '{skill}' names no skill directory; remove the lock entry");
        }

        foreach ((string skill, string cls) in rows.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            if (!skillDirectories.Contains(skill))
            {
                continue;
            }

            if (!locked.TryGetValue(skill, out string? expected))
            {
                if (cls.Contains("Vendored", StringComparison.Ordinal))
                {
                    failures.Add(
                        $"skills-lock.json: vendored skill '{skill}' has no lock entry; add a lock entry (see Lock file in {SkillsDirectory}/README.md)"
                    );
                }
            }
            else if (!string.Equals(expected, hashOf(skill), StringComparison.Ordinal))
            {
                failures.Add(
                    $"skills-lock.json: the contents of '{skill}' no longer match its computedHash; recompute computedHash as {SkillsDirectory}/README.md describes, or restore the skill files"
                );
            }
        }

        return failures;
    }

    /// <summary>Computes the <c>npx skills</c> folder hash of <paramref name="directory"/>.</summary>
    /// <param name="directory">The skill directory.</param>
    /// <returns>The lower-case hexadecimal SHA-256.</returns>
    internal static string ComputeHash(string directory)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        IEnumerable<string> files = Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(directory, file).Replace('\\', '/'))
            .Where(path => !path.Split('/').Any(part => part is ".git" or "node_modules"))
            .Order(StringComparer.InvariantCulture);

        foreach (string relative in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(relative));
            hash.AppendData(File.ReadAllBytes(Path.Combine(directory, relative)));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    // Maps skill name to the rest of its table row (the Class column is in it). A skill listed twice keeps its first row.
    private static Dictionary<string, string> ParseRows(string readme)
    {
        Dictionary<string, string> rows = new(StringComparer.Ordinal);
        foreach (Match match in RowPattern().Matches(readme))
        {
            rows.TryAdd(match.Groups["name"].Value, match.Groups["rest"].Value);
        }

        return rows;
    }

    private static Dictionary<string, string> ParseLock(string lockJson)
    {
        LockFile? file = JsonSerializer.Deserialize<LockFile>(lockJson, LockOptions);
        return (file?.Skills ?? []).ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ComputedHash ?? string.Empty,
            StringComparer.Ordinal
        );
    }

    [GeneratedRegex(@"^\|\s*`(?<name>[^`]+)`\s*\|(?<rest>.*)$", RegexOptions.Multiline)]
    private static partial Regex RowPattern();

    private sealed record LockFile(Dictionary<string, LockEntry>? Skills);

    private sealed record LockEntry(string? ComputedHash);
}
