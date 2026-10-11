namespace TruthWeaver.Evaluation;

/// <summary>The law that one <see cref="RewriteStep"/> applied. Every law is an identity of Strong Kleene (K3) logic.</summary>
public enum RewriteLaw
{
    /// <summary>An alias became its definition: <c>ANY</c>, <c>ALL</c>, <c>ExactlyOne</c>, <c>GreaterThan</c>, <c>LessThan</c> or an <c>AtLeast</c> that equals <c>OR</c> or <c>AND</c>.</summary>
    AliasCollapse,

    /// <summary>A negation of a negation was removed; the double negation of <c>x</c> became <c>x</c>.</summary>
    DoubleNegation,

    /// <summary>A nested <c>AND</c>, <c>OR</c> or <c>COALESCE</c> was spliced into its parent.</summary>
    Flatten,

    /// <summary>The operands of a commutative operator were put in canonical order. The value does not change.</summary>
    Reorder,

    /// <summary>A repeated operand of <c>AND</c> or <c>OR</c> was removed (<c>a AND a</c> is <c>a</c>).</summary>
    Idempotence,

    /// <summary>An operator over constants only became a constant.</summary>
    ConstantFold,

    /// <summary>An identity constant was dropped (<c>a AND True</c> is <c>a</c>).</summary>
    Identity,

    /// <summary>A dominant constant decided the node (<c>a AND False</c> is <c>False</c>).</summary>
    Annihilator,

    /// <summary>An operand that contains a sibling was removed (<c>a AND (a OR b)</c> is <c>a</c>).</summary>
    Absorption,

    /// <summary>De Morgan's law removed nodes (<c>NOT (NOT a AND NOT b)</c> is <c>a OR b</c>).</summary>
    DeMorgan,

    /// <summary>A negation was absorbed by a derived operator (<c>NOT a IMPLIES b</c> is <c>a OR b</c>, <c>NOT IsKnown(a)</c> is <c>IsUnknown(a)</c>).</summary>
    NegationThroughDerived,

    /// <summary>A <c>COALESCE</c> lost an operand that cannot contribute, or collapsed.</summary>
    Coalesce,

    /// <summary>An inspection (<c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c>, <c>IsKnown</c>) was folded or rewritten.</summary>
    Inspection,

    /// <summary><c>If</c> had a constant condition or equal branches.</summary>
    If,

    /// <summary>A derived operator with a constant operand was expanded one level and reduced.</summary>
    DerivedWithConstant,

    /// <summary>A threshold operator had <c>True</c>/<c>False</c> operands or a bound that settles it.</summary>
    Threshold,
}
