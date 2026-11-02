namespace TruthWeaver.Tool.Core.Tests.Patching;

using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Patching;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class EmbeddedRuleSessionTests : IDisposable
{
    private const string YamlFixture = "patch/commented.yaml";
    private readonly string folder = Directory.CreateTempSubdirectory("tw-session-").FullName;

    /// <summary>Removes the temporary folder.</summary>
    public void Dispose()
    {
        Directory.Delete(this.folder, recursive: true);
    }

    /// <summary>A rule that changed in the file after the session read it is reported by the prepared write.</summary>
    [Fact]
    public void Prepare_RuleChangedOnDiskSinceOpening_ReportsChangedOnDisk_Test()
    {
        string file = this.CopyFixture();
        EmbeddedRuleSession session = EmbeddedRuleSession.Open(file, "rules.publish").Value!;
        File.WriteAllText(file, File.ReadAllText(file).Replace("role: editor", "role: owner", StringComparison.Ordinal));
        RuleDocument edited = Edit(session, "isActive", "isEnabled");

        ToolResult<EmbeddedRuleSession.PreparedWrite> prepared = session.Prepare(
            edited,
            WriteBackMode.Patch,
            ToolSettings.Default
        );

        Assert.True(prepared.Succeeded);
        Assert.True(prepared.Value.ChangedOnDisk);
        Assert.Equal(RulePatchKind.Patched, prepared.Value.Kind);
    }

    /// <summary>A rule that did not change in the file is not reported as changed on disk.</summary>
    [Fact]
    public void Prepare_FileUnchangedSinceOpening_ReportsNoConflict_Test()
    {
        string file = this.CopyFixture();
        EmbeddedRuleSession session = EmbeddedRuleSession.Open(file, "rules.publish").Value!;

        ToolResult<EmbeddedRuleSession.PreparedWrite> prepared = session.Prepare(
            Edit(session, "isActive", "isEnabled"),
            WriteBackMode.Patch,
            ToolSettings.Default
        );

        Assert.False(prepared.Value!.ChangedOnDisk);
    }

    /// <summary>After a commit, the written rule is the baseline, so a second write sees no conflict.</summary>
    [Fact]
    public void Commit_AfterWrite_ResetsTheBaseline_Test()
    {
        string file = this.CopyFixture();
        EmbeddedRuleSession session = EmbeddedRuleSession.Open(file, "rules.publish").Value!;
        RuleDocument edited = Edit(session, "isActive", "isEnabled");
        EmbeddedRuleSession.PreparedWrite first = session.Prepare(edited, WriteBackMode.Patch, ToolSettings.Default).Value!;

        Assert.True(session.Commit(first).Succeeded);
        EmbeddedRuleSession.PreparedWrite second = session
            .Prepare(Edit(session, "isEnabled", "isActive"), WriteBackMode.Patch, ToolSettings.Default)
            .Value!;

        Assert.Contains("isEnabled", File.ReadAllText(file), StringComparison.Ordinal);
        Assert.False(second.ChangedOnDisk);
    }

    /// <summary>Round-trip mode rewrites the whole document.</summary>
    [Fact]
    public void Prepare_RoundTripMode_RewritesTheDocument_Test()
    {
        string file = this.CopyFixture();
        EmbeddedRuleSession session = EmbeddedRuleSession.Open(file, "rules.publish").Value!;

        ToolResult<EmbeddedRuleSession.PreparedWrite> prepared = session.Prepare(
            Edit(session, "isActive", "isEnabled"),
            WriteBackMode.RoundTrip,
            ToolSettings.Default
        );

        Assert.Equal(RulePatchKind.DocumentRewritten, prepared.Value!.Kind);
    }

    private static RuleDocument Edit(EmbeddedRuleSession session, string from, string to)
    {
        RuleDocument document = session.CreateDocument(ToolSettings.Default);
        document.SetText(document.Text.Replace(from, to, StringComparison.Ordinal));
        return document;
    }

    private string CopyFixture()
    {
        string file = Path.Combine(this.folder, "policies.yaml");
        File.Copy(Fixtures.Path(YamlFixture), file);
        return file;
    }
}
