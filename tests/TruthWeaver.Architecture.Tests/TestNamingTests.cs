namespace TruthWeaver.Architecture.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using Xunit.v3;

/// <summary>
/// Enforces the test-name convention <c>{MemberUnderTest}_{Scenario}_{Expectation}_Test</c> across every test project in
/// <c>tests/</c> and <c>samples/*.Tests</c>. Methods that predate the convention are listed in
/// <see cref="TestNamingBaseline"/>, which may only shrink.
/// </summary>
public sealed partial class TestNamingTests
{
    private const string ExpectedShape = "{MemberUnderTest}_{Scenario}_{Expectation}_Test";

    /// <summary>The assembly names of the test projects under <c>tests/</c> and <c>samples/</c>.</summary>
    private static readonly string[] TestAssemblyNames =
    [
        "TruthWeaver.Tests",
        "TruthWeaver.Abstractions.Tests",
        "TruthWeaver.DataSources.Json.Tests",
        "TruthWeaver.Generators.Tests",
        "TruthWeaver.Predicates.Tests",
        "TruthWeaver.Testing.Tests",
        "TruthWeaver.Yaml.Tests",
        "TruthWeaver.Architecture.Tests",
        "Consumer.Tests",
        "DataSource.Tests",
        "PredicateLibrary.Tests",
    ];

    /// <summary>Every test method follows the naming convention unless the baseline lists it.</summary>
    [Fact]
    public void TestMethods_Names_FollowTheConventionOrAreBaselined_Test()
    {
        List<string> offenders = [];
        foreach (string id in ViolatingTests())
        {
            if (!TestNamingBaseline.Methods.Contains(id))
            {
                offenders.Add($"{id} (expected {ExpectedShape})");
            }
        }

        Assert.True(offenders.Count == 0, "Rename these tests:\n" + string.Join('\n', offenders));
    }

    /// <summary>A baseline entry names a test that still breaks the convention, so a fixed entry must be removed.</summary>
    [Fact]
    public void TestNamingBaseline_Entries_AllStillViolateTheConvention_Test()
    {
        HashSet<string> violating = [.. ViolatingTests()];
        string[] stale = [.. TestNamingBaseline.Methods.Where(m => !violating.Contains(m)).Order(StringComparer.Ordinal)];

        Assert.True(stale.Length == 0, "Remove these now-clean entries from TestNamingBaseline:\n" + string.Join('\n', stale));
    }

    /// <summary>A conforming name has at least member, scenario and expectation segments, then the <c>_Test</c> suffix.</summary>
    [GeneratedRegex(@"^[^_]+(_[^_]+){2,}_Test$", RegexOptions.CultureInvariant)]
    private static partial Regex ConventionalName();

    /// <summary>Lists <c>Namespace.Type.Method</c> for each test method whose name breaks the convention.</summary>
    private static IEnumerable<string> ViolatingTests()
    {
        foreach (string name in TestAssemblyNames)
        {
            Assembly assembly = Assembly.Load(name);
            foreach (Type type in assembly.GetTypes())
            {
                foreach (
                    MethodInfo method in type.GetMethods(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                            | BindingFlags.DeclaredOnly
                    )
                )
                {
                    // [Fact], [Theory] and any attribute derived from them mark a test; helpers and theory data do not.
                    if (method.IsDefined(typeof(FactAttribute), inherit: true) && !ConventionalName().IsMatch(method.Name))
                    {
                        yield return $"{type.FullName}.{method.Name}";
                    }
                }
            }
        }
    }
}
