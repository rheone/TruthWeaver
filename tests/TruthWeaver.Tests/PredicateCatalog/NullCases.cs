namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Predicates;

/// <summary>
/// The null-selection registrations of a pair whose factories take a <see cref="NullBehavior"/> option. For a null
/// selected value the positive factory answers Unknown when registered with no option (the default) and False when
/// registered with <see cref="NullBehavior.False"/>; in both cases the twin answers <c>NOT positive</c>.
/// </summary>
/// <param name="DefaultPositive">Registers the positive factory with no <see cref="NullBehavior"/> argument.</param>
/// <param name="DefaultTwin">Registers the twin factory with no <see cref="NullBehavior"/> argument.</param>
/// <param name="FalsePositive">Registers the positive factory with <see cref="NullBehavior.False"/>.</param>
/// <param name="FalseTwin">Registers the twin factory with <see cref="NullBehavior.False"/>.</param>
internal sealed record NullCases(
    ProbeFactory DefaultPositive,
    ProbeFactory DefaultTwin,
    ProbeFactory FalsePositive,
    ProbeFactory FalseTwin
);
