namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;

/// <summary>
/// The Strong Kleene (K3) scalar truth functions over <see cref="TruthValue"/>. This is the one place the connective
/// truth tables live: <c>Evaluator</c> applies them to evaluated operands, and the rewriters (<c>Simplifier</c>,
/// <c>NormalForms</c>) apply them to constant operands, so the two cannot drift apart.
/// </summary>
/// <remarks>
/// <c>TruthWeaver.Testing.K3Oracle</c> deliberately keeps its own independent definitions so that it can check this type.
/// </remarks>
internal static class K3Logic
{
    /// <summary>Strong Kleene negation: swaps <c>True</c> and <c>False</c> and leaves <c>Unknown</c> alone.</summary>
    internal static TruthValue Not(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    /// <summary>Strong Kleene conjunction: <c>False</c> dominates, then <c>Unknown</c>, else <c>True</c>.</summary>
    internal static TruthValue And(TruthValue left, TruthValue right)
    {
        return (left, right) switch
        {
            (TruthValue.False, _) => TruthValue.False,
            (_, TruthValue.False) => TruthValue.False,
            (TruthValue.True, TruthValue.True) => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    /// <summary>Strong Kleene disjunction: <c>True</c> dominates, then <c>Unknown</c>, else <c>False</c>.</summary>
    internal static TruthValue Or(TruthValue left, TruthValue right)
    {
        return (left, right) switch
        {
            (TruthValue.True, _) => TruthValue.True,
            (_, TruthValue.True) => TruthValue.True,
            (TruthValue.False, TruthValue.False) => TruthValue.False,
            _ => TruthValue.Unknown,
        };
    }

    /// <summary>Strong Kleene exclusive or: <c>Unknown</c> when either side is, else true when exactly one side is.</summary>
    internal static TruthValue Xor(TruthValue left, TruthValue right)
    {
        if (left == TruthValue.Unknown || right == TruthValue.Unknown)
        {
            return TruthValue.Unknown;
        }

        return (left == TruthValue.True) ^ (right == TruthValue.True) ? TruthValue.True : TruthValue.False;
    }

    /// <summary>Strong Kleene biconditional: the negation of <see cref="Xor"/>.</summary>
    internal static TruthValue Equivalent(TruthValue left, TruthValue right)
    {
        return Not(Xor(left, right));
    }

    /// <summary>Strong Kleene negated conjunction: <c>NOT (left AND right)</c>.</summary>
    internal static TruthValue Nand(TruthValue left, TruthValue right)
    {
        return Not(And(left, right));
    }

    /// <summary>Strong Kleene negated disjunction: <c>NOT (left OR right)</c>.</summary>
    internal static TruthValue Nor(TruthValue left, TruthValue right)
    {
        return Not(Or(left, right));
    }

    /// <summary>Strong Kleene material implication: <c>NOT antecedent OR consequent</c>.</summary>
    internal static TruthValue Implies(TruthValue antecedent, TruthValue consequent)
    {
        return Or(Not(antecedent), consequent);
    }

    /// <summary>
    /// Strong Kleene n-ary parity: <c>Unknown</c> if any operand is <c>Unknown</c> (the fold of binary XOR, which is
    /// <c>Unknown</c> whenever either side is), otherwise <c>True</c> for an odd number of <c>True</c> operands.
    /// </summary>
    internal static TruthValue Parity(IReadOnlyList<TruthValue> operands)
    {
        if (operands.Any(v => v == TruthValue.Unknown))
        {
            return TruthValue.Unknown;
        }

        return operands.Count(v => v == TruthValue.True) % 2 == 1 ? TruthValue.True : TruthValue.False;
    }

    /// <summary>
    /// Tests the K3 state of <paramref name="value"/>. These are the external (non-monotone) operators, so the answer is
    /// always a definite <c>True</c> or <c>False</c>, never <c>Unknown</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException"><paramref name="kind"/> is not a defined inspection.</exception>
    internal static TruthValue Inspect(InspectionKind kind, TruthValue value)
    {
        bool matches = kind switch
        {
            InspectionKind.IsTrue => value == TruthValue.True,
            InspectionKind.IsFalse => value == TruthValue.False,
            InspectionKind.IsUnknown => value == TruthValue.Unknown,
            InspectionKind.IsKnown => value != TruthValue.Unknown,
            _ => throw new InvalidOperationException($"Unhandled inspection kind '{kind}'."),
        };
        return matches ? TruthValue.True : TruthValue.False;
    }
}
