namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// A generic factory for the externally-selected-value predicate pattern (see
/// <c>README.md</c>'s "n arguments, class-based, externally-selected value" section): a rule-text
/// literal argument and/or a <c>TContext</c>-supplied value is a key looked up live through some
/// external source (the selected value), then tested to produce the predicate's Kleene answer (which may be <see cref="TruthValue.Unknown"/>).
/// </summary>
/// <remarks>
/// This factory is the lighter-weight, complementary path to that class-based
/// <see cref="IPredicate{TContext}"/> pattern, for the sub-case where the thing doing the selecting is
/// safe to capture once, at registration time — a long-lived, thread-safe client (a cached
/// feature-flag reader, an <c>HttpClient</c>-backed lookup wrapper), exactly like any other lambda
/// predicate's selector (see <see cref="StringPredicates"/>). It is <em>not</em> appropriate when the
/// selecting dependency is scoped (a <c>DbContext</c>, a per-request <c>HttpClient</c>) and needs to be
/// re-created fresh on every evaluation — use a class-based <see cref="IPredicate{TContext}"/> for
/// that case instead, per README's documented alternative.
/// </remarks>
public static class SelectedValuePredicates
{
    /// <summary>
    /// Creates a predicate that selects a value from the context and/or rule-text arguments, then
    /// tests the selected value to produce the Kleene answer.
    /// </summary>
    /// <typeparam name="TContext">The application context type <paramref name="select"/> reads from.</typeparam>
    /// <typeparam name="TSelected">The type of the value selected before testing.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of what this predicate answers.</param>
    /// <param name="select">
    /// Performs the live selection, reading whatever it needs from <c>TContext</c> and/or the
    /// term's rule-text arguments. Captured once at registration time — see the type-level remarks
    /// for when that's appropriate.
    /// </param>
    /// <param name="test">Turns the selected value into the predicate's Kleene answer; return <see cref="TruthValue.Unknown"/> when the selected value cannot decide it.</param>
    /// <param name="arguments">The rule-text argument declarations <paramref name="select"/> needs, if any.</param>
    /// <returns>The predicate's schema and evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Create<TContext, TSelected>(
        string name,
        string label,
        string description,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TSelected>> select,
        Func<TSelected, TruthValue> test,
        params PredicateArgumentSchema[] arguments
    )
    {
        PredicateSchema schema = new(name, label, description, arguments);

        async ValueTask<TruthValue> EvaluateAsync(TContext context, PredicateArguments args, CancellationToken ct)
        {
            TSelected selected = await select(context, args, ct);
            return test(selected);
        }

        return (schema, EvaluateAsync);
    }

    /// <summary>
    /// Creates a predicate for the single-value, no-comparison-target shape: the selected value
    /// <em>is</em> the Kleene answer, with no separate <c>test</c> delegate needed (e.g. a
    /// feature-flag-style check: <c>isFeatureEnabled(flagKey: "new-checkout")</c>).
    /// </summary>
    /// <typeparam name="TContext">The application context type <paramref name="select"/> reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of what this predicate answers.</param>
    /// <param name="select">
    /// Performs the live selection, reading whatever it needs from <c>TContext</c> and/or the
    /// term's rule-text arguments, selecting the Kleene answer directly. Captured once at
    /// registration time — see the type-level remarks for when that's appropriate.
    /// </param>
    /// <param name="arguments">The rule-text argument declarations <paramref name="select"/> needs, if any.</param>
    /// <returns>The predicate's schema and evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Create<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> select,
        params PredicateArgumentSchema[] arguments
    )
    {
        PredicateSchema schema = new(name, label, description, arguments);
        return (schema, select);
    }
}
