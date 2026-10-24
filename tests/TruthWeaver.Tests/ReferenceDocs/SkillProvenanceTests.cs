namespace TruthWeaver.Tests.ReferenceDocs;

using TruthWeaver.Tests.DocExamples;

/// <summary>
/// The project skills folder, its provenance table (<c>.claude/skills/README.md</c>) and <c>skills-lock.json</c> must agree.
/// <see cref="SkillProvenanceChecker"/> is proven to fail on a fixture for each drift case; the real tree is checked as is.
/// </summary>
public sealed class SkillProvenanceTests
{
    private const string Readme = """
        | Skill | Source | Class | License | Version |
        | --- | --- | --- | --- | --- |
        | `csharp-async` | `owner/repo` `skills/csharp-async` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
        | `humanizer` | `blader/humanizer` | Vendored | MIT | 3.1.0 |
        """;

    private const string Lock = """{ "version": 1, "skills": { "humanizer": { "computedHash": "abc" } } }""";

    private static readonly string[] Skills = ["csharp-async", "humanizer"];

    /// <summary>The real skills folder, table and lock agree.</summary>
    [Fact]
    public void CheckTree_RealRepository_ReportsNoFailures_Test()
    {
        IReadOnlyList<string> failures = SkillProvenanceChecker.CheckTree(DocExampleChecker.FindRepositoryRoot());

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A table row per skill and a matching lock hash pass.</summary>
    [Fact]
    public void Check_ConsistentFixture_ReportsNoFailures_Test()
    {
        Assert.Empty(Check());
    }

    /// <summary>A skill directory with no table row is reported by name with the fix.</summary>
    [Fact]
    public void Check_SkillWithoutRow_NamesTheSkillAndTheFix_Test()
    {
        string failure = Assert.Single(Check(skills: [.. Skills, "new-skill"]));

        Assert.Contains("'new-skill'", failure, StringComparison.Ordinal);
        Assert.Contains("add a row", failure, StringComparison.Ordinal);
    }

    /// <summary>A table row for a skill directory that does not exist is reported by name with the fix.</summary>
    [Fact]
    public void Check_RowForMissingSkill_NamesTheSkillAndTheFix_Test()
    {
        string failure = Assert.Single(Check(skills: ["humanizer"]));

        Assert.Contains("'csharp-async'", failure, StringComparison.Ordinal);
        Assert.Contains("remove the row", failure, StringComparison.Ordinal);
    }

    /// <summary>A locked skill whose folder hash differs from its lock entry is reported by name with the fix.</summary>
    [Fact]
    public void Check_LockedSkillWithChangedContents_NamesTheSkillAndTheFix_Test()
    {
        string failure = Assert.Single(Check(hash: _ => "different"));

        Assert.Contains("'humanizer'", failure, StringComparison.Ordinal);
        Assert.Contains("computedHash", failure, StringComparison.Ordinal);
    }

    /// <summary>A lock entry for a skill directory that does not exist is reported by name with the fix.</summary>
    [Fact]
    public void Check_LockEntryForMissingSkill_NamesTheSkillAndTheFix_Test()
    {
        const string lockJson = """{ "skills": { "humanizer": { "computedHash": "abc" }, "gone": { "computedHash": "x" } } }""";

        string failure = Assert.Single(Check(lockJson: lockJson));

        Assert.Contains("'gone'", failure, StringComparison.Ordinal);
        Assert.Contains("remove the lock entry", failure, StringComparison.Ordinal);
    }

    /// <summary>A vendored skill without a lock entry is reported by name with the fix.</summary>
    [Fact]
    public void Check_VendoredSkillWithoutLockEntry_NamesTheSkillAndTheFix_Test()
    {
        string failure = Assert.Single(Check(lockJson: """{ "skills": {} }"""));

        Assert.Contains("'humanizer'", failure, StringComparison.Ordinal);
        Assert.Contains("lock entry", failure, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> Check(
        string[]? skills = null,
        string? lockJson = null,
        Func<string, string>? hash = null
    )
    {
        return SkillProvenanceChecker.Check(skills ?? Skills, Readme, lockJson ?? Lock, hash ?? (_ => "abc"));
    }
}
