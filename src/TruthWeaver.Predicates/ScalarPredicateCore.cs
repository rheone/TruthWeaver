namespace TruthWeaver.Predicates;

using System.Globalization;
using TruthWeaver.Abstractions;

/// <summary>
/// The shared builders behind <see cref="NumericPredicates"/> and <see cref="ScalarPredicates"/>. Every
/// builder returns a predicate schema and a stateless evaluation delegate. Comparison, range, membership and default
/// builders answer a null selection per <see cref="NullBehavior"/>, and a <c>NotX</c> twin answers the Strong Kleene
/// complement of its positive predicate for every selection, a null one included. The null-test builder is definite.
/// </summary>
internal static class ScalarPredicateCore
{
    /// <summary>Builds a predicate that compares the selected value with one literal argument.</summary>
    /// <param name="kind">The scalar kind of the selector and the argument.</param>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short display name.</param>
    /// <param name="description">The schema description.</param>
    /// <param name="selector">Reads the value from the context.</param>
    /// <param name="nullBehavior">What a null selection answers.</param>
    /// <param name="argumentName">The rule-text argument name.</param>
    /// <param name="argumentDescription">The argument description.</param>
    /// <param name="test">Maps the three-way comparison result (selected against argument) to the positive answer.</param>
    /// <param name="negate"><see langword="true"/> for the <c>NotX</c> twin, which answers the complement of <paramref name="test"/>.</param>
    /// <typeparam name="TContext">The application context type.</typeparam>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <returns>The schema and delegate.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Compare<TContext, T>(
        ScalarKind<T> kind,
        string name,
        string label,
        string description,
        Func<TContext, T?> selector,
        NullBehavior nullBehavior,
        string argumentName,
        string argumentDescription,
        Func<int, bool> test,
        bool negate = false
    )
        where T : struct, IComparable<T>, IEquatable<T>
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, argumentDescription, kind.Kind)]
        );

        return (
            schema,
            (context, args, _) =>
            {
                T target = kind.Get(args, argumentName);
                T? selected = selector(context);
                return selected is null
                    ? PredicateResult.ForNullAsync(nullBehavior, negate)
                    : PredicateResult.FromBoolAsync(test(selected.Value.CompareTo(target)) != negate);
            }
        );
    }

    /// <summary>
    /// Builds a range predicate with inclusive bounds. <paramref name="outside"/> selects the exact complement.
    /// Reversed literal bounds are a compile-time diagnostic through the schema's
    /// <see cref="PredicateSchema.ArgumentValidator"/>. A reversed bound that is not a literal throws
    /// <see cref="ArgumentException"/> at evaluation, before the selection is read, so the authoring error shows for a
    /// null selection too. The bounds are never swapped silently.
    /// </summary>
    /// <param name="kind">The scalar kind of the selector and both bounds.</param>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short display name.</param>
    /// <param name="description">The schema description.</param>
    /// <param name="selector">Reads the value from the context.</param>
    /// <param name="nullBehavior">What a null selection answers.</param>
    /// <param name="lowerName">The lower-bound argument name.</param>
    /// <param name="upperName">The upper-bound argument name.</param>
    /// <param name="outside"><see langword="true"/> for <c>Outside</c>, <see langword="false"/> for <c>Between</c>.</param>
    /// <typeparam name="TContext">The application context type.</typeparam>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <returns>The schema and delegate.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Range<TContext, T>(
        ScalarKind<T> kind,
        string name,
        string label,
        string description,
        Func<TContext, T?> selector,
        NullBehavior nullBehavior,
        string lowerName,
        string upperName,
        bool outside
    )
        where T : struct, IComparable<T>, IEquatable<T>
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [
                new PredicateArgumentSchema(lowerName, $"The inclusive lower bound ({kind.Noun}).", kind.Kind),
                new PredicateArgumentSchema(
                    upperName,
                    $"The inclusive upper bound ({kind.Noun}). It must not be less than the lower bound.",
                    kind.Kind
                ),
            ]
        )
        {
            ArgumentValidator = args => ReversedBounds(kind, args, lowerName, upperName),
        };

        return (
            schema,
            (context, args, _) =>
            {
                T lower = kind.Get(args, lowerName);
                T upper = kind.Get(args, upperName);
                if (lower.CompareTo(upper) > 0)
                {
                    string lowerText = kind.Format?.Invoke(lower) ?? $"{lower}";
                    string upperText = kind.Format?.Invoke(upper) ?? $"{upper}";
                    throw new ArgumentException(
                        $"Predicate '{name}' has reversed bounds: '{lowerName}' ({lowerText}) is greater than '{upperName}' ({upperText}).",
                        nameof(args)
                    );
                }

                T? selected = selector(context);
                if (selected is null)
                {
                    return PredicateResult.ForNullAsync(nullBehavior, outside);
                }

                bool inRange = selected.Value.CompareTo(lower) >= 0 && selected.Value.CompareTo(upper) <= 0;
                return PredicateResult.FromBoolAsync(inRange != outside);
            }
        );
    }

    /// <summary>Builds a scalar-membership predicate over a candidate array. <paramref name="negate"/> selects <c>NotIn</c>.</summary>
    /// <param name="kind">The scalar kind of the selector and the candidates.</param>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short display name.</param>
    /// <param name="description">The schema description.</param>
    /// <param name="selector">Reads the value from the context.</param>
    /// <param name="nullBehavior">What a null selection answers.</param>
    /// <param name="argumentName">The candidate-array argument name.</param>
    /// <param name="negate"><see langword="true"/> for <c>NotIn</c>.</param>
    /// <typeparam name="TContext">The application context type.</typeparam>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <returns>The schema and delegate.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Membership<TContext, T>(
        ScalarKind<T> kind,
        string name,
        string label,
        string description,
        Func<TContext, T?> selector,
        NullBehavior nullBehavior,
        string argumentName,
        bool negate
    )
        where T : struct, IComparable<T>, IEquatable<T>
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, $"The candidate {kind.Noun} values.", kind.ArrayKind)]
        );

        return (
            schema,
            (context, args, _) =>
            {
                IReadOnlyList<T> candidates = kind.GetArray(args, argumentName);
                T? selected = selector(context);
                if (selected is null)
                {
                    return PredicateResult.ForNullAsync(nullBehavior, negate);
                }

                T value = selected.Value;
                bool found = candidates.Any(candidate => value.CompareTo(candidate) == 0);

                return PredicateResult.FromBoolAsync(found != negate);
            }
        );
    }

    /// <summary>Builds the definite null test. <paramref name="negate"/> selects <c>IsNotNull</c>.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short display name.</param>
    /// <param name="description">The schema description.</param>
    /// <param name="selector">Reads the value from the context.</param>
    /// <param name="negate"><see langword="true"/> for <c>IsNotNull</c>.</param>
    /// <typeparam name="TContext">The application context type.</typeparam>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <returns>The schema and delegate.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) NullTest<TContext, T>(string name, string label, string description, Func<TContext, T?> selector, bool negate)
        where T : struct
    {
        return (
            PredicateSchema.NoArguments(name, label, description),
            (context, _, _) => PredicateResult.FromBoolAsync((selector(context) is null) != negate)
        );
    }

    /// <summary>Builds the <c>default(T)</c> test. <paramref name="negate"/> selects <c>IsNotDefault</c>.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short display name.</param>
    /// <param name="description">The schema description.</param>
    /// <param name="selector">Reads the value from the context.</param>
    /// <param name="nullBehavior">What a null selection answers.</param>
    /// <param name="negate"><see langword="true"/> for <c>IsNotDefault</c>.</param>
    /// <typeparam name="TContext">The application context type.</typeparam>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <returns>The schema and delegate.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) DefaultTest<TContext, T>(
        string name,
        string label,
        string description,
        Func<TContext, T?> selector,
        NullBehavior nullBehavior,
        bool negate
    )
        where T : struct, IEquatable<T>
    {
        return (
            PredicateSchema.NoArguments(name, label, description),
            (context, _, _) =>
            {
                T? selected = selector(context);
                return selected is null
                    ? PredicateResult.ForNullAsync(nullBehavior, negate)
                    : PredicateResult.FromBoolAsync(selected.Value.Equals(default) != negate);
            }
        );
    }

    /// <summary>
    /// The compile-time check behind <see cref="Range{TContext, T}"/>: reports reversed bounds when both bounds are
    /// literals. A bound that is absent (its value comes from a data source) skips the check, and the evaluation-time
    /// check covers it. The problem suggests swapping the bounds but the compiler never does it.
    /// </summary>
    private static IReadOnlyList<PredicateArgumentProblem> ReversedBounds<T>(
        ScalarKind<T> kind,
        PredicateArguments args,
        string lowerName,
        string upperName
    )
        where T : struct, IComparable<T>, IEquatable<T>
    {
        if (!args.TryGetRaw(lowerName, out _) || !args.TryGetRaw(upperName, out _))
        {
            return [];
        }

        T lower = kind.Get(args, lowerName);
        T upper = kind.Get(args, upperName);
        if (lower.CompareTo(upper) <= 0)
        {
            return [];
        }

        // Invariant text, so the diagnostic reads the same whatever the compiling thread's culture is.
        string lowerText = kind.Format?.Invoke(lower) ?? string.Create(CultureInfo.InvariantCulture, $"{lower}");
        string upperText = kind.Format?.Invoke(upper) ?? string.Create(CultureInfo.InvariantCulture, $"{upper}");
        return
        [
            new PredicateArgumentProblem(
                $"reversed bounds: '{lowerName}' ({lowerText}) is greater than '{upperName}' ({upperText}).",
                $"'{lowerName}' less than or equal to '{upperName}'",
                $"'{lowerName}' is {lowerText} and '{upperName}' is {upperText}",
                $"Swap the values of '{lowerName}' and '{upperName}'."
            ),
        ];
    }
}
