namespace TruthWeaver.Analysis;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Rewriting;

/// <summary>
/// The opt-in lint step that runs after <see cref="Analyzer"/> (ticket k3-hardening 09). It walks the tree and reports
/// constructs that are provably redundant under Strong Kleene (K3) semantics as <see cref="DiagnosticSeverity.Info"/>
/// diagnostics, each with a <see cref="DiagnosticSuggestionKind.Replacement"/> holding the simpler rule text and a message
/// that states why the replacement means the same. A lint only fires when the replacement is K3-equivalent, never on a
/// two-valued intuition. Findings carry no source span: expression nodes do not remember where they were written.
/// A finding whose construct sits inside another finding's construct links to the nearest enclosing finding through
/// <see cref="Diagnostic.EnclosedBy"/>; no finding is dropped.
/// </summary>
internal static class Linter
{
    /// <summary>Lints a compiled tree.</summary>
    /// <param name="root">The compiled expression tree.</param>
    /// <param name="options">The options; <see cref="CompilerOptions.Lints"/> selects the rules and <see cref="CompilerOptions.MaxAnalysisTerms"/> caps the BDD work.</param>
    /// <returns>The findings, outermost construct first.</returns>
    public static IReadOnlyList<Diagnostic> Lint(Expression root, CompilerOptions options)
    {
        List<Diagnostic> diagnostics = [];

        // Whole-rule findings come first and stand alone: they describe the rule, not a construct inside it.
        if (options.Lints.HasFlag(LintRules.DeepNesting))
        {
            LintDepth(root, options, diagnostics);
        }

        if (options.Lints.HasFlag(LintRules.NotCanonical))
        {
            LintCanonical(root, options, diagnostics);
        }

        Visit(root, options, diagnostics, null);
        return diagnostics;
    }

    /// <summary>
    /// Runs every node-level lint on <paramref name="node"/>, links the findings to <paramref name="enclosing"/>, then
    /// visits the children with the first finding of this node (if any) as their enclosing finding.
    /// </summary>
    private static void Visit(Expression node, CompilerOptions options, List<Diagnostic> diagnostics, Diagnostic? enclosing)
    {
        int first = diagnostics.Count;
        if (options.Lints.HasFlag(LintRules.RedundantInspection) && node is InspectionExpression inspection)
        {
            LintInspection(inspection, options, diagnostics);
        }

        if (options.Lints.HasFlag(LintRules.RedundantCoalesce) && node is CoalesceExpression coalesce)
        {
            LintCoalesce(coalesce, options, diagnostics);
        }

        if (options.Lints.HasFlag(LintRules.ConstantIfCondition) && node is IfExpression conditional)
        {
            LintIfCondition(conditional, options, diagnostics);
        }

        if (
            options.Lints.HasFlag(LintRules.IdenticalIfBranches)
            && node is IfExpression { WhenTrue: var whenTrue, WhenFalse: var whenFalse } sameBranches
            && whenTrue == whenFalse
        )
        {
            diagnostics.Add(
                Finding(
                    DiagnosticCodes.IdenticalIfBranches,
                    "both branches are the same expression, so the condition does not matter",
                    "with equal branches the consensus term makes If(c, t, t) equal t even when c is Unknown",
                    sameBranches,
                    whenTrue
                )
            );
        }

        if (options.Lints.HasFlag(LintRules.VacuousCardinality) && node is ThresholdExpression or BetweenExpression)
        {
            LintCardinality(node, options, diagnostics);
        }

        if (options.Lints.HasFlag(LintRules.DuplicateOperands))
        {
            LintDuplicates(node, diagnostics);
        }

        if (
            options.Lints.HasFlag(LintRules.DoubleNegation)
            && node is NotExpression { Operand: NotExpression { Operand: var inner } } doubleNegation
        )
        {
            diagnostics.Add(
                Finding(
                    DiagnosticCodes.DoubleNegation,
                    "a negation of a negation cancels out",
                    "Strong K3 negation swaps True and False and leaves Unknown alone, so applying it twice changes nothing",
                    doubleNegation,
                    inner
                )
            );
        }

        if (options.Lints.HasFlag(LintRules.WideChain) && node is AndExpression or OrExpression)
        {
            LintWideChain(node, options, diagnostics);
        }

        // Findings of one node describe the same construct, so they are not enclosed by each other; they are enclosed by
        // the nearest ancestor that has a finding.
        for (int i = first; i < diagnostics.Count; i++)
        {
            diagnostics[i] = diagnostics[i] with { EnclosedBy = enclosing };
        }

        Diagnostic? childEnclosing = diagnostics.Count > first ? diagnostics[first] : enclosing;
        foreach (Expression child in ExpressionTools.Children(node))
        {
            Visit(child, options, diagnostics, childEnclosing);
        }
    }

