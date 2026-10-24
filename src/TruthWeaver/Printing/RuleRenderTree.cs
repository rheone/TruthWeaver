namespace TruthWeaver.Printing;

using TruthWeaver.Ast;
using TruthWeaver.Evaluation;

/// <summary>
/// <para>
/// Zips a rule's <see cref="OutlineNode"/> (what to say: labels, always the full static shape)
/// against an optional <see cref="TraceNode"/> (what happened: results and skips, only as deep as
/// evaluation actually went) into one <see cref="RenderNode"/> tree. Both source trees are built from
/// the same shared node-shape seam (<c>ExpressionShape.Of</c>): <c>CompiledRule.OutlineOf</c> and
/// <c>Evaluator</c> respectively, so they align positionally with no need to match by text or replay
/// any short-circuit logic here.
/// </para>
/// <para>
/// Every format-specific printer (<see cref="MermaidTreePrinter"/>, <see cref="PlainTextTreePrinter"/>)
/// renders this one shared tree, so "what a skipped subtree looks like" is decided once, not per format.
/// </para>
/// </summary>
internal static class RuleRenderTree
{
    /// <summary>Builds a purely structural render tree, with no evaluation coloring.</summary>
    /// <param name="description">The rule's outline.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/EQUIVALENT operator labels.</param>
    /// <param name="showArgumentValues">Whether to include a term's <see cref="OutlineNode.ArgumentText"/> in its rendered label.</param>
    /// <param name="constants">How to write a constant leaf, or <see langword="null"/> for the outline's own label.</param>
    /// <returns>The render tree.</returns>
    public static RenderNode Build(
        OutlineNode description,
        OperatorStyle style = OperatorStyle.Word,
        bool showArgumentValues = true,
        ConstantFormat? constants = null
    )
    {
        return Build(description, evaluated: null, ancestorSkipped: false, style, showArgumentValues, constants);
    }

    /// <summary>Builds a render tree colored by one evaluation's result tree.</summary>
    /// <param name="description">The rule's outline.</param>
    /// <param name="evaluated">The root of the matching <see cref="Decision.TraceTree"/>.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/EQUIVALENT operator labels.</param>
    /// <param name="showArgumentValues">Whether to include a term's <see cref="OutlineNode.ArgumentText"/> in its rendered label.</param>
    /// <param name="constants">How to write a constant leaf, or <see langword="null"/> for the outline's own label.</param>
    /// <returns>The render tree.</returns>
    public static RenderNode Build(
        OutlineNode description,
        TraceNode evaluated,
        OperatorStyle style = OperatorStyle.Word,
        bool showArgumentValues = true,
        ConstantFormat? constants = null
    )
    {
        return Build(description, evaluated, ancestorSkipped: false, style, showArgumentValues, constants);
    }

    private static RenderNode Build(
        OutlineNode description,
        TraceNode? evaluated,
        bool ancestorSkipped,
        OperatorStyle style,
        bool showArgumentValues,
        ConstantFormat? constants
    )
    {
        if (description.QuantifierVariable is not null)
        {
            return BuildQuantifier(description, evaluated, ancestorSkipped, style, showArgumentValues, constants);
        }

        AssertOperandCountsAligned(description, evaluated);

        bool skipped = ancestorSkipped || evaluated is { NotEvaluated: true };
        RenderState state = skipped ? RenderState.Skipped : StateFor(evaluated?.Result);

        // A skipped subtree's TraceNode has no children (evaluation never recursed into it), so
        // there is nothing per-descendant to pass down. "skipped" itself propagates via `skipped`.
        IReadOnlyList<TraceNode>? children = evaluated is { NotEvaluated: false } ? evaluated.Children : null;

        List<RenderNode> renderedChildren = [with(description.Operands.Count)];
        for (int i = 0; i < description.Operands.Count; i++)
        {
            TraceNode? childEvaluated = children is { Count: > 0 } ? children[i] : null;
            renderedChildren.Add(Build(description.Operands[i], childEvaluated, skipped, style, showArgumentValues, constants));
        }

        OutlineNodeKind kind = description.Operands.Count > 0 ? OutlineNodeKind.Operator : description.Kind;
        return new RenderNode(
            StyledLabel(description, style, showArgumentValues, constants),
            state,
            renderedChildren,
            kind,
            BaseLabel(description, style, constants),
            showArgumentValues ? description.ArgumentText : null,
            description
        );
    }

