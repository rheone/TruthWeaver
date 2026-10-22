namespace TruthWeaver.Tool.Tui.Controllers;

using TruthWeaver.Diagnostics;

/// <content>Renders the text of one diagnostic, and the analyzer findings of a node of the tree.</content>
internal sealed partial class InspectController
{
    private static void DiagnosticText(Lines text, Diagnostic diagnostic)
    {
        text.Add($"{diagnostic.Severity} {diagnostic.Code}");
        text.Add(diagnostic.Message);
        if (diagnostic.Expected is not null || diagnostic.Found is not null)
        {
            text.Add($"Expected: {diagnostic.Expected ?? "-"}; found: {diagnostic.Found ?? "-"}");
        }

        if (diagnostic.Suggestion is { } suggestion)
        {
            text.Add($"Suggestion: {suggestion.Text}");
        }

        text.Add("Documentation: docs/diagnostics.md");
    }

    // The analyzer findings: at the root the ones that name no node (a whole-rule finding), at a node the ones of its path.
    private void Findings(Lines text, string path, bool isRoot)
    {
        Diagnostic[] findings =
        [
            .. this.workbench.Document.Diagnostics.Where(d =>
                d.Severity != DiagnosticSeverity.Error && (d.Path == path || (isRoot && d.Path is null))
            ),
        ];
        if (findings.Length == 0)
        {
            return;
        }

        text.Add("Findings:");
        foreach (Diagnostic finding in findings.Take(5))
        {
            text.Add($"  {finding.Code}: {finding.Message}");
        }

        if (findings.Length > 5)
        {
            text.Add($"  and {findings.Length - 5} more (see the Diagnostics pane)");
        }
    }
}
