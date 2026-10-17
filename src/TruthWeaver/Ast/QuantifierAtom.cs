namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;
using TruthWeaver.Printing;

/// <summary>
/// Keys a <see cref="QuantifierExpression"/> as the single opaque atom every consumer that lists "the terms of a
/// rule" treats it as (ADR-0010 decision 8): the analyzer's own BDD variable pair, and <c>RuleCoverage</c>'s
/// distinct-term list. The key is the node's own canonical text, which includes its body, so two syntactically
/// equal quantifiers in one rule share one atom and two quantifiers with differently written (even if
/// semantically equivalent) bodies do not. Sound, intentionally incomplete (ADR-0010 decision 8).
/// </summary>
internal static class QuantifierAtom
{
    /// <summary>Gets the term identity a quantifier is keyed by wherever it is treated as an atom.</summary>
    /// <param name="node">The quantifier.</param>
    /// <returns>A synthetic, zero-argument identity whose predicate name is the quantifier's canonical text.</returns>
    public static TermIdentity IdentityOf(QuantifierExpression node)
    {
        return new TermIdentity(CanonicalPrinter.Print(node), []);
    }
}
