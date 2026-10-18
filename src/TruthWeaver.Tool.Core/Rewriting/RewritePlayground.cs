namespace TruthWeaver.Tool.Core.Rewriting;

using System.Text.Json.Nodes;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Yaml;

/// <summary>
/// Applies the rewrites of the library to a rule without changing it, so a front end can show the result before the author
/// accepts it, and compares a rule with an earlier version of it. A front end puts an accepted result into the document
/// with <see cref="RuleDocument.ReplaceRule"/>.
/// </summary>
public static class RewritePlayground
{
    /// <summary>Applies a rewrite to a rule.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="kind">The rewrite.</param>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>. <see cref="ToolSettings.MaxRewriteNodeCount"/> caps the normal forms and the expansion.</param>
    /// <returns>The rule before and after, the steps and the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="RuleRewriteKind"/>.</exception>
    public static RewritePreview Preview(CompiledRule<StubAssignment> rule, RuleRewriteKind kind, ToolSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        int cap = (settings ?? ToolSettings.Default).MaxRewriteNodeCount;
        if (kind == RuleRewriteKind.Simplify)
        {
            SimplifyResult<StubAssignment> simplified = rule.SimplifyWithSteps();
            return new RewritePreview(kind, rule, simplified.Rule, simplified.Steps, simplified.Diagnostics);
        }

        CompilationResult<StubAssignment> result = kind switch
        {
            RuleRewriteKind.Canonicalize => rule.Canonicalize(),
            RuleRewriteKind.NegationNormalForm => rule.ToNnf(maxNodeCount: cap),
            RuleRewriteKind.ConjunctiveNormalForm => rule.ToCnf(maxNodeCount: cap),
            RuleRewriteKind.DisjunctiveNormalForm => rule.ToDnf(maxNodeCount: cap),
            RuleRewriteKind.Compress => rule.CompressToDerived(),
            RuleRewriteKind.ExpandToPrimitives => rule.ExpandToPrimitives(cap),
            RuleRewriteKind.OptimizeOrder => rule.OptimizeOrder(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a defined rewrite."),
        };
        return new RewritePreview(kind, rule, result.Rule, [], result.Diagnostics);
    }

    /// <summary>
    /// Compares a rule with an earlier version of it: whether they are equivalent under Strong K3, and whether the current
    /// rule is tighter or looser. Both checks stop at <see cref="ToolSettings.MaxAnalysisTerms"/> distinct terms and then
    /// report that they are undecided.
    /// </summary>
    /// <param name="earlier">The earlier version, such as the last saved or accepted rule.</param>
    /// <param name="current">The current rule.</param>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The comparison.</returns>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    public static RuleComparison Compare(
        CompiledRule<StubAssignment> earlier,
        CompiledRule<StubAssignment> current,
        ToolSettings? settings = null
    )
    {
        ArgumentNullException.ThrowIfNull(earlier);
        ArgumentNullException.ThrowIfNull(current);

        int cap = (settings ?? ToolSettings.Default).MaxAnalysisTerms;
        return new RuleComparison(
            RuleEquivalence.Compare(earlier, current, cap),
            RuleImplication.Compare(earlier, current, cap)
        );
    }

    /// <summary>
    /// Puts the notes of a rule back on the rule that a rewrite made from it. A rewrite returns a rule with no notes. A note
    /// stays when the node at its path has the same subtree after the rewrite as before it; a note on a node that the
    /// rewrite changed, moved or removed is dropped, since it may no longer say the right thing about that node.
    /// </summary>
    /// <param name="before">The rule before the rewrite.</param>
    /// <param name="after">The rule after the rewrite.</param>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The rule after the rewrite with the notes that stayed, and the number of notes that were dropped.</returns>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    public static NotedRewrite CarryNotes(
        CompiledRule<StubAssignment> before,
        CompiledRule<StubAssignment> after,
        ToolSettings? settings = null
    )
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        int total = before.Notes.Count;
        if (total == 0)
        {
            return new NotedRewrite(after, 0);
        }

        JsonObject beforeTree = (JsonObject)JsonNode.Parse(before.PrintJson(includeNotes: false))!;
        JsonObject afterTree = (JsonObject)JsonNode.Parse(after.PrintJson(includeNotes: false))!;

        // The notes go on a copy, so a note written on a node does not change a later comparison of the node above it.
        JsonObject noted = (JsonObject)afterTree.DeepClone();
        int kept = 0;
        foreach ((string path, string note) in before.Notes)
        {
            if (
                RuleTree.TryLocate(beforeTree, path, out JsonObject? was, out _, out _)
                && RuleTree.TryLocate(afterTree, path, out JsonObject? now, out _, out _)
                && JsonNode.DeepEquals(was, now)
                && RuleTree.TryLocate(noted, path, out JsonObject? target, out _, out _)
            )
            {
                target["note"] = note;
                kept++;
            }
        }

        if (kept == 0)
        {
            return new NotedRewrite(after, total);
        }

        CompilationResult<StubAssignment> compiled = RuleConverter
            .CreateCompiler(settings ?? ToolSettings.Default)
            .CompileAs(noted.ToJsonString(), RuleFormat.Json);

        // The notes never change the tree, so this compiles; if it does not, the rewrite stays and every note is dropped.
        return compiled.Rule is { } rule ? new NotedRewrite(rule, total - kept) : new NotedRewrite(after, total);
    }

    /// <summary>Gets the name of a rewrite as the library calls it, such as <c>ToNnf</c>.</summary>
    /// <param name="kind">The rewrite.</param>
    /// <returns>The name.</returns>
    public static string NameOf(RuleRewriteKind kind)
    {
        return kind switch
        {
            RuleRewriteKind.Simplify => "Simplify",
            RuleRewriteKind.Canonicalize => "Canonicalize",
            RuleRewriteKind.NegationNormalForm => "ToNnf",
            RuleRewriteKind.ConjunctiveNormalForm => "ToCnf",
            RuleRewriteKind.DisjunctiveNormalForm => "ToDnf",
            RuleRewriteKind.Compress => "CompressToDerived",
            RuleRewriteKind.ExpandToPrimitives => "ExpandToPrimitives",
            RuleRewriteKind.OptimizeOrder => "OptimizeOrder",
            _ => kind.ToString(),
        };
    }
}
