namespace TruthWeaver.Architecture.Tests;

/// <summary>
/// Test methods exempt from <see cref="TestNamingTests"/> because their names predate the
/// <c>{MemberUnderTest}_{Scenario}_{Expectation}_Test</c> convention. Each entry is <c>Namespace.Type.Method</c>. The list may
/// only shrink: never add a new test here, and remove an entry as soon as the method is renamed (the check fails on an entry
/// that is now clean).
/// </summary>
internal static class TestNamingBaseline
{
    // Deliberately empty: Roslynator raises RCS1259 on an empty collection initializer, so none is written.
    internal static readonly IReadOnlySet<string> Methods = new HashSet<string>(StringComparer.Ordinal);
}
