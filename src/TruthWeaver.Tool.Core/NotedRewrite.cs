namespace TruthWeaver.Tool.Core;

using TruthWeaver.Evaluation;

/// <summary>A rewritten rule with the notes that stayed on it (see <see cref="RewritePlayground.CarryNotes"/>).</summary>
/// <param name="Rule">The rule after the rewrite, with the notes that stayed.</param>
/// <param name="DroppedNotes">The number of notes of the rule before the rewrite that are not on <paramref name="Rule"/>.</param>
public sealed record NotedRewrite(CompiledRule<StubAssignment> Rule, int DroppedNotes);
