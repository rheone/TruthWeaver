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
}
