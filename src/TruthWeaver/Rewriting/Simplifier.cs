namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Evaluation;

/// <summary>
/// Replaces an expression with an equivalent, never larger one using only rewrites that are identities of Strong Kleene logic
/// (ADR-0005 decision 10). Classical-only laws are deliberately absent: <c>a OR NOT a = True</c>, <c>a AND NOT a = False</c>,
/// <c>a IMPLIES a = True</c>, <c>a EQUIVALENT a = True</c>, <c>a XOR a = False</c> and complement absorption all fail when
/// <c>a</c> is <c>Unknown</c>, so none is used. Every rule here was checked against the truth-table oracle by the property tests.
/// </summary>
/// <remarks>
/// The rules, bottom-up inside a canonicalise-rewrite loop that repeats until nothing changes:
/// <list type="bullet">
/// <item><description>Constant folding falls out of the identity/annihilator rules and the operator definitions: a node whose operands are all constants folds to a constant.</description></item>
/// <item><description><c>AND</c>/<c>OR</c>: drop the identity constant, annihilate on the dominant constant, absorb (<c>a AND (a OR b) = a</c>). An <c>Unknown</c> operand is kept.</description></item>
/// <item><description><c>NOT</c>: fold constants, remove double negation, <c>NOT NAND</c> to <c>AND</c>, <c>NOT NOR</c> to <c>OR</c>, <c>NOT IsKnown</c> to <c>IsUnknown</c> (and back), flip a threshold, De Morgan only where it removes nodes.</description></item>
/// <item><description><c>COALESCE</c>/inspections: drop <c>Unknown</c> constants, stop at the first operand that can never be <c>Unknown</c>, fold inspections of such operands.</description></item>
/// <item><description><c>If</c>: a constant condition picks a branch; equal branches are that branch. Other derived operators with a constant operand are expanded one level and re-simplified, and kept only if that is no larger.</description></item>
/// <item><description>Thresholds: <c>True</c>/<c>False</c> operands are eliminated by shifting <c>k</c>; out-of-range thresholds fold to constants; one remaining operand is that operand or its negation.</description></item>
/// </list>
/// </remarks>
internal static class Simplifier
{
    /// <summary>A safety bound on the repeat-until-stable loop; each productive round shrinks the tree.</summary>
    private const int MaxPasses = 16;

    /// <summary>Simplifies <paramref name="root"/> and everything below it.</summary>
    /// <param name="root">The tree to simplify.</param>
    /// <param name="trace">Receives each change in the order it is made, or <see langword="null"/> to report nothing.</param>
    /// <returns>An equivalent tree that is never larger than <paramref name="root"/>.</returns>
    public static Expression Simplify(Expression root, RewriteTrace? trace = null)
    {
        Expression current = Canonicalizer.Canonicalize(root, trace);
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            Expression next = Canonicalizer.Canonicalize(new Pass(trace).Visit(current), trace);
            if (next.Equals(current))
            {
                break;
            }

            current = next;
        }

        // Every rewrite is size-guarded, so this only protects against a future rule that is not.
        if (ExpressionTools.Size(current) <= ExpressionTools.Size(root))
        {
            return current;
        }

