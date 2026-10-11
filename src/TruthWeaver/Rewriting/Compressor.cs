namespace TruthWeaver.Rewriting;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Rewrites primitive shapes back into readable derived operators (ADR-0005 decision 10): the inverse direction of
/// <see cref="PrimitiveExpander"/>. Every pattern below is an identity of Strong Kleene logic (verified against the
/// truth-table oracle by the property tests), never a classical-only law, and every replacement has no more nodes than
/// the shape it replaces, so the result is never larger than the input.
/// </summary>
/// <remarks>
/// Matching is top-down so a composite pattern (for example the <c>XOR</c> shape) is recognised before its parts are
/// rewritten into something else; a pass is repeated until nothing changes so that a shape exposed by a rewrite below it
/// (a range that appears only after a pair is merged) is recognised too, which also makes the result idempotent.
/// </remarks>
internal static class Compressor
{
    /// <summary>
    /// A safety bound on the repeat-until-stable loop. Every rewrite shrinks the tree or moves a node to a derived form no
    /// pattern consumes, so passes converge in a handful of rounds; the bound only guards against a future pattern cycling.
    /// </summary>
    private const int MaxPasses = 16;

    /// <summary>Compresses <paramref name="root"/> and everything below it.</summary>
    /// <param name="root">The tree to compress.</param>
    /// <returns>An equivalent tree, no larger than <paramref name="root"/>, using derived operators where patterns matched.</returns>
    public static Expression Compress(Expression root)
    {
        Expression current = root;
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            Expression next = new Pass().Visit(current);
            if (next.Equals(current))
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    /// <summary>One top-down sweep. The memo keeps shared sub-trees shared and stops a repeated operand being rewritten twice.</summary>
    private sealed class Pass
    {
        private readonly Dictionary<Expression, Expression> memo = [with(ReferenceEqualityComparer.Instance)];

        public Expression Visit(Expression node)
        {
            if (this.memo.TryGetValue(node, out Expression? known))
            {
                return known;
            }

            Expression result = this.Match(node) ?? ExpressionTools.MapChildren(node, this.Visit);
            this.memo[node] = result;
            return result;
        }

        private Expression? Match(Expression node)
        {
            return node switch
            {
                OrExpression o => this.MatchOr(o),
                AndExpression a => this.MatchAnd(a),
                NotExpression n => this.MatchNot(n),
                ThresholdExpression t => this.MatchThreshold(t),
                CoalesceExpression c => this.MatchCoalesce(c),
                _ => null,
            };
        }

        private Expression? MatchOr(OrExpression or)
        {
            EquatableArray<Expression> ops = or.Operands;
            if (ops.Count == 2)
            {
                // XOR and EQUIVALENT expand to a two-term OR of two-term ANDs (see PrimitiveExpander); OR is commutative.
                if (XorForm.TryMatch(ops, out Expression? xl, out Expression? xr))
                {
                    return new XorExpression(this.Visit(xl), this.Visit(xr));
                }

                if (EquivalentForm.TryMatch(ops, out Expression? el, out Expression? er))
                {
                    return new EquivalentExpression(this.Visit(el), this.Visit(er));
                }

                // IsKnown(x) = COALESCE(x, False) OR COALESCE(NOT x, False).
                if (InspectionForm.TryMatchPair(ops, InspectionKind.IsKnown, out Expression? known))
                {
                    return new InspectionExpression(InspectionKind.IsKnown, this.Visit(known));
                }

                // NOT a OR b is a implication; with both sides negated it is a NAND (De Morgan holds in K3).
                if (ops[0] is NotExpression na && ops[1] is NotExpression nb)
                {
                    return new NandExpression(this.Visit(na.Operand), this.Visit(nb.Operand));
                }

                if (ops[0] is NotExpression antecedent)
                {
                    return new ImpliesExpression(this.Visit(antecedent.Operand), this.Visit(ops[1]));
                }

                if (ops[1] is NotExpression swapped)
                {
                    return new ImpliesExpression(this.Visit(swapped.Operand), this.Visit(ops[0]));
                }
            }

            if (IfForm.TryMatch(ops, out Expression? c, out Expression? t, out Expression? f))
            {
                return new IfExpression(this.Visit(c), this.Visit(t), this.Visit(f));
            }

            return ParityForm.TryMatch(ops, out EquatableArray<Expression> parityOperands)
                ? new ParityExpression(ExpressionTools.Array(parityOperands.Select(this.Visit)))
                : null;
        }

        private Expression? MatchAnd(AndExpression and)
        {
            EquatableArray<Expression> ops = and.Operands;
            if (ops.Count == 2)
            {
                if (ops[0] is NotExpression na && ops[1] is NotExpression nb)
                {
                    return new NorExpression(this.Visit(na.Operand), this.Visit(nb.Operand));
                }

                // IsUnknown(x) = COALESCE(x, True) AND COALESCE(NOT x, True).
                if (InspectionForm.TryMatchPair(ops, InspectionKind.IsUnknown, out Expression? unknown))
                {
                    return new InspectionExpression(InspectionKind.IsUnknown, this.Visit(unknown));
                }
            }

            return this.MergeRange(ops);
        }

        /// <summary>
        /// <c>AND(AtLeast(m, ops), AtMost(M, ops))</c> over the same operands is <c>BETWEEN(m, M, ops)</c> by definition
        /// (ADR-0005 decision 6). <c>m &lt;= M</c> keeps the bounds a valid <c>BETWEEN</c>: the threshold ranges the compiler
        /// enforces already guarantee <c>1 &lt;= m</c> and <c>M &lt;= n - 1</c>, so the full range can never appear.
        /// </summary>
        private Expression? MergeRange(EquatableArray<Expression> ops)
        {
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i] is not ThresholdExpression { Comparison: ThresholdComparison.AtLeast } lower)
                {
                    continue;
                }

                for (int j = 0; j < ops.Count; j++)
                {
                    if (
                        ops[j] is ThresholdExpression { Comparison: ThresholdComparison.AtMost } upper
                        && upper.Operands.Equals(lower.Operands)
                        && lower.K <= upper.K
                    )
                    {
                        Expression between = new BetweenExpression(
                            lower.K,
                            upper.K,
                            ExpressionTools.Array(lower.Operands.Select(this.Visit))
                        );
                        if (ops.Count == 2)
                        {
                            return between;
                        }

                        List<Expression> rest = [];
                        for (int k = 0; k < ops.Count; k++)
                        {
                            if (k == i)
                            {
                                rest.Add(between);
                            }
                            else if (k != j)
                            {
                                rest.Add(this.Visit(ops[k]));
                            }
                        }

                        return new AndExpression(ExpressionTools.Array(rest));
                    }
                }
            }

