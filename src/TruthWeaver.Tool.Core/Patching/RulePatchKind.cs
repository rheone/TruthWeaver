namespace TruthWeaver.Tool.Core.Patching;

/// <summary>What <see cref="RulePatcher"/> did to one rule of a document.</summary>
public enum RulePatchKind
{
    /// <summary>The edited rule compiles to the same tree and notes as the rule in the document, so no byte changed.</summary>
    Unchanged,

    /// <summary>Only the changed values were replaced. Comments and layout, inside and outside the rule, stay.</summary>
    Patched,

    /// <summary>
    /// The rule's own span was written again: the change did not fit the old structure, or the rule is rule text, which has
    /// no positions for its parts. Text outside the rule stays; comments and layout inside the rule do not.
    /// </summary>
    RuleRewritten,

    /// <summary>The whole document was serialized again in <see cref="WriteBackMode.RoundTrip"/> mode.</summary>
    DocumentRewritten,
}