        // The rule is returned unchanged, so no step describes it.
        trace?.Clear();
        return root;
    }

    /// <summary>The result of one rewrite and the law it applied.</summary>
    /// <param name="Result">The rewritten node.</param>
    /// <param name="Law">The law applied.</param>
    private readonly record struct Rewritten(Expression Result, RewriteLaw Law);

    /// <summary>One bottom-up sweep.</summary>
    private sealed class Pass(RewriteTrace? trace)
    {
        private readonly Dictionary<Expression, Expression> memo = [with(ReferenceEqualityComparer.Instance)];
        private readonly Dictionary<Expression, bool> definite = [with(ReferenceEqualityComparer.Instance)];

        public Expression Visit(Expression node)
        {
            if (this.memo.TryGetValue(node, out Expression? known))
            {
                return known;
            }

            Expression result = ExpressionTools.MapChildren(node, this.Visit);
            for (int round = 0; round < 8; round++)
            {
                if (this.Rewrite(result) is not { } rewritten)
                {
                    break;
                }

                trace?.Report(rewritten.Law, result, rewritten.Result);
                result = rewritten.Result;
            }

            // A result is a fixed point of this sweep, so revisiting it (when a rewrite re-simplifies an expansion) is free.
            this.memo[node] = result;
            this.memo[result] = result;
            return result;
        }

        private static ConstantExpression Constant(TruthValue value)
        {
            return new ConstantExpression(value);
        }

        /// <summary>Strong Kleene negation of an expression, folding constants and double negation.</summary>
        private static Expression Negate(Expression operand)
        {
            return operand switch
            {
                ConstantExpression { Value: TruthValue.True } => Constant(TruthValue.False),
                ConstantExpression { Value: TruthValue.False } => Constant(TruthValue.True),
                ConstantExpression => operand,
                NotExpression inner => inner.Operand,
                _ => new NotExpression(operand),
            };
        }

        private static Expression Inspect(InspectionKind kind, Expression operand)
        {
            return new InspectionExpression(kind, operand);
        }

        /// <summary>The value of an inspection of a known constant.</summary>
        private static TruthValue Inspected(InspectionKind kind, TruthValue value)
        {
            bool result = kind switch
            {
                InspectionKind.IsTrue => value == TruthValue.True,
                InspectionKind.IsFalse => value == TruthValue.False,
                InspectionKind.IsUnknown => value == TruthValue.Unknown,
                _ => value != TruthValue.Unknown,
            };
            return result ? TruthValue.True : TruthValue.False;
        }

        private static Expression? SmallerOrNull(Expression original, Expression candidate)
        {
            return ExpressionTools.Size(candidate) < ExpressionTools.Size(original) ? candidate : null;
        }

        private static Expression? RewriteNot(NotExpression n)
        {
            // Threshold rows: count < k is the negation of count >= k for every count in the interval, so NOT flips the
            // comparison. De Morgan holds in K3 too, but pushing NOT inward adds a NOT per operand: only take it when smaller.
            return n.Operand switch
            {
                ConstantExpression or NotExpression => Negate(n.Operand),
                NandExpression nand => new AndExpression(ExpressionTools.Array([nand.Left, nand.Right])),
                NorExpression nor => new OrExpression(ExpressionTools.Array([nor.Left, nor.Right])),
                InspectionExpression { Kind: InspectionKind.IsKnown } known => Inspect(InspectionKind.IsUnknown, known.Operand),
                InspectionExpression { Kind: InspectionKind.IsUnknown } unknown => Inspect(
                    InspectionKind.IsKnown,
                    unknown.Operand
                ),
                ThresholdExpression { Comparison: ThresholdComparison.AtLeast } atLeast => new ThresholdExpression(
                    ThresholdComparison.AtMost,
                    atLeast.K - 1,
                    atLeast.Operands
                ),
                ThresholdExpression { Comparison: ThresholdComparison.AtMost } atMost => new ThresholdExpression(
                    ThresholdComparison.AtLeast,
                    atMost.K + 1,
                    atMost.Operands
                ),
                AndExpression and => SmallerOrNull(n, new OrExpression(ExpressionTools.Array(and.Operands.Select(Negate)))),
                OrExpression or => SmallerOrNull(n, new AndExpression(ExpressionTools.Array(or.Operands.Select(Negate)))),
                _ => null,
            };
        }

        /// <summary>
        /// <c>AND</c>/<c>OR</c> over constants and absorption. The identity constant (<c>True</c> for AND, <c>False</c> for OR)
        /// drops out, the dominant one decides the whole node, and an <c>Unknown</c> constant stays (it is neither).
        /// Absorption (<c>a AND (a OR b) = a</c>) is the lattice law that does hold in K3; the complement variants do not.
        /// </summary>
        private static Rewritten? RewriteJunction(EquatableArray<Expression> operands, bool isAnd)
        {
            TruthValue identity = isAnd ? TruthValue.True : TruthValue.False;
            TruthValue dominant = isAnd ? TruthValue.False : TruthValue.True;
            bool onlyConstants = operands.All(o => o is ConstantExpression);
            List<Expression> kept = [];
            foreach (Expression operand in operands)
            {
                if (operand is ConstantExpression constant)
                {
                    if (constant.Value == dominant)
                    {
                        return new Rewritten(
                            Constant(dominant),
                            onlyConstants ? RewriteLaw.ConstantFold : RewriteLaw.Annihilator
                        );
                    }

                    if (constant.Value == identity)
                    {
                        continue;
                    }
                }

                kept.Add(operand);
            }

            // Dropping identity constants is one step; absorption is left to the next round, so each step has one law.
            RewriteLaw law = onlyConstants ? RewriteLaw.ConstantFold : RewriteLaw.Identity;
            List<Expression> survivors = kept;
            if (kept.Count == operands.Count)
            {
                // An operand of the dual operator that contains a sibling operand is redundant.
                survivors = [.. kept.Where(candidate => !IsAbsorbed(candidate, kept, isAnd))];
                law = RewriteLaw.Absorption;
            }

            if (survivors.Count == operands.Count)
            {
                return null;
            }

            Expression rewritten = survivors.Count switch
            {
                0 => Constant(identity),
                1 => survivors[0],
                _ => isAnd
                    ? new AndExpression(ExpressionTools.Array(survivors))
                    : new OrExpression(ExpressionTools.Array(survivors)),
            };
            return new Rewritten(rewritten, law);
        }

        private static bool IsAbsorbed(Expression candidate, List<Expression> siblings, bool inAnd)
        {
            EquatableArray<Expression> inner;
            if (inAnd && candidate is OrExpression or)
            {
                inner = or.Operands;
            }
            else if (!inAnd && candidate is AndExpression and)
            {
                inner = and.Operands;
            }
            else
            {
                return false;
            }

            return siblings.Any(sibling => !sibling.Equals(candidate) && inner.Contains(sibling));
        }

        /// <summary>
        /// <c>Xor(NOT l, r)</c> is <c>Equivalent(l, r)</c> and <c>Xor(NOT l, NOT r)</c> is <c>Xor(l, r)</c> (and dually for
        /// <c>Equivalent</c>): each negation strips a <c>NOT</c> and an odd number flips the operator.
        /// </summary>
        private static Expression? RewriteNegatedPair(Expression left, Expression right, bool xor)
        {
            bool leftNegated = left is NotExpression;
            bool rightNegated = right is NotExpression;
            if (!leftNegated && !rightNegated)
            {
                return null;
            }

            Expression newLeft = left is NotExpression l ? l.Operand : left;
            Expression newRight = right is NotExpression r ? r.Operand : right;
            bool useXor = xor ^ (leftNegated ^ rightNegated);
            return useXor ? new XorExpression(newLeft, newRight) : new EquivalentExpression(newLeft, newRight);
        }

        /// <summary>
        /// Threshold operators with constant operands. A <c>True</c> operand adds one to every possible count, so it
        /// shifts <c>k</c> down by one for all three comparisons; a <c>False</c> operand adds nothing and just drops out.
        /// What is left is a smaller, sometimes structurally constant, threshold.
        /// </summary>
        private static Expression? RewriteThreshold(ThresholdExpression t)
        {
            if (t.Comparison is ThresholdComparison.GreaterThan or ThresholdComparison.LessThan)
            {
                // Canonicalisation rewrites these to AtLeast / AtMost before simplification starts.
                return null;
            }

            int shifted = t.K;
            List<Expression> rest = [];
            foreach (Expression operand in t.Operands)
            {
                switch (operand)
                {
                    case ConstantExpression { Value: TruthValue.True }:
                        shifted--;
                        break;
                    case ConstantExpression { Value: TruthValue.False }:
                        break;
                    default:
                        rest.Add(operand);
                        break;
                }
            }

            int m = rest.Count;
            bool onlyUnknown = m > 0 && rest.All(o => o is ConstantExpression { Value: TruthValue.Unknown });
            if (m == t.Operands.Count && m >= 2 && !onlyUnknown)
            {
                return null;
            }

            // The count of True operands lies in [0, m]; decide whether the comparison is settled for every count.
            switch (t.Comparison)
            {
                case ThresholdComparison.AtLeast when shifted <= 0:
                    return Constant(TruthValue.True);
                case ThresholdComparison.AtLeast when shifted > m:
                case ThresholdComparison.AtMost when shifted < 0:
                case ThresholdComparison.Exactly when shifted < 0 || shifted > m:
                    return Constant(TruthValue.False);
                case ThresholdComparison.AtMost when shifted >= m:
                    return Constant(TruthValue.True);
                case ThresholdComparison.Exactly when m == 0:
                    return Constant(TruthValue.True);
                default:
                    break;
            }

            if (onlyUnknown)
            {
                // Every count in [0, m] is possible and none is certain, so a valid threshold is Unknown.
                return Constant(TruthValue.Unknown);
            }

            if (m == 1)
            {
                // One operand: the count is 0 or 1, so the threshold is the operand (shifted == 1) or its negation (== 0).
                return shifted == 1 ? rest[0] : Negate(rest[0]);
            }

            return new ThresholdExpression(t.Comparison, shifted, ExpressionTools.Array(rest));
        }

        private static Rewritten? Tag(Expression? result, RewriteLaw law)
        {
            return result is null ? null : new Rewritten(result, law);
        }

        /// <summary>The law of a <c>NOT</c> rewrite, read from the operand it negates.</summary>
        private static RewriteLaw NotLaw(NotExpression n)
        {
            return n.Operand switch
            {
                ConstantExpression => RewriteLaw.ConstantFold,
                NotExpression => RewriteLaw.DoubleNegation,
                AndExpression or OrExpression => RewriteLaw.DeMorgan,
                _ => RewriteLaw.NegationThroughDerived,
            };
        }

        private Rewritten? Rewrite(Expression node)
        {
            return node switch
            {
                NotExpression n => Tag(RewriteNot(n), NotLaw(n)),
                AndExpression a => RewriteJunction(a.Operands, isAnd: true),
                OrExpression o => RewriteJunction(o.Operands, isAnd: false),
                CoalesceExpression c => Tag(this.RewriteCoalesce(c), RewriteLaw.Coalesce),
                InspectionExpression s => Tag(this.RewriteInspection(s), RewriteLaw.Inspection),
                IfExpression f => this.RewriteIf(f),
                ThresholdExpression t => Tag(RewriteThreshold(t), RewriteLaw.Threshold),
                ImpliesExpression { Antecedent: NotExpression negated } i => Tag(
                    new OrExpression(ExpressionTools.Array([negated.Operand, i.Consequent])),
                    RewriteLaw.NegationThroughDerived
                ),
                NandExpression { Left: NotExpression l, Right: NotExpression r } => Tag(
                    new OrExpression(ExpressionTools.Array([l.Operand, r.Operand])),
                    RewriteLaw.DeMorgan
                ),
                NorExpression { Left: NotExpression l, Right: NotExpression r } => Tag(
                    new AndExpression(ExpressionTools.Array([l.Operand, r.Operand])),
                    RewriteLaw.DeMorgan
                ),
                XorExpression x => Tag(RewriteNegatedPair(x.Left, x.Right, xor: true), RewriteLaw.NegationThroughDerived)
                    ?? this.ViaExpansion(node),
                EquivalentExpression e => Tag(
                    RewriteNegatedPair(e.Left, e.Right, xor: false),
                    RewriteLaw.NegationThroughDerived
                ) ?? this.ViaExpansion(node),
                ImpliesExpression or NandExpression or NorExpression or ParityExpression => this.ViaExpansion(node),
                AnyExpression or AllExpression or NoneExpression or ExactlyOneExpression or BetweenExpression =>
                    this.ViaExpansion(node),
                _ => null,
            };
        }

        private Expression? RewriteCoalesce(CoalesceExpression c)
        {
            List<Expression> kept = [];
            foreach (Expression operand in c.Operands)
            {
                // Unknown ?? x is x, and x ?? Unknown is x, so an Unknown constant never contributes.
                if (operand is ConstantExpression { Value: TruthValue.Unknown })
                {
                    continue;
                }

                kept.Add(operand);

                // Nothing after an operand that is never Unknown can be reached.
                if (this.IsDefinite(operand))
                {
                    break;
                }
            }

            if (kept.Count == c.Operands.Count)
            {
                return null;
            }

            return kept.Count switch
            {
                0 => Constant(TruthValue.Unknown),
                1 => kept[0],
                _ => new CoalesceExpression(ExpressionTools.Array(kept)),
            };
        }

        private Expression? RewriteInspection(InspectionExpression s)
        {
            if (s.Operand is ConstantExpression constant)
            {
                return Constant(Inspected(s.Kind, constant.Value));
            }

            if (this.IsDefinite(s.Operand))
            {
                return s.Kind switch
                {
                    InspectionKind.IsKnown => Constant(TruthValue.True),
                    InspectionKind.IsUnknown => Constant(TruthValue.False),
                    InspectionKind.IsTrue => s.Operand,
                    _ => Negate(s.Operand),
                };
            }

            // NOT swaps True and False and fixes Unknown, so inspecting a negation inspects the opposite state.
            if (s.Operand is NotExpression negated)
            {
                return s.Kind switch
                {
                    InspectionKind.IsTrue => Inspect(InspectionKind.IsFalse, negated.Operand),
                    InspectionKind.IsFalse => Inspect(InspectionKind.IsTrue, negated.Operand),
                    _ => Inspect(s.Kind, negated.Operand),
                };
            }

            return null;
        }

        private Rewritten? RewriteIf(IfExpression f)
        {
            return f.Condition switch
            {
                ConstantExpression { Value: TruthValue.True } => new Rewritten(f.WhenTrue, RewriteLaw.If),
                ConstantExpression { Value: TruthValue.False } => new Rewritten(f.WhenFalse, RewriteLaw.If),

                // For an Unknown condition the definition adds the (t AND f) consensus term, which makes equal branches
                // exactly that branch, so the condition is irrelevant.
                _ when f.WhenTrue.Equals(f.WhenFalse) => new Rewritten(f.WhenTrue, RewriteLaw.If),
                _ => this.ViaExpansion(f),
            };
        }

        /// <summary>
        /// For a derived operator with a constant operand: expands that one operator to the primitive kernel (its verified
        /// definition), simplifies the result, and keeps it only if it is no larger than the operator it replaces.
        /// </summary>
        private Rewritten? ViaExpansion(Expression node)
        {
            if (!ExpressionTools.Children(node).Exists(child => child is ConstantExpression))
            {
                return null;
            }

            // The inner steps are not reported: the candidate may be discarded, and the accepted one is a single step.
            trace?.Pause();
            Expression candidate = this.Visit(PrimitiveExpander.ExpandTop(node));
            trace?.Resume();
            return ExpressionTools.Size(candidate) <= ExpressionTools.Size(node)
                ? new Rewritten(candidate, RewriteLaw.DerivedWithConstant)
                : null;
        }

        /// <summary>
        /// Whether the expression can never be <c>Unknown</c>, judged structurally and conservatively: a definite constant,
        /// or an inspection; a <c>COALESCE</c> with any such operand; or any other operator all of whose
        /// operands are. Terms are never definite, since a predicate may answer or fault to <c>Unknown</c>.
        /// </summary>
        private bool IsDefinite(Expression node)
        {
            if (this.definite.TryGetValue(node, out bool known))
            {
                return known;
            }

            bool result = node switch
            {
                ConstantExpression c => c.Value != TruthValue.Unknown,
                TermExpression => false,
                InspectionExpression => true,
                CoalesceExpression c => c.Operands.Any(this.IsDefinite),
                _ => ExpressionTools.Children(node).All(this.IsDefinite),
            };
            this.definite[node] = result;
            return result;
        }
    }
}
