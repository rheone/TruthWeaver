namespace TruthWeaver.Evaluation;

/// <summary>One change that a rewrite made: the law it applied and the subtree before and after.</summary>
/// <param name="Law">The law that was applied.</param>
/// <param name="Before">The canonical text of the changed subtree before the step.</param>
/// <param name="After">The canonical text of the changed subtree after the step.</param>
public sealed record RewriteStep(RewriteLaw Law, string Before, string After)
{
    /// <summary>Returns the law, then the before and after text.</summary>
    /// <returns>The step text.</returns>
    public override string ToString()
    {
        return $"{this.Law}: {this.Before} => {this.After}";
    }
}
