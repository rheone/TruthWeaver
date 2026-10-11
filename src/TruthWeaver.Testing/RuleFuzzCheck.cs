namespace TruthWeaver.Testing;

/// <summary>The checks that <see cref="RuleFuzzer"/> runs on each generated rule that compiles.</summary>
public enum RuleFuzzCheck
{
    /// <summary>The evaluator gives the brute-force Strong Kleene value of the rule for every assignment of its terms.</summary>
    Evaluation,

    /// <summary>The rule from <c>Simplify</c> gives the Strong Kleene value of the original rule for every assignment.</summary>
    Simplify,

    /// <summary>The rule from <c>Canonicalize</c> gives the Strong Kleene value of the original rule for every assignment.</summary>
    Canonicalize,

    /// <summary>The rule from <c>Simplify</c> has no more nodes than the original.</summary>
    SimplifyNeverLarger,

    /// <summary>Simplifying the rule from <c>Simplify</c> again gives the same canonical text.</summary>
    SimplifyIdempotent,

    /// <summary>The rule from <c>Canonicalize</c> has no more nodes than the original.</summary>
    CanonicalizeNeverLarger,

    /// <summary>Canonicalizing the rule from <c>Canonicalize</c> again gives the same canonical text.</summary>
    CanonicalizeIdempotent,

    /// <summary>The canonical rule text compiles to a rule with the same canonical text.</summary>
    DslRoundTrip,

    /// <summary>The JSON tree from <c>PrintJson</c> compiles to a rule with the same canonical text.</summary>
    JsonRoundTrip,

    /// <summary>The rule from <c>ToNnf</c> gives the Strong Kleene value of the original rule for every assignment.</summary>
    ToNnf,

    /// <summary>The rule from <c>ToCnf</c> gives the Strong Kleene value of the original rule for every assignment.</summary>
    ToCnf,

    /// <summary>The rule from <c>ToDnf</c> gives the Strong Kleene value of the original rule for every assignment.</summary>
    ToDnf,

    /// <summary>Rewriting the rule from <c>ToNnf</c> with <c>ToNnf</c> again gives the same canonical text.</summary>
    NnfIdempotent,

    /// <summary>Rewriting the rule from <c>ToCnf</c> with <c>ToCnf</c> again gives the same canonical text.</summary>
    CnfIdempotent,

    /// <summary>Rewriting the rule from <c>ToDnf</c> with <c>ToDnf</c> again gives the same canonical text.</summary>
    DnfIdempotent,
}
