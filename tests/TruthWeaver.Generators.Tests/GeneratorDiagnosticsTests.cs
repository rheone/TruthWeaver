namespace TruthWeaver.Generators.Tests;

using System.Reflection;
using Microsoft.CodeAnalysis;

/// <summary>
/// Guards <see cref="GeneratorDiagnostics"/>'s own invariant: every descriptor's rule id is unique. Nothing else
/// catches two descriptors sharing one id - <c>RS2000</c> (analyzer release tracking) only checks that each id is
/// listed in <c>AnalyzerReleases.Unshipped.md</c>, not that ids are distinct from each other, and these are ad-hoc
/// descriptors, not a <c>DiagnosticAnalyzer</c>, so <c>RS1019</c> does not apply either. Confirmed missing by
/// an incident: two concurrently-developed tickets each independently claimed <c>TWG008</c> for a different
/// diagnostic, and only a lucky textual merge conflict surfaced it.
/// </summary>
public sealed class GeneratorDiagnosticsTests
{
    /// <summary>Every <see cref="DiagnosticDescriptor"/> field on <see cref="GeneratorDiagnostics"/> has its own id.</summary>
    [Fact]
    public void Descriptors_EveryId_IsUnique_Test()
    {
        // GeneratorDiagnostics is internal to TruthWeaver.Generators, so it is reached by name rather than by
        // typeof(...), the same way a test checks an invariant of a type it should not need InternalsVisibleTo for.
        Type generatorDiagnostics = typeof(PredicateRegistrationGenerator).Assembly.GetType(
            "TruthWeaver.Generators.GeneratorDiagnostics",
            throwOnError: true
        )!;
        FieldInfo[] fields = generatorDiagnostics.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        List<DiagnosticDescriptor> descriptors =
        [
            .. fields
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .Select(f => (DiagnosticDescriptor)f.GetValue(null)!),
        ];

        IEnumerable<string> duplicateIds = descriptors
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.True(descriptors.Count > 0, "No DiagnosticDescriptor fields were found; this test would pass vacuously.");
        Assert.Empty(duplicateIds);
    }
}
