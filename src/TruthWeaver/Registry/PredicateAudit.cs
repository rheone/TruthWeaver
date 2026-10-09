namespace TruthWeaver.Registry;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// Audits a predicate registry against the rules that use it, so a host can retire predicates that no stored rule
/// references any more.
/// </summary>
public static class PredicateAudit
{
    /// <summary>
    /// Finds the registered predicates that no rule in <paramref name="rules"/> references. Names match
    /// case-insensitively, the same normalization that term identity uses. The audit reads
    /// <see cref="CompiledRule{TContext}.PredicateNames"/> only and compiles no rule text.
    /// </summary>
    /// <typeparam name="TContext">The application context type the registry and rules share.</typeparam>
    /// <param name="registry">The registry whose predicates are audited.</param>
    /// <param name="rules">The compiled rules to check. An empty set leaves every schema unused.</param>
    /// <returns>The schemas of the unused predicates, in no guaranteed order. Empty when the registry has none.</returns>
    public static IReadOnlyList<PredicateSchema> FindUnused<TContext>(
        PredicateRegistry<TContext> registry,
        IEnumerable<CompiledRule<TContext>> rules
    )
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(rules);

        HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);
        foreach (CompiledRule<TContext> rule in rules)
        {
            used.UnionWith(rule.PredicateNames);
        }

        return [.. registry.Schemas.Where(schema => !used.Contains(schema.Name))];
    }
}