    /// <summary>
    /// Builds a quantifier node (ADR-0010 decision 7). Its <see cref="TraceNode"/> has one child per element, each
    /// wrapping the body's own trace as its single child, not one child per written operand like every other
    /// node, so <see cref="AssertOperandCountsAligned"/> does not apply here. Every element's body trace is folded
    /// onto the outline's one body operand instead of zipped positionally: a body position counts as reached when
    /// at least one element reached it, and its value is the common value when every element that reached it
    /// agrees, or <see cref="TruthValue.Unknown"/> when elements disagree.
    /// </summary>
    private static RenderNode BuildQuantifier(
        OutlineNode description,
        TraceNode? evaluated,
        bool ancestorSkipped,
        OperatorStyle style,
        bool showArgumentValues,
        ConstantFormat? constants
    )
    {
        bool skipped = ancestorSkipped || evaluated is { NotEvaluated: true };
        RenderState state = skipped ? RenderState.Skipped : StateFor(evaluated?.Result);

        TraceNode? bodyTrace = null;
        if (evaluated is { NotEvaluated: false } quantifierTrace && quantifierTrace.Children.Count > 0)
        {
            bodyTrace = MergeElementBodies(quantifierTrace.Children);
        }

        RenderNode bodyNode = Build(description.Operands[0], bodyTrace, skipped, style, showArgumentValues, constants);
        return new RenderNode(
            StyledLabel(description, style, showArgumentValues, constants),
            state,
            [bodyNode],
            OutlineNodeKind.Operator,
            BaseLabel(description, style, constants),
            showArgumentValues ? description.ArgumentText : null,
            description
        );
    }

    /// <summary>Takes each element's per-body trace (the single child of its per-element trace node) and merges them.</summary>
    private static TraceNode MergeElementBodies(EquatableArray<TraceNode> elementTraces)
    {
        List<TraceNode> bodies = [with(elementTraces.Count)];
        foreach (TraceNode element in elementTraces)
        {
            if (element.Children.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one body trace for quantifier element '{element.Text}', found {element.Children.Count}."
                );
            }

            bodies.Add(element.Children[0]);
        }

