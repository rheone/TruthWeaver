namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;

/// <summary>
/// Spells a <see cref="CallExpression"/> as rule text and keys it as the single opaque atom that the analyzer, the rewrites
/// and the coverage report treat it as (ADR-0019). The key is the node's own canonical text, which holds the library rule
/// name and the hash pin and never the body of the callee, so two calls with the same name and pin share one atom and a call
/// is never analyzed through the rule it names.
/// </summary>
internal static class CallAtom
{
    /// <summary>Gets the rule text of a call: <c>call("name")</c>, or <c>call("name", hash: "...")</c> with a pin.</summary>
    /// <param name="node">The call.</param>
    /// <returns>The canonical text.</returns>
    public static string TextOf(CallExpression node)
    {
        string name = Escape(node.Name);
        return node.PinnedHash is { } pin ? $"call(\"{name}\", hash: \"{Escape(pin)}\")" : $"call(\"{name}\")";
    }

    /// <summary>Gets the term identity a call is keyed by wherever it is treated as an atom.</summary>
    /// <param name="node">The call.</param>
    /// <returns>A synthetic, zero-argument identity whose predicate name is the canonical text of the call.</returns>
    public static TermIdentity IdentityOf(CallExpression node)
    {
        return new TermIdentity(TextOf(node), []);
    }

    // Escapes \ and " so the text is valid inside a DSL quoted string, as the DSL lexer reads it.
    private static string Escape(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
