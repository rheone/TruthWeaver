namespace TruthWeaver.Tool.Core.Patching;

using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Loading;

/// <summary>One edited rule to write back into a document with <see cref="RulePatcher"/>.</summary>
/// <typeparam name="TContext">The context type of the compiled rule. The patcher uses only its tree and notes.</typeparam>
/// <param name="Path">
/// The path of the rule in the document (see <see cref="RuleLoadOptions.Path"/>), or <see langword="null"/> when the whole
/// document is the rule.
/// </param>
/// <param name="Rule">The edited rule.</param>
public sealed record RuleEdit<TContext>(string? Path, CompiledRule<TContext> Rule);