    /// <summary>
    /// Reports a rule whose depth reaches <see cref="CompilerOptions.DeepNestingFraction"/> of
    /// <see cref="CompilerOptions.MaxDepth"/>, so the author hears about it before the hard limit rejects the rule.
    /// </summary>
    private static void LintDepth(Expression root, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        int depth = ExpressionTools.Depth(root);
        if (depth < options.MaxDepth * options.DeepNestingFraction)
        {
            return;
        }

        diagnostics.Add(
            Diagnostic.Info(
                DiagnosticCodes.DeepNesting,
                $"Strong K3 lint: the rule is {depth} levels deep, close to the limit of {options.MaxDepth}; consider flattening or splitting it",
                SourceSpan.None,
                expected: $"nesting well under {options.MaxDepth} deep",
                found: $"{depth} levels"
            )
        );
    }

    /// <summary>Reports an <c>AND</c> or <c>OR</c> chain with more operands than <see cref="CompilerOptions.WideChainOperandLimit"/>.</summary>
    private static void LintWideChain(Expression node, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        int count = node is AndExpression and ? and.Operands.Count : ((OrExpression)node).Operands.Count;
        if (count <= options.WideChainOperandLimit)
        {
            return;
        }

        string name = node is AndExpression ? "AND" : "OR";
        diagnostics.Add(
            Diagnostic.Info(
                DiagnosticCodes.WideChain,
                $"Strong K3 lint: this {name} chain has {count} operands, more than {options.WideChainOperandLimit}; consider grouping related operands",
                SourceSpan.None,
                expected: $"at most {options.WideChainOperandLimit} operands",
                found: $"{count} operands"
            )
        );
    }

    /// <summary>
    /// Reports a rule that <c>Canonicalize()</c> would change, with the canonical text as the replacement. Canonicalization
    /// keeps the value for every input, so the replacement is K3-equivalent. A rule larger than
    /// <see cref="CompilerOptions.MaxRewriteNodeCount"/> is skipped, as the expanding rewrites refuse such a size.
    /// </summary>
    private static void LintCanonical(Expression root, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        if (ExpressionTools.Size(root) > options.MaxRewriteNodeCount)
        {
            return;
        }

        Expression canonical = Canonicalizer.Canonicalize(root);
        if (canonical.Equals(root))
        {
            return;
        }

        diagnostics.Add(
            Finding(
                DiagnosticCodes.NotCanonical,
                "the rule is not in canonical form",
                "Canonicalize() gives an equivalent rule with one deterministic operand order and shape",
                root,
                canonical
            )
        );
    }

