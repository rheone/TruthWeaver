namespace TruthWeaver.Parsing;

using System.Runtime.CompilerServices;

/// <summary>
/// Attaches each comment of DSL rule text to one node of the parsed tree as that node's note (ADR-0005 decision 2,
/// amended 2026-10-11). The rule, in order of precedence:
/// <list type="number">
/// <item><description><b>Trailing.</b> A comment on the same line as the token before it attaches to the smallest node that ends at that token (<c>b // note</c>).</description></item>
/// <item><description><b>Leading.</b> Otherwise it attaches to the smallest node that starts at the token after it (<c>/* note */ b</c>, <c>// note</c> on the line above).</description></item>
/// <item><description>Otherwise to the smallest node that ends at the token before it, on any line.</description></item>
/// <item><description>Otherwise to the smallest node that encloses it, such as a call whose bound it sits beside.</description></item>
/// </list>
/// A node's extent includes the delimiters of a group written around it, so a comment before <c>(a AND b)</c> is the
/// note of the <c>AND</c> node and a comment before <c>a</c> inside the group is the note of <c>a</c>. Several comments
/// on one node join, in source order, with a line feed.
/// </summary>
/// <remarks>
/// Every lookup below is amortized O(1) or O(log children) rather than a scan of every node extent per comment: both
/// comments and tokens are in source order, so the token boundaries that surround each comment are found with a single
/// shared forward pointer across all comments (ticket 25), and the four attachment rules are answered from indexes
/// built once over the tree (a start/end lookup for the equality rules, and a parent/children map, with children kept
/// sorted by start, for the enclosing-node rule's binary search). The old shape re-scanned every extent for every
/// comment, which made a comment-heavy input quadratic.
/// </remarks>
internal static class NoteAttacher
{
    /// <summary>Attaches every comment to a node by setting <see cref="RuleNode.Note"/>.</summary>
    /// <param name="root">The parsed tree.</param>
    /// <param name="comments">The comments, in source order.</param>
    /// <param name="tokens">The token stream the tree was parsed from.</param>
    /// <param name="source">The rule text.</param>
    /// <param name="groups">The extent of each node that was written inside grouping delimiters, delimiters included.</param>
    /// <returns>
    /// The total number of token-cursor advances <see cref="ScanTokens"/> made across every comment: a deterministic,
    /// machine-independent proxy for the work this method did, for a test to assert scales linearly with the input
    /// rather than quadratically (ticket 25 fixed a quadratic re-scan here; this count is what a regression of that
    /// fix would blow up, where a wall-clock measurement is noisy under load). <see langword="0"/> when there are no
    /// comments or nothing to attach to.
    /// </returns>
    public static long Attach(
        RuleNode root,
        IReadOnlyList<Comment> comments,
        IReadOnlyList<Token> tokens,
        string source,
        IReadOnlyDictionary<RuleNode, (int Start, int End)> groups
    )
    {
        if (comments.Count == 0)
        {
            return 0;
        }

        List<Extent> extents = [];
        Measure(root, 0, groups, extents);
        if (extents.Count == 0)
        {
            return 0;
        }

        NodeIndex index = NodeIndex.Build(extents);

        Dictionary<RuleNode, List<string>> pieces = [with(ReferenceEqualityComparer.Instance)];
        int tokenCursor = 0;
        long scanSteps = 0;
        foreach (Comment comment in comments)
        {
            (int? previousEnd, int? nextStart) = ScanTokens(tokens, comment, ref tokenCursor, ref scanSteps);
            RuleNode target = FindTarget(comment, previousEnd, nextStart, source, index);
            if (!pieces.TryGetValue(target, out List<string>? list))
            {
                list = [];
                pieces[target] = list;
            }

            list.Add(comment.Text);
        }

        foreach ((RuleNode node, List<string> list) in pieces)
        {
            node.Note = NoteText.Normalize(string.Join('\n', list));
        }

        return scanSteps;
    }

