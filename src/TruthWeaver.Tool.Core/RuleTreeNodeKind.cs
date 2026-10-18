namespace TruthWeaver.Tool.Core;

/// <summary>The kind of a node of a <see cref="RuleTree"/>.</summary>
public enum RuleTreeNodeKind
{
    /// <summary>A constant: <c>True</c>, <c>False</c> or <c>Unknown</c>.</summary>
    Constant = 0,

    /// <summary>A term: a predicate with its arguments.</summary>
    Term = 1,

    /// <summary>An operator with its operands, such as <c>AND</c> or <c>AtLeast</c>.</summary>
    Operator = 2,

    /// <summary>
    /// A <c>let</c> definition, listed after the rule: its name and parameters, with its body as its one operand. It is not a
    /// node of the compiled rule, so it has no decision.
    /// </summary>
    Definition = 3,

    /// <summary>A use of a definition inside a definition body, with its arguments as its operands.</summary>
    Use = 4,

    /// <summary>A parameter of the definition, inside its body: each use puts its argument in this place.</summary>
    Parameter = 5,
}
