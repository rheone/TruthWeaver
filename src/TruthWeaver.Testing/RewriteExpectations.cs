namespace TruthWeaver.Testing;

/// <summary>
/// The extra checks that <c>RewriteAssertions.AssertSound</c> makes on a rewrite. K3 equivalence is
/// always checked and has no flag.
/// </summary>
[Flags]
public enum RewriteExpectations
{
    /// <summary>Check K3 equivalence only.</summary>
    None = 0,

    /// <summary>The rewritten rule has no more nodes than the original, counted as <c>Metrics.NodeCount</c>.</summary>
    NeverLarger = 1,

    /// <summary>Rewriting the rewritten rule again gives a rule with the same canonical text.</summary>
    Idempotent = 2,

    /// <summary>Both extra checks. This is what <c>Simplify</c> and <c>Canonicalize</c> promise.</summary>
    All = NeverLarger | Idempotent,
}
