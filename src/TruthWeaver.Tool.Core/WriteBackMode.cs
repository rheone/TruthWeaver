namespace TruthWeaver.Tool.Core;

/// <summary>How <see cref="RulePatcher"/> writes an edited rule back into its document.</summary>
public enum WriteBackMode
{
    /// <summary>
    /// The default. Replace only the values that changed, inside the rule's own span, so comments, key order and layout
    /// stay byte for byte. When a change does not fit the old structure, the rule alone is written again, with a
    /// <see cref="ToolDiagnosticCodes.RuleRewritten"/> warning.
    /// </summary>
    Patch,

    /// <summary>
    /// Read the whole document, put the edited rule in it, and serialize the whole document again. YAML comments and the
    /// layout of the document can be lost, so this mode always gives a <see cref="ToolDiagnosticCodes.RoundTripWriteBack"/>
    /// warning.
    /// </summary>
    RoundTrip,
}