    /// <summary>
    /// An inspection is redundant when the states its operand can take (from the dual-rail BDD) make the test constant, or,
    /// for <c>IsTrue</c>/<c>IsFalse</c> over an operand that is never <c>Unknown</c>, make it a plain restatement of the
    /// operand (<c>IsTrue(x)</c> is <c>x</c>, <c>IsFalse(x)</c> is <c>NOT x</c> once <c>x</c> is two-valued).
    /// </summary>
    private static void LintInspection(InspectionExpression node, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        if (Analyzer.Profile(node.Operand, options.MaxAnalysisTerms) is not { } profile)
        {
            return;
        }

        // The inspection is True in the "tested" states and False in the "other" states; if the operand can reach only one
        // side the result is constant.
        bool testedCanHold = node.Kind switch
        {
            InspectionKind.IsTrue => profile.CanBeTrue,
            InspectionKind.IsFalse => profile.CanBeFalse,
            InspectionKind.IsUnknown => profile.CanBeUnknown,
            _ => profile.CanBeTrue || profile.CanBeFalse,
        };
        bool otherCanHold = node.Kind switch
        {
            InspectionKind.IsTrue => profile.CanBeFalse || profile.CanBeUnknown,
            InspectionKind.IsFalse => profile.CanBeTrue || profile.CanBeUnknown,
            InspectionKind.IsUnknown => profile.CanBeTrue || profile.CanBeFalse,
            _ => profile.CanBeUnknown,
        };

        Expression? replacement = null;
        string reason = string.Empty;
        if (!otherCanHold)
        {
            replacement = new ConstantExpression(TruthValue.True);
            reason = "the operand can only be in the state the inspection tests for";
        }
        else if (!testedCanHold)
        {
            replacement = new ConstantExpression(TruthValue.False);
            reason = "the operand can never be in the state the inspection tests for";
        }
        else if (!profile.CanBeUnknown && node.Kind == InspectionKind.IsTrue)
        {
            replacement = node.Operand;
            reason = "the operand is never Unknown, so testing it for True returns the operand itself";
        }
        else if (!profile.CanBeUnknown && node.Kind == InspectionKind.IsFalse)
        {
            replacement = new NotExpression(node.Operand);
            reason = "the operand is never Unknown, so testing it for False returns its negation";
        }

        if (replacement is null)
        {
            return;
        }

        diagnostics.Add(
            Finding(DiagnosticCodes.RedundantInspection, "this inspection is redundant", reason, node, replacement)
        );
    }

    /// <summary>
    /// <c>COALESCE(x, ...)</c> yields the first operand that is not <c>Unknown</c>, so once an operand can never be
    /// <c>Unknown</c> it always decides the result and every operand after it is dead. Only operands before the cut that
    /// can be <c>Unknown</c> still matter, so the replacement keeps the operands up to and including that one.
    /// </summary>
    private static void LintCoalesce(CoalesceExpression node, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        for (int i = 0; i < node.Operands.Count - 1; i++)
        {
            if (Analyzer.Profile(node.Operands[i], options.MaxAnalysisTerms) is { CanBeUnknown: false })
            {
                Expression replacement =
                    i == 0 ? node.Operands[0] : new CoalesceExpression(ExpressionTools.Array(node.Operands.Take(i + 1)));
                diagnostics.Add(
                    Finding(
                        DiagnosticCodes.RedundantCoalesce,
                        "the operands after the first one that can never be Unknown are unreachable",
                        "COALESCE returns the first operand that is not Unknown, and this operand never is",
                        node,
                        replacement
                    )
                );
                return;
            }
        }
    }

    /// <summary>
    /// <c>If(c, t, f)</c> is <c>(c AND t) OR (NOT c AND f) OR (t AND f)</c>. With <c>c</c> always <c>True</c> that is
    /// <c>t OR (t AND f)</c>, which is <c>t</c>; with <c>c</c> always <c>False</c> it is <c>f</c> likewise. A condition
    /// that can be <c>Unknown</c> is left alone, because then neither branch is simply chosen.
    /// </summary>
    private static void LintIfCondition(IfExpression node, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        if (
            Analyzer.Profile(node.Condition, options.MaxAnalysisTerms) is not { CanBeUnknown: false } profile
            || profile.CanBeTrue == profile.CanBeFalse
        )
        {
            return;
        }

        string justification = profile.CanBeTrue
            ? "the condition is always True, so If(c, t, f) is t"
            : "the condition is always False, so If(c, t, f) is f";
        diagnostics.Add(
            Finding(
                DiagnosticCodes.ConstantIfCondition,
                "the condition never changes, so one branch can never be chosen",
                justification,
                node,
                profile.CanBeTrue ? node.WhenTrue : node.WhenFalse
            )
        );
    }

