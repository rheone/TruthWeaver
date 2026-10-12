namespace TruthWeaver.Evaluation;

/// <summary>The options of <see cref="CompiledRule{TContext}.OptimizeOrder"/>.</summary>
/// <param name="MeasuredCosts">
/// An opt-in override of per-predicate relative cost, keyed by predicate name (matched case-insensitively, as rule text
/// matches predicate names). A name present here overrides <see cref="Abstractions.PredicateSchema.Cost"/> for that
/// predicate, for this call only; a predicate not present here falls back to its schema's <c>Cost</c> hint (or the
/// neutral baseline of <c>0</c> when that is also unset). <see langword="null"/> (the default) uses only the static
/// <c>Cost</c> hints, so the same rule and registry give the same order on every call.
/// </param>
public sealed record OrderOptimizationOptions(IReadOnlyDictionary<string, double>? MeasuredCosts = null)
{
    /// <summary>Gets the default options: no measured override, so only the static <c>Cost</c> hints decide the order.</summary>
    public static OrderOptimizationOptions Default { get; } = new();
}