    /// <summary>
    /// Finds the token boundaries immediately surrounding <paramref name="comment"/>: the end of the nearest token
    /// before it and the start of the nearest token after it. Both <paramref name="tokens"/> and the comments
    /// <see cref="Attach"/> iterates are in source order, so <paramref name="cursor"/> only ever moves forward across
    /// the whole comment list; a token it has already stepped past for an earlier comment is never revisited.
    /// </summary>
    /// <param name="tokens">The token stream, in source order.</param>
    /// <param name="comment">The comment to locate.</param>
    /// <param name="cursor">The shared forward cursor into <paramref name="tokens"/>, advanced in place.</param>
    /// <param name="steps">Accrues one for every cursor advance, across every comment in one <see cref="Attach"/> call.</param>
    private static (int? PreviousEnd, int? NextStart) ScanTokens(
        IReadOnlyList<Token> tokens,
        Comment comment,
        ref int cursor,
        ref long steps
    )
    {
        while (cursor < tokens.Count && tokens[cursor].Kind != TokenKind.Eof && tokens[cursor].Span.End <= comment.Span.Start)
        {
            cursor++;
            steps++;
        }

        int? previousEnd = cursor > 0 ? tokens[cursor - 1].Span.End : null;
        int? nextStart =
            cursor < tokens.Count && tokens[cursor].Kind != TokenKind.Eof && tokens[cursor].Span.Start >= comment.Span.End
                ? tokens[cursor].Span.Start
                : null;
        return (previousEnd, nextStart);
    }

    private static RuleNode FindTarget(Comment comment, int? previousEnd, int? nextStart, string source, NodeIndex index)
    {
        bool sameLine = previousEnd is { } end && source.AsSpan(end, comment.Span.Start - end).IndexOf('\n') < 0;

        return (sameLine && previousEnd is { } sameEnd ? index.DeepestByEnd(sameEnd) : null)
            ?? (nextStart is { } start ? index.DeepestByStart(start) : null)
            ?? (previousEnd is { } anyEnd ? index.DeepestByEnd(anyEnd) : null)
            ?? index.FindEnclosing(comment.Span.Start, comment.Span.End)
            ?? index.Root;
    }

    /// <summary>
    /// Records the extent of <paramref name="node"/> and everything below it: its own span, widened to any group written
    /// around it and to the extents of its operands (an <c>AND</c> chain's span starts at its first operand's text, which
    /// may itself be inside parentheses). Error placeholders are skipped, so a comment never becomes the note of one.
    /// </summary>
    /// <returns>The node's extent, or <see langword="null"/> for an error placeholder.</returns>
    private static (int Start, int End)? Measure(
        RuleNode node,
        int depth,
        IReadOnlyDictionary<RuleNode, (int Start, int End)> groups,
        List<Extent> extents
    )
    {
        if (node is ErrorNode)
        {
            return null;
        }

        // Belt-and-suspenders: the parsers that build this tree (DslParser, TreeFormatReader) already bound its
        // depth against CompilerOptions.MaxDepth, but that limit is caller-configurable and this traversal has no
        // diagnostics channel of its own. Rather than thread a second depth budget and an error path through every
        // call site just for a note-attachment nicety, stop descending if the stack is genuinely running low; the
        // affected subtree simply keeps no note instead of risking a StackOverflowException.
        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            return null;
        }

        int index = extents.Count;
        int start = node.Span.Start;
        int end = node.Span.End;
        extents.Add(new Extent(node, start, end, depth));
        foreach (RuleNode operand in RuleNode.OperandsOf(node))
        {
            if (Measure(operand, depth + 1, groups, extents) is { } inner)
            {
                start = Math.Min(start, inner.Start);
                end = Math.Max(end, inner.End);
            }
        }

        if (groups.TryGetValue(node, out (int Start, int End) group))
        {
            start = Math.Min(start, group.Start);
            end = Math.Max(end, group.End);
        }