    /// <summary>
    /// A threshold or <c>BETWEEN</c> is vacuous when its value is fixed by its constant operands alone. The check swaps
    /// every non-constant operand for a fresh, independent term and asks the BDD which values the node can then take: a
    /// single reachable value means no assignment of the real operands can change it, in K3 including <c>Unknown</c>.
    /// </summary>
    private static void LintCardinality(Expression node, CompilerOptions options, List<Diagnostic> diagnostics)
    {
        EquatableArray<Expression> operands = node is ThresholdExpression threshold
            ? threshold.Operands
            : ((BetweenExpression)node).Operands;
        if (!operands.Any(o => o is ConstantExpression))
        {
            return;
        }

        Expression[] generic = [.. operands.Select((o, i) => o is ConstantExpression ? o : FreshTerm(i))];
        Expression skeleton = node switch
        {
            ThresholdExpression t => t with { Operands = ExpressionTools.Array(generic) },
            BetweenExpression b => b with { Operands = ExpressionTools.Array(generic) },
            _ => node,
        };
        if (Analyzer.Profile(skeleton, options.MaxAnalysisTerms) is not { } profile)
        {
            return;
        }

        TruthValue? only = (profile.CanBeTrue, profile.CanBeFalse, profile.CanBeUnknown) switch
        {
            (true, false, false) => TruthValue.True,
            (false, true, false) => TruthValue.False,
            (false, false, true) => TruthValue.Unknown,
            _ => null,
        };
        if (only is { } value)
        {
            diagnostics.Add(
                Finding(
                    DiagnosticCodes.VacuousCardinality,
                    $"the constant operands already decide this count, so it is always {value}",
                    "whatever the other operands are, the count of true operands cannot cross the threshold either way",
                    node,
                    new ConstantExpression(value)
                )
            );
        }
    }

    /// <summary>
    /// Repeats are dropped only where K3 makes that exact: <c>AND</c>, <c>OR</c>, <c>ANY</c> and <c>ALL</c> are idempotent
    /// (<c>x AND x</c> is <c>x</c>, <c>Unknown</c> included) and a <c>COALESCE</c> operand equal to an earlier one is
    /// <c>Unknown</c> exactly when that one was, so it can never supply the result. <c>PARITY</c>, the counting operators
    /// and <c>XOR</c> are not idempotent (a repeat changes the count, or <c>x XOR x</c> is <c>Unknown</c> not <c>False</c>),
    /// so they are never flagged. Sameness is structural: identical operand trees, not merely equivalent ones.
    /// </summary>
    private static void LintDuplicates(Expression node, List<Diagnostic> diagnostics)
    {
        Func<EquatableArray<Expression>, Expression> rebuild;
        EquatableArray<Expression> operands;
        switch (node)
        {
            case AndExpression and:
                operands = and.Operands;
                rebuild = ops => new AndExpression(ops);
                break;
            case OrExpression or:
                operands = or.Operands;
                rebuild = ops => new OrExpression(ops);
                break;
            case AnyExpression any:
                operands = any.Operands;
                rebuild = ops => new AnyExpression(ops);
                break;
            case AllExpression all:
                operands = all.Operands;
                rebuild = ops => new AllExpression(ops);
                break;
            case CoalesceExpression coalesce:
                operands = coalesce.Operands;
                rebuild = ops => new CoalesceExpression(ops);
                break;
            default:
                return;
        }

        List<Expression> distinct = [.. operands.Distinct()];
        if (distinct.Count == operands.Count)
        {
            return;
        }

        Expression replacement = distinct.Count == 1 ? distinct[0] : rebuild(ExpressionTools.Array(distinct));
        diagnostics.Add(
            Finding(
                DiagnosticCodes.DuplicateOperands,
                "an operand appears more than once",
                "this connective is idempotent in Strong K3, so a repeated operand adds nothing",
                node,
                replacement
            )
        );
    }

    private static TermExpression FreshTerm(int index)
    {
        return new TermExpression(new TermIdentity($"$operand{index}", []));
    }

    /// <summary>Builds an informational lint finding: what is wrong, the K3 justification, the construct and its replacement.</summary>
    private static Diagnostic Finding(string code, string what, string justification, Expression node, Expression replacement)
    {
        string printed = CanonicalPrinter.Print(node);
        return Diagnostic.Info(
            code,
            $"Strong K3 lint: {what} ({justification}): {printed}",
            SourceSpan.None,
            found: printed,
            suggestion: new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, CanonicalPrinter.Print(replacement))
        );
    }
}
