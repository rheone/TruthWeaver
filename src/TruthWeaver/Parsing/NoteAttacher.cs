namespace TruthWeaver.Parsing;

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
internal static class NoteAttacher
{
    /// <summary>Attaches every comment to a node by setting <see cref="RuleNode.Note"/>.</summary>
    /// <param name="root">The parsed tree.</param>
    /// <param name="comments">The comments, in source order.</param>
    /// <param name="tokens">The token stream the tree was parsed from.</param>
    /// <param name="source">The rule text.</param>
    /// <param name="groups">The extent of each node that was written inside grouping delimiters, delimiters included.</param>
    public static void Attach(
        RuleNode root,
        IReadOnlyList<Comment> comments,
        IReadOnlyList<Token> tokens,
        string source,
        IReadOnlyDictionary<RuleNode, (int Start, int End)> groups
    )
    {
        if (comments.Count == 0)
        {
            return;
        }

        List<Extent> extents = [];
        Measure(root, 0, groups, extents);
        if (extents.Count == 0)
        {
            return;
        }

        Dictionary<RuleNode, List<string>> pieces = [with(ReferenceEqualityComparer.Instance)];
        foreach (Comment comment in comments)
        {
            RuleNode target = FindTarget(comment, tokens, source, extents);
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
    }

    private static RuleNode FindTarget(Comment comment, IReadOnlyList<Token> tokens, string source, List<Extent> extents)
    {
        int? previousEnd = null;
        int? nextStart = null;
        foreach (Token token in tokens)
        {
            if (token.Kind == TokenKind.Eof)
            {
                break;
            }

            if (token.Span.End <= comment.Span.Start)
            {
                previousEnd = token.Span.End;
            }
            else if (token.Span.Start >= comment.Span.End)
            {
                nextStart = token.Span.Start;
                break;
            }
        }

        bool sameLine = previousEnd is { } end && source.AsSpan(end, comment.Span.Start - end).IndexOf('\n') < 0;

        return (sameLine ? Deepest(extents, e => e.End == previousEnd) : null)
            ?? (nextStart is { } start ? Deepest(extents, e => e.Start == start) : null)
            ?? (previousEnd is { } anyEnd ? Deepest(extents, e => e.End == anyEnd) : null)
            ?? Deepest(extents, e => e.Start <= comment.Span.Start && e.End >= comment.Span.End)
            ?? extents[0].Node;
    }

    /// <summary>Gets the deepest node whose extent matches; nodes that share a start or an end are nested, so the deepest is the smallest.</summary>
    private static RuleNode? Deepest(List<Extent> extents, Func<Extent, bool> matches)
    {
        Extent? best = null;
        foreach (Extent extent in extents)
        {
            if (matches(extent) && (best is null || extent.Depth > best.Value.Depth))
            {
                best = extent;
            }
        }

        return best?.Node;
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
}
