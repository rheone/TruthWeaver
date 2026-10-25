namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// Counts nodes against a <c>CompilerOptions.MaxNodeCount</c> budget while a tree is read, expanded or walked, and
/// reports <see cref="DiagnosticCodes.MaxNodeCountExceeded"/> exactly once no matter how many calls are still
/// unwinding above the one that tripped it. <see cref="TreeFormatReader"/>, <c>DefinitionExpander</c> and
/// <c>RuleDefinitions.Reader</c> each count a node the same way, once, before recursing into its operands (tickets
/// 04 and 05); this holds that shape in one place instead of three.
/// </summary>
/// <param name="maxNodeCount">The most nodes this guard allows before it reports the over-limit diagnostic.</param>
internal sealed class NodeCountGuard(int maxNodeCount)
{
    private int count;
    private bool reported;

    /// <summary>Counts one more node.</summary>
    /// <param name="diagnostics">The sink to add the over-limit diagnostic to, the first time the budget is exceeded.</param>
    /// <param name="span">The span of the node being counted, used as the diagnostic's location.</param>
    /// <param name="path">The node's path in the document, or <see langword="null"/> for DSL text.</param>
    /// <returns>
    /// <see langword="true"/> while the count is still within the budget; once past it, <see langword="false"/>,
    /// so the caller can bail out instead of recursing into this node's operands.
    /// </returns>
    public bool TryCount(List<Diagnostic> diagnostics, SourceSpan span, string? path)
    {
        this.count++;
        if (this.count <= maxNodeCount)
        {
            return true;
        }

        if (!this.reported)
        {
            this.reported = true;
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MaxNodeCountExceeded,
                    $"Rule exceeds the maximum node count of {maxNodeCount}.",
                    span,
                    expected: $"at most {maxNodeCount} nodes",
                    found: "more nodes than that",
                    path: path
                )
            );
        }

        return false;
    }
}
