namespace TruthWeaver.Evaluation;

/// <summary>The result of <see cref="CompiledRule{TContext}.SimplifyWithSteps"/>: the simplified rule and how it got there.</summary>
/// <typeparam name="TContext">The application context type of the rule.</typeparam>
/// <param name="Rule">The simplified rule. It is the same rule that <see cref="CompiledRule{TContext}.Simplify"/> returns.</param>
/// <param name="Steps">
/// The changes in the order the rewrite applied them. The list is empty when the rule is already simple.
/// </param>
public sealed record SimplifyResult<TContext>(CompiledRule<TContext> Rule, IReadOnlyList<RewriteStep> Steps);
