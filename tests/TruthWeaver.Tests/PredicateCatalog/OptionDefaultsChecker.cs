namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;

/// <summary>
/// Checks the option-default conventions of the predicate catalog (see <c>docs/predicate-conventions.md</c>): an optional
/// <c>ignoreCase</c> or <c>trim</c> argument defaults to <c>false</c>, and an optional Boolean <c>include*</c> argument
/// states a default. Failures are returned, not thrown, so fixtures can prove the checks fail.
/// </summary>
/// <remarks>
/// The <c>nullBehavior</c> default is a factory parameter, not a schema field, and the twin table already checks it for
/// every pair (<see cref="NotXTwinChecker.CheckNullCaseCoverage"/>), so this checker does not repeat it.
/// </remarks>
internal static class OptionDefaultsChecker
{
    private const string ConventionsPage = "docs/predicate-conventions.md";

    /// <summary>Checks the arguments of each schema against the option-default conventions.</summary>
    /// <param name="schemas">The schemas to check, each with the key of the factory that produced it.</param>
    /// <returns>One message per violation; empty when every schema follows the conventions.</returns>
    internal static IReadOnlyList<string> Check(IEnumerable<(string Factory, PredicateSchema Schema)> schemas)
    {
        List<string> failures = [];
        foreach ((string factory, PredicateSchema schema) in schemas)
        {
            foreach (PredicateArgumentSchema argument in schema.Arguments.Where(argument => !argument.Required))
            {
                if (argument.Name is "ignoreCase" or "trim" && !DefaultsToFalse(argument))
                {
                    failures.Add(
                        $"{factory}: the optional argument '{argument.Name}' must default to false, but its default is "
                            + $"{Describe(argument.Default)}. Every case and whitespace option is opt-in. See {ConventionsPage}."
                    );
                }

                if (
                    argument.Type == LiteralKind.Boolean
                    && argument.Name.StartsWith("include", StringComparison.Ordinal)
                    && argument.Default is null
                )
                {
                    failures.Add(
                        $"{factory}: the optional Boolean argument '{argument.Name}' has no default. A range-end flag "
                            + $"states which end is inside by default. See {ConventionsPage}."
                    );
                }
            }
        }

        return failures;
    }

    private static bool DefaultsToFalse(PredicateArgumentSchema argument)
    {
        return argument.Default is { } value && value.TryAsBoolean(out bool flag) && !flag;
    }

    private static string Describe(LiteralValue? value)
    {
        return value is null ? "missing" : value.Value.ToString() ?? "unknown";
    }
}