        extents[index] = new Extent(node, start, end, depth);
        return (start, end);
    }

    private readonly record struct Extent(RuleNode Node, int Start, int End, int Depth);

    /// <summary>
    /// The lookups <see cref="FindTarget"/> needs over a tree's extents, built once in <see cref="Build"/> (a single
    /// linear pass) instead of re-scanning every extent for every comment.
    /// </summary>
    private sealed class NodeIndex
    {
        private readonly Dictionary<int, Extent> deepestByEnd;
        private readonly Dictionary<int, Extent> deepestByStart;
        private readonly Dictionary<RuleNode, Extent> extentOf;
        private readonly Dictionary<RuleNode, List<RuleNode>> childrenOf;

        private NodeIndex(
            RuleNode root,
            Dictionary<int, Extent> deepestByEnd,
            Dictionary<int, Extent> deepestByStart,
            Dictionary<RuleNode, Extent> extentOf,
            Dictionary<RuleNode, List<RuleNode>> childrenOf
        )
        {
            this.Root = root;
            this.deepestByEnd = deepestByEnd;
            this.deepestByStart = deepestByStart;
            this.extentOf = extentOf;
            this.childrenOf = childrenOf;
        }

        /// <summary>Gets the tree's root, the final fallback when nothing else matches.</summary>
        public RuleNode Root { get; }

        /// <summary>
        /// Builds the index from a tree's extents (<see cref="Measure"/>'s preorder output, depth-first): a start/end
        /// lookup for the equality rules, and the parent/children map (children sorted by start, so the fourth rule can
        /// binary-search them) rebuilt from the preorder-plus-depth sequence with a single stack pass, with no second
        /// tree walk.
        /// </summary>
        public static NodeIndex Build(List<Extent> extents)
        {
            Dictionary<int, Extent> deepestByEnd = [];
            Dictionary<int, Extent> deepestByStart = [];
            Dictionary<RuleNode, Extent> extentOf = new(extents.Count, ReferenceEqualityComparer.Instance);
            Dictionary<RuleNode, List<RuleNode>> childrenOf = [with(ReferenceEqualityComparer.Instance)];

            // Nodes that share a boundary are nested (an ancestor and a descendant that both start, or both end, at the
            // same offset), so the one with the greater depth is the smallest and wins the lookup.
            foreach (Extent extent in extents)
            {
                if (!deepestByEnd.TryGetValue(extent.End, out Extent existingEnd) || extent.Depth > existingEnd.Depth)
                {
                    deepestByEnd[extent.End] = extent;
                }

                if (!deepestByStart.TryGetValue(extent.Start, out Extent existingStart) || extent.Depth > existingStart.Depth)
                {
                    deepestByStart[extent.Start] = extent;
                }

                extentOf[extent.Node] = extent;
            }

            // Reconstruct direct parent/children links from the preorder-plus-depth sequence: pop the ancestor stack
            // back to the nearest shallower entry, which is this extent's parent, then push this extent. Children land
            // in each parent's list in the order they were visited, which is source order (left to right), so the list
            // is already sorted by start for FindEnclosing's binary search.
            Stack<Extent> ancestors = new();
            foreach (Extent extent in extents)
            {
                while (ancestors.Count > 0 && ancestors.Peek().Depth >= extent.Depth)
                {
                    ancestors.Pop();
                }

                if (ancestors.Count > 0)
                {
                    RuleNode parent = ancestors.Peek().Node;
                    if (!childrenOf.TryGetValue(parent, out List<RuleNode>? children))
                    {
                        children = [];
                        childrenOf[parent] = children;
                    }

                    children.Add(extent.Node);
                }

                ancestors.Push(extent);
            }

            return new NodeIndex(extents[0].Node, deepestByEnd, deepestByStart, extentOf, childrenOf);
        }

        /// <summary>The deepest node whose extent ends exactly at <paramref name="end"/>, or <see langword="null"/>.</summary>
        public RuleNode? DeepestByEnd(int end)
        {
            return this.deepestByEnd.TryGetValue(end, out Extent extent) ? extent.Node : null;
        }

        /// <summary>The deepest node whose extent starts exactly at <paramref name="start"/>, or <see langword="null"/>.</summary>
        public RuleNode? DeepestByStart(int start)
        {
            return this.deepestByStart.TryGetValue(start, out Extent extent) ? extent.Node : null;
        }

        /// <summary>
        /// The deepest node whose extent fully contains <c>[start, end)</c>, found by descending from the root into
        /// whichever child contains the span, one level at a time, with a binary search (children are pairwise disjoint
        /// and sorted by start, so at most one can contain any given span) rather than a scan of every node.
        /// </summary>
        public RuleNode? FindEnclosing(int start, int end)
        {
            if (!this.extentOf.TryGetValue(this.Root, out Extent current) || current.Start > start || current.End < end)
            {
                return null;
            }

            RuleNode node = this.Root;
            while (this.childrenOf.TryGetValue(node, out List<RuleNode>? children))
            {
                RuleNode? next = FindContainingChild(children, start, end, this.extentOf);
                if (next is null)
                {
                    break;
                }

                node = next;
            }

            return node;
        }

        private static RuleNode? FindContainingChild(
            List<RuleNode> children,
            int start,
            int end,
            Dictionary<RuleNode, Extent> extentOf
        )
        {
            int low = 0;
            int high = children.Count - 1;
            int candidate = -1;
            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                if (extentOf[children[mid]].Start <= start)
                {
                    candidate = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            if (candidate < 0)
            {
                return null;
            }

            RuleNode child = children[candidate];
            return extentOf[child].End >= end ? child : null;
        }
    }
}
