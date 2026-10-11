namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The primitive form of <c>PARITY(ops)</c>, <c>OR(Exactly(1, ops), Exactly(3, ops), ...)</c> over every odd count up to
/// <c>n</c>: its builder and its recogniser side by side (see <see cref="XorForm"/> for why they live together).
/// </summary>
/// <remarks>
/// <c>PARITY</c> is "an odd number of operands are True", and Unknown if any operand is Unknown. With no Unknown operand
/// the count interval is a single count and the disjunction is True iff that count is odd; with at least one Unknown the
/// interval holds two or more consecutive counts, so every <c>Exactly(k)</c> that can match is Unknown (never True) and at
/// least one odd count is always inside the interval, which makes the disjunction Unknown. The form is linear in the
/// operand count, unlike a fold of the binary XOR form, which repeats its accumulator twice per step and so grows
/// exponentially. The recogniser needs at least three operands: two operands build a single <c>Exactly(1)</c>, which is
/// not an <c>OR</c>, so there is nothing to recognise.
/// </remarks>
internal static class ParityForm
{
    /// <summary>Builds the primitive form.</summary>
    /// <param name="operands">The parity operands (at least two).</param>
    /// <returns>The <c>OR</c> of the odd-count <c>Exactly</c> tests, or the single <c>Exactly(1)</c> for two operands.</returns>
    public static Expression Build(EquatableArray<Expression> operands)
    {
        List<Expression> oddCounts = [];
        for (int k = 1; k <= operands.Count; k += 2)
        {
            oddCounts.Add(new ThresholdExpression(ThresholdComparison.Exactly, k, operands));
        }

        // Two operands have a single odd count (1), and an OR needs at least two operands.
        return oddCounts.Count == 1 ? oddCounts[0] : new OrExpression(new EquatableArray<Expression>(oddCounts));
    }

    /// <summary>Recognises the primitive form of three or more operands.</summary>
    /// <param name="disjuncts">The operands of an <c>OR</c>.</param>
    /// <param name="operands">The parity operands when matched.</param>
    /// <returns><see langword="true"/> when <paramref name="disjuncts"/> is exactly the form.</returns>
    public static bool TryMatch(EquatableArray<Expression> disjuncts, out EquatableArray<Expression> operands)
    {
        operands = default;
        if (disjuncts[0] is not ThresholdExpression { Comparison: ThresholdComparison.Exactly, K: 1 } first)
        {
            return false;
        }

        int n = first.Operands.Count;
        if (n < 3 || disjuncts.Count != (n + 1) / 2)
        {
            return false;
        }

        for (int i = 0; i < disjuncts.Count; i++)
        {
            if (
                disjuncts[i] is not ThresholdExpression { Comparison: ThresholdComparison.Exactly } term
                || term.K != (2 * i) + 1
                || !term.Operands.Equals(first.Operands)
            )
            {
                return false;
            }
        }

        operands = first.Operands;
        return true;
    }
}