        return MergeBodyTraces(bodies);
    }

    /// <summary>Merges one body position's trace across every element that reached it, then recurses into its children.</summary>
    private static TraceNode MergeBodyTraces(IReadOnlyList<TraceNode> bodies)
    {
        List<TraceNode> reached = [.. bodies.Where(b => !b.NotEvaluated)];
        if (reached.Count == 0)
        {
            return bodies[0] with { Result = null, NotEvaluated = true, Children = EquatableArray<TraceNode>.Empty };
        }

        TruthValue? value = reached.All(b => b.Result == reached[0].Result) ? reached[0].Result : TruthValue.Unknown;
        int childCount = reached.Max(b => b.Children.Count);
        List<TraceNode> mergedChildren = [with(childCount)];
        for (int i = 0; i < childCount; i++)
        {
            mergedChildren.Add(MergeBodyTraces([.. reached.Where(b => i < b.Children.Count).Select(b => b.Children[i])]));
        }

        return new TraceNode(reached[0].Text, value, false, new EquatableArray<TraceNode>(mergedChildren));
    }

    /// <summary>
    /// Renders <paramref name="description"/>'s label in <paramref name="style"/>, with its
    /// <see cref="OutlineNode.ArgumentText"/> appended when <paramref name="showArgumentValues"/>
    /// is <see langword="true"/> and the term has any. Only an operator node's exact word-form label
    /// (<c>AND</c>, <c>OR</c>, <c>NOT</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>IMPLIES</c>, <c>NAND</c>, <c>NOR</c>) with at least one operand is
    /// eligible for restyling: a term or constant leaf (always zero operands) is never restyled even
    /// if a predicate's authored label happens to collide with one of those words, and
    /// <c>ExactlyOne</c>/threshold/<c>BETWEEN</c>/inspection labels (e.g. <c>AtLeast(3)</c>) fall through unchanged in every
    /// style, since they have no symbolic or C-style spelling. <c>If</c> keeps its word label except in the C-style form,
    /// where it renders as <c>?:</c>, mirroring the DSL ternary. <c>COALESCE</c> renders as <c>??</c> in both the
    /// symbolic and the C-style form. <c>IMPLIES</c> has a symbolic spelling
    /// (<c>→</c>) but no C-family one, so it keeps its word form in <see cref="OperatorStyle.CStyle"/>; <c>NAND</c>/<c>NOR</c> behave the same way (<c>↑</c>/<c>↓</c>).
    /// </summary>
    private static string StyledLabel(
        OutlineNode description,
        OperatorStyle style,
        bool showArgumentValues,
        ConstantFormat? constants
    )
    {
        string label = BaseLabel(description, style, constants);
        return showArgumentValues && description.ArgumentText is { } argumentText ? $"{label} ({argumentText})" : label;
    }

    private static string BaseLabel(OutlineNode description, OperatorStyle style, ConstantFormat? constants)
    {
        // A quantifier's head (ADR-0010) renders as part of the label itself, in each style: "FORALL line IN
        // lines" (word, and C-style, which has no quantifier glyph of its own) or "∀ line ∈ lines" (symbolic).
        // This bypasses ArgumentText's "label (argumentText)" shape below, which is for a term's arguments, not a
        // quantifier's head.
        if (description is { QuantifierVariable: { } variable, QuantifierCollection: { } collection })
        {
            if (style != OperatorStyle.Symbolic)
            {
                return $"{description.Label} {variable} IN {collection}";
            }

            string symbol = QuantifierText.Glyph(description.Label);
            return $"{symbol} {variable} ∈ {collection}";
        }

        // A constant leaf takes the constant format. Its outline label is always the canonical word, so the word tells the value.
        if (
            constants is not null
            && description.Kind == OutlineNodeKind.Constant
            && TruthValueText.TryParse(description.Label, out TruthValue value)
        )
        {
            return constants.Text(value);
        }

        if (style == OperatorStyle.Word || description.Operands.Count == 0)
        {
            return description.Label;
        }

        OperatorDefinitions.TryGetByLabel(description.Label, out OperatorDefinition? definition);
        return style switch
        {
            OperatorStyle.Symbolic => definition?.SymbolicSpelling ?? description.Label,
            OperatorStyle.CStyle => definition?.CStyleSpelling ?? description.Label,
            _ => throw new InvalidOperationException($"Unhandled operator style '{style}'."),
        };
    }

    /// <summary>
    /// Guards the positional-zip invariant this method depends on: <see cref="OutlineNode"/> and
    /// <see cref="TraceNode"/> are built by two independent recursive traversals (<c>CompiledRule.OutlineOf</c>
    /// and <see cref="Evaluation.Evaluator{TContext}"/> respectively. Nothing structural stops them from disagreeing on operand count if either traversal is edited carelessly.
    /// Runs for every node except a quantifier (<see cref="BuildQuantifier"/> folds its trace instead, per ADR-0010
    /// decision 7): without it, a mismatch surfaces later as an <see cref="IndexOutOfRangeException"/>
    /// from the positional zip below, which is far harder to diagnose than this guard's explicit message.
    /// </summary>
    private static void AssertOperandCountsAligned(OutlineNode description, TraceNode? evaluated)
    {
        if (evaluated is { NotEvaluated: false } && evaluated.Children.Count != description.Operands.Count)
        {
            throw new InvalidOperationException(
                $"OutlineNode/TraceNode operand-count mismatch at node '{description.Label}': "
                    + $"the description has {description.Operands.Count} operand(s) but the evaluated "
                    + $"tree has {evaluated.Children.Count}. RuleRenderTree.Build zips these two trees "
                    + "positionally, assuming both were built from the same operand order."
            );
        }
    }

    private static RenderState StateFor(TruthValue? result)
    {
        return result switch
        {
            TruthValue.True => RenderState.True,
            TruthValue.False => RenderState.False,
            TruthValue.Unknown => RenderState.Indeterminate,
            null => RenderState.NoData,
            _ => throw new InvalidOperationException($"Unhandled truth value '{result}'."),
        };
    }
}