            return null;
        }

        private Expression? MatchNot(NotExpression not)
        {
            // count < k+1 is the negation of count >= k+1 for every count in the interval, so NOT flips a threshold comparison.
            return not.Operand switch
            {
                AndExpression { Operands.Count: 2 } and => new NandExpression(
                    this.Visit(and.Operands[0]),
                    this.Visit(and.Operands[1])
                ),
                OrExpression { Operands.Count: 2 } or => new NorExpression(
                    this.Visit(or.Operands[0]),
                    this.Visit(or.Operands[1])
                ),
                ThresholdExpression threshold
                    when ThresholdSemantics.Negate(threshold.Comparison, threshold.K) is { } flipped => new ThresholdExpression(
                    flipped.Comparison,
                    flipped.K,
                    ExpressionTools.Array(threshold.Operands.Select(this.Visit))
                ),
                _ => null,
            };
        }

        private Expression? MatchThreshold(ThresholdExpression t)
        {
            int n = t.Operands.Count;
            if (n < 2)
            {
                // ANY/ALL/NONE/ExactlyOne need two or more operands.
                return null;
            }

            EquatableArray<Expression> ops = ExpressionTools.Array(t.Operands.Select(this.Visit));
            return (t.Comparison, t.K) switch
            {
                (ThresholdComparison.AtLeast, 1) => new AnyExpression(ops),
                (ThresholdComparison.AtLeast, _) when t.K == n => new AllExpression(ops),
                (ThresholdComparison.AtMost, 0) => new NoneExpression(ops),
                (ThresholdComparison.Exactly, 1) => new ExactlyOneExpression(ops),
                _ => null,
            };
        }

        private Expression? MatchCoalesce(CoalesceExpression c)
        {
            if (c.Operands.Count != 2 || c.Operands[1] is not ConstantExpression { Value: not TruthValue.Unknown } fallback)
            {
                return null;
            }

            // COALESCE(NOT x, False) is exactly IsFalse(x); any other COALESCE with a constant is already the shortest form.
            if (fallback.Value == TruthValue.False && c.Operands[0] is NotExpression negated)
            {
                return new InspectionExpression(InspectionKind.IsFalse, this.Visit(negated.Operand));
            }

            return null;
        }
    }
}
