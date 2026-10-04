namespace TruthWeaver.Ast;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The one table of <see cref="OperatorDefinition"/>s, one per operator in the closed set. <see cref="OperatorInfo"/>
/// and <see cref="TreeFormatOpNames"/> read their names, labels and descriptions from here.
/// </summary>
internal static class OperatorDefinitions
{
    private const string InspectionTail = "The result is never Unknown, so it never collapses the enclosing rule.";

    private static readonly OperatorDefinition[] Definitions =
    [
        new("Not", "not", 1, 1, "NOT", "Logical negation. Unknown stays Unknown."),
        new("And", "and", 2, null, "AND", "True iff every operand is true. Short-circuits at the first False."),
        new("Or", "or", 2, null, "OR", "True iff at least one operand is true. Short-circuits at the first True."),
        new(
            "Xor",
            "xor",
            2,
            2,
            "XOR",
            "True iff exactly one of the two operands is true. Unknown if either operand is Unknown."
        ),
        new(
            "Equivalent",
            "equivalent",
            2,
            2,
            "EQUIVALENT",
            "Logical biconditional (IFF, formerly XNOR) — true iff both operands agree (both true or both false). The negation of XOR; Unknown if either operand is Unknown."
        ),
        new(
            "Implies",
            "implies",
            2,
            2,
            "IMPLIES",
            "Material implication (NOT antecedent OR consequent). True if the antecedent is False or the consequent is True; otherwise Unknown if either operand is Unknown."
        ),
        new(
            "Nand",
            "nand",
            2,
            2,
            "NAND",
            "Negated conjunction, NOT (left AND right). False only when both operands are True; True if either is False; otherwise Unknown."
        ),
        new(
            "Nor",
            "nor",
            2,
            2,
            "NOR",
            "Negated disjunction, NOT (left OR right). True only when both operands are False; False if either is True; otherwise Unknown."
        ),
        new(
            "Parity",
            "parity",
            2,
            null,
            "PARITY",
            "N-ary parity. True iff an odd number of operands are true and none is Unknown; False iff an even number are true and none is Unknown; Unknown whenever any operand is Unknown."
        ),
        new(
            "Any",
            "any",
            2,
            null,
            "ANY",
            "True iff at least one operand is true (AtLeast(1, ...)). False iff every operand is false; Unknown when no operand is true yet some are Unknown."
        ),
        new(
            "All",
            "all",
            2,
            null,
            "ALL",
            "True iff every operand is true (AtLeast(n, ...)). False iff any operand is false; Unknown when no operand is false yet some are Unknown."
        ),
        new(
            "None",
            "none",
            2,
            null,
            "NONE",
            "True iff no operand is true (AtMost(0, ...)). False iff any operand is true; Unknown when no operand is true yet some are Unknown."
        ),
        new("ExactlyOne", "exactlyOne", 2, null, "ExactlyOne", "True iff exactly one operand is true."),
        new("AtLeast", "atLeast", 1, null, "AtLeast({K})", "True iff at least {K} of the operands are true."),
        new("AtMost", "atMost", 1, null, "AtMost({K})", "True iff at most {K} of the operands are true."),
        new("GreaterThan", "greaterThan", 1, null, "GreaterThan({K})", "True iff more than {K} of the operands are true."),
        new("LessThan", "lessThan", 1, null, "LessThan({K})", "True iff fewer than {K} of the operands are true."),
        new("Exactly", "exactly", 1, null, "Exactly({K})", "True iff exactly {K} of the operands are true."),
        new(
            "Between",
            "between",
            2,
            null,
            "BETWEEN({K}, {Max})",
            "True iff the number of true operands is between {K} and {Max} inclusive (AtLeast({K}, ...) AND AtMost({Max}, ...)). False iff no possible count of true operands lies in that range; Unknown otherwise."
        ),
        new(
            "Coalesce",
            "coalesce",
            2,
            null,
            "COALESCE",
            "The first operand that is not Unknown; True and False pass through unchanged and the result is Unknown only if every operand is Unknown. Operands after the first known value are skipped."
        ),
        new(
            "If",
            "if",
            3,
            3,
            "If",
            "Conditional: the second operand when the condition is True, the third when it is False. An Unknown condition does not pick a branch: the result is the branch value when both branches are the same definite value, otherwise Unknown. Only the needed branch is evaluated for a definite condition."
        ),
        new(
            "IsTrue",
            "isTrue",
            1,
            1,
            "IsTrue",
            "Inspection: True iff the operand is True; False if it is False or Unknown. " + InspectionTail
        ),
        new(
            "IsFalse",
            "isFalse",
            1,
            1,
            "IsFalse",
            "Inspection: True iff the operand is False; False if it is True or Unknown. " + InspectionTail
        ),
        new(
            "IsUnknown",
            "isUnknown",
            1,
            1,
            "IsUnknown",
            "Inspection: True iff the operand is Unknown; False if it is True or False. " + InspectionTail
        ),
        new(
            "IsKnown",
            "isKnown",
            1,
            1,
            "IsKnown",
            "Inspection: True iff the operand is True or False; False if it is Unknown. " + InspectionTail
        ),
    ];

    private static readonly Dictionary<string, OperatorDefinition> ByOpName = Definitions.ToDictionary(
        d => d.OpName,
        StringComparer.Ordinal
    );

    /// <summary>Gets every operator definition, in no significant order.</summary>
    public static IReadOnlyList<OperatorDefinition> All => Definitions;

    /// <summary>Looks up the definition for a canonical op-name.</summary>
    /// <param name="opName">The canonical op-name (a <see cref="NodeShape.OpName"/> value).</param>
    /// <param name="definition">The definition when found.</param>
    /// <returns><see langword="true"/> if <paramref name="opName"/> names an operator in the closed set.</returns>
    public static bool TryGet(string opName, [NotNullWhen(true)] out OperatorDefinition? definition)
    {
        return ByOpName.TryGetValue(opName, out definition);
    }
}
