namespace TruthWeaver.Tool.Tui.Tests.Views;

using System.Reflection;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Keeps the export dialog level with the command line (<c>export</c> and <c>print</c>): the same artifacts and the same
/// <see cref="ExportOptions"/>. The command line side of this check is <c>ExportParityTests</c> in the command line tests.
/// </summary>
public sealed class ExportParityTests
{
    /// <summary>
    /// How the dialog reaches each <see cref="ExportOptions"/> property. A new property fails the test until it is exposed
    /// here and in the command line, or listed as front-end specific.
    /// </summary>
    private static readonly Dictionary<string, string> Exposed = new(StringComparer.Ordinal)
    {
        [nameof(ExportOptions.Settings)] = "The settings of the workbench.",
        [nameof(ExportOptions.AllowLargerTruthTable)] = "The larger truth tables box (ExportDialog.AllowLarger).",
        [nameof(ExportOptions.TruthTableTermLimit)] = "The term limit field (ExportDialog.TermLimit).",
        [nameof(ExportOptions.Images)] = "The image entries and the export of every artifact to a folder.",
    };

    /// <summary>The choices that only the workbench has, so the command line does not need them.</summary>
    private static readonly string[] WorkbenchOnly =
    [
        "The clipboard as the destination (Copy).",
        "The suggested file name and the path field.",
    ];

    /// <summary>Every artifact id of the catalog has an entry in the dialog.</summary>
    [Fact]
    public void Entries_EveryCatalogArtifact_HasAnEntry_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, new ScriptedPrompts());
        ExportController controller = new(workbench, new ScriptedServices().Services);

        HashSet<string> listed = [.. controller.Entries().Select(e => e.Artifact?.Id).OfType<string>()];

        string[] missing = [.. ArtifactCatalog.All.Select(a => a.Id).Where(id => !listed.Contains(id))];
        Assert.Empty(missing);
    }

    /// <summary>Every property of <see cref="ExportOptions"/> is exposed by the dialog, and the list has no stale name.</summary>
    [Fact]
    public void ExportOptions_EveryProperty_IsExposedByTheDialog_Test()
    {
        string[] properties =
        [
            .. typeof(ExportOptions)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != "EqualityContract")
                .Select(p => p.Name),
        ];

        Assert.Equal(properties.Order(StringComparer.Ordinal), Exposed.Keys.Order(StringComparer.Ordinal));
        Assert.NotEmpty(WorkbenchOnly);
    }
}
