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

    /// <summary>The canonical rule text compiles to a rule with the same canonical text.</summary>
    DslRoundTrip,

    /// <summary>The JSON tree from <c>PrintJson</c> compiles to a rule with the same canonical text.</summary>
    JsonRoundTrip,
}
