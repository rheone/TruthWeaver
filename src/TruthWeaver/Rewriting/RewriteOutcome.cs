namespace TruthWeaver.Rewriting;

using TruthWeaver.Ast;

/// <summary>The result of a repeat-until-stable rewrite: the tree it ended with and whether it reached a fixed point.</summary>
/// <param name="Tree">The rewritten tree. It is a valid, equivalent tree even when <paramref name="Converged"/> is <see langword="false"/>.</param>
/// <param name="Converged">
/// <see langword="true"/> when a further pass changed nothing; <see langword="false"/> when the pass limit ended the loop
/// first, so rewriting <paramref name="Tree"/> again can still change it.
/// </param>
internal readonly record struct RewriteOutcome(Expression Tree, bool Converged);
