namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using System.Text;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Tool.Tui.Workbench;

/// <content>
/// The text of the inspect key and of the mouse hover: what a part of the rule means and what it does now. Both routes call
/// <see cref="DescribeNode"/> (a node of the tree) or <see cref="DescribeOffset"/> (a place in the editor text), so the
/// keyboard and the hover never differ. The descriptions come from the library: the outline of the compiled rule (the
/// operator table), the predicate catalog and the palette, not from copies.
/// </content>
/// <remarks>
/// The controller is split by reason to change: this file holds the scaffolding every part shares; <c>.Nodes.cs</c> builds
/// the text of a node of the tree (and the operator and predicate facts both routes read); <c>.Tokens.cs</c> finds and
/// describes the token or symbol under an offset of the editor text; <c>.Diagnostics.cs</c> renders a diagnostic and the
/// analyzer findings.
/// </remarks>
internal sealed partial class InspectController
{
    private static readonly ConditionalWeakTable<Workbench, InspectController> Controllers = [];

    // The position of every node in the flat list, by node, per tree: built once, so finding a node is a lookup and not a
    // search of the whole list for each call.
    private static readonly ConditionalWeakTable<RuleTree, Dictionary<RuleTreeNode, int>> NodeIndexes = [];

    private readonly Workbench workbench;

    /// <summary>Initializes a new instance of the <see cref="InspectController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public InspectController(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
    }

    /// <summary>Gets the controller of a workbench. The window, the keys and the hover use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static InspectController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new InspectController(key));
    }

    // The lines of one description. The text is the lines joined with line feeds.
    private sealed class Lines : List<string>
    {
        public override string ToString()
        {
            StringBuilder builder = new();
            foreach (string line in this)
            {
                _ = builder.AppendLine(line);
            }

            return builder.ToString().TrimEnd().ReplaceLineEndings("\n");
        }
    }
}
