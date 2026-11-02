namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The palette of the open rule for the panes and the Insert menu: the one place that builds a <see cref="RulePalette"/> from
/// the workbench and reads the predicate schema file (once, through <see cref="PaletteSchemaCache"/>). The search and the
/// predicate facts are the ones of the palette.
/// </summary>
internal sealed class PaletteController
{
    private static readonly ConditionalWeakTable<Workbench, PaletteController> Controllers = [];

    private readonly Workbench workbench;
    private readonly PaletteSchemaCache schemas = new();

    /// <summary>Initializes a new instance of the <see cref="PaletteController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public PaletteController(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
    }

    /// <summary>Gets the controller of a workbench; the same one for the same workbench.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static PaletteController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, w => new PaletteController(w));
    }

    /// <summary>Gets the schema file of the settings, or <see langword="null"/> when none is set or it cannot be read.</summary>
    /// <returns>The schema file.</returns>
    public PredicateSchemaFile? Schema()
    {
        return this.schemas.Get(this.workbench.Settings.SchemaFile);
    }

    /// <summary>
    /// Gets the palette: the operators, the constants, the predicates of the schema file and those of the rule, and the
    /// definitions of the text when it compiled.
    /// </summary>
    /// <returns>The palette.</returns>
    public RulePalette Palette()
    {
        return RulePalette.Create(this.workbench.Document.Rule, this.Schema(), this.Definitions());
    }

    /// <summary>
    /// Gets the definitions of the text, only when the text compiled: the palette, the tree and the edits start from that rule.
    /// </summary>
    /// <returns>The definitions, or <see langword="null"/> when the text has none or did not compile.</returns>
    public RuleDefinitions? Definitions()
    {
        RuleDocument document = this.workbench.Document;
        return document.IsRuleCurrent && document.Definitions.HasDefinitions ? document.Definitions : null;
    }
}
