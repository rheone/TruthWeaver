namespace TruthWeaver.Evaluation;

/// <summary>The role of an <see cref="OutlineNode"/> in a rule's outline.</summary>
public enum OutlineNodeKind
{
    /// <summary>A predicate term. This is the default for a hand-built leaf node.</summary>
    Term,

    /// <summary>A logical or cardinality operator, with operands.</summary>
    Operator,

    /// <summary>A fixed <c>True</c>, <c>False</c> or <c>Unknown</c> value.</summary>
    Constant,
}
