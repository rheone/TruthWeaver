namespace TruthWeaver.Diffing;

using TruthWeaver.Evaluation;

/// <summary>
/// Renders a <see cref="RuleDiffResult"/> as human-readable text, suitable for an
/// audit-log entry or a rule-review UI: one line per <see cref="RuleDiffEntry"/>, naming each node by
/// its <see cref="OutlineNode.Label"/>/<see cref="OutlineNode.Description"/> rather than its
/// closed-set AST type name (ADR-0004).
/// </summary>
/// <example>
/// Given a "before" rule <c>isManager AND isDepartmentHead</c> and an "after" rule
/// <c>isManager AND isDepartmentHead AND hasRole(role: "Y")</c>:
/// <code>
/// RuleDiffResult diff = RuleDiff.Compare(before, after);
/// string text = RuleDiffPrinter.Print(diff);
/// // "Added operand at root/2: hasRole - Test predicate 'hasRole', true iff 'role' equals 'Y'."
/// </code>
/// </example>
public static class RuleDiffPrinter
{
    /// <summary>Renders a diff as human-readable text, one line per entry.</summary>
    /// <param name="diff">The diff to render.</param>
    /// <returns><c>"No changes."</c> when <paramref name="diff"/> is empty; otherwise one line per entry, newline-separated.</returns>
    public static string Print(RuleDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return diff.HasChanges ? string.Join('\n', diff.Entries.Select(PrintEntry)) : "No changes.";
    }

    /// <summary>
    /// Writes a diff as human-readable text to <paramref name="writer"/>, one entry at a time. The output is exactly the text
    /// that <see cref="Print(RuleDiffResult)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="diff">The diff to render.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="diff"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public static void Print(TextWriter writer, RuleDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(diff);

        if (!diff.HasChanges)
        {
            writer.Write("No changes.");
            return;
        }

        bool first = true;
        foreach (RuleDiffEntry entry in diff.Entries)
        {
            if (!first)
            {
                writer.Write('\n');
            }

            first = false;
            writer.Write(PrintEntry(entry));
        }
    }

    private static string PrintEntry(RuleDiffEntry entry)
    {
        string path = PrintPath(entry.Path);
        return entry.Kind switch
        {
            RuleDiffChangeKind.Added => $"Added operand at {path}: {PrintNode(entry.After)}",
            RuleDiffChangeKind.Removed => $"Removed operand at {path}: {PrintNode(entry.Before)}",
            RuleDiffChangeKind.Changed => $"Changed at {path}: {PrintNode(entry.Before)} -> {PrintNode(entry.After)}",
            _ => throw new InvalidOperationException($"Unhandled diff change kind '{entry.Kind}'."),
        };
    }

    private static string PrintNode(OutlineNode? node)
    {
        return node is null ? "(none)" : $"{node.Label} - {node.Description}";
    }

    private static string PrintPath(IReadOnlyList<int> path)
    {
        return path.Count == 0 ? "root" : "root/" + string.Join('/', path);
    }
}
