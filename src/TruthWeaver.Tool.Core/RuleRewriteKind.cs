namespace TruthWeaver.Tool.Core;

/// <summary>A rewrite that <see cref="RewritePlayground.Preview"/> applies to a rule. Each one keeps the value of the rule for every assignment.</summary>
public enum RuleRewriteKind
{
    /// <summary><c>Simplify</c>, with the list of its steps (<c>SimplifyWithSteps</c>).</summary>
    Simplify = 0,

    /// <summary><c>Canonicalize</c>: aliases, double negation, flattening, ordering and repeated operands.</summary>
    Canonicalize = 1,

    /// <summary><c>ToNnf</c>: negation normal form.</summary>
    NegationNormalForm = 2,

    /// <summary><c>ToCnf</c>: conjunctive normal form.</summary>
    ConjunctiveNormalForm = 3,

    /// <summary><c>ToDnf</c>: disjunctive normal form.</summary>
    DisjunctiveNormalForm = 4,

    /// <summary><c>CompressToDerived</c>: the derived operators in place of their expansions.</summary>
    Compress = 5,

    /// <summary><c>ExpandToPrimitives</c>: <c>AND</c>, <c>OR</c> and <c>NOT</c> in place of the derived operators.</summary>
    ExpandToPrimitives = 6,

    /// <summary><c>OptimizeOrder</c>: the operands of <c>AND</c> and <c>OR</c> cheapest first.</summary>
    OptimizeOrder = 7,
}
