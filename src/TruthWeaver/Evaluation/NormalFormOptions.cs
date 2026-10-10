namespace TruthWeaver.Evaluation;

/// <summary>
/// The options of <see cref="CompiledRule{TContext}.ToNnf"/>, <see cref="CompiledRule{TContext}.ToCnf"/> and
/// <see cref="CompiledRule{TContext}.ToDnf"/>.
/// </summary>
/// <param name="ExpandThresholds">
/// <see langword="true"/> to expand thresholds (<c>AtLeast</c>, <c>AtMost</c>, <c>Exactly</c> and the like) to
/// <c>AND</c>, <c>OR</c> and <c>NOT</c>, up to <c>CompilerOptions.MaxRewriteNodeCount</c> nodes. <see langword="false"/>
/// (the default) keeps a threshold as an atom and adds a warning that gives the growth estimate, because the expansion
/// has one group for every subset of the operands.
/// </param>
public sealed record NormalFormOptions(bool ExpandThresholds = false)
{
    /// <summary>Gets the default options: thresholds stay atoms.</summary>
    public static NormalFormOptions Default { get; } = new();
}
