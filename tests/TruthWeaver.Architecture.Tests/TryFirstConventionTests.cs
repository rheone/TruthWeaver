namespace TruthWeaver.Architecture.Tests;

using System.Reflection;
using TruthWeaver.Compilation;

/// <summary>
/// Guards the Try-first convention: every public member named <c>XOrThrow</c> has a public sibling named <c>TryX</c>,
/// so a caller can always handle the failure as a normal outcome instead of catching an exception.
/// </summary>
public sealed class TryFirstConventionTests
{
    private const string Suffix = "OrThrow";

    private static readonly Assembly[] Shipped =
    [
        typeof(TruthWeaver.Abstractions.TruthValue).Assembly,
        typeof(RuleCompiler<>).Assembly,
        typeof(TruthWeaver.Yaml.YamlRuleExtensions).Assembly,
        typeof(TruthWeaver.Predicates.StringPredicates).Assembly,
        typeof(TruthWeaver.Testing.DecisionAssertions).Assembly,
    ];

    /// <summary>Every public OrThrow method in a shipped assembly has a Try sibling on the same type.</summary>
    [Fact]
    public void ShippedAssemblies_PublicOrThrowMembers_HaveATrySibling_Test()
    {
        IEnumerable<string> offenders = Shipped.SelectMany(a => a.GetExportedTypes()).SelectMany(MissingTrySiblings);

        Assert.Empty(offenders);
    }

    /// <summary>The check reports an OrThrow method that has no Try sibling, so the guard is not vacuous.</summary>
    [Fact]
    public void MissingTrySiblings_TypeWithoutSibling_ReportsOnlyTheUnpairedMethod_Test()
    {
        List<string> offenders = [.. MissingTrySiblings(typeof(FailingFixture))];

        string offender = Assert.Single(offenders);
        Assert.Contains("LoadOrThrow", offender, StringComparison.Ordinal);
    }

    /// <summary>The check accepts an OrThrow method that has a Try sibling.</summary>
    [Fact]
    public void MissingTrySiblings_TypeWithSibling_ReportsNothing_Test()
    {
        Assert.Empty(MissingTrySiblings(typeof(PassingFixture)));
    }

    private static IEnumerable<string> MissingTrySiblings(Type type)
    {
        MethodInfo[] methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly
        );
        HashSet<string> names = [.. methods.Select(m => m.Name)];
        foreach (MethodInfo method in methods.Where(m => m.Name.EndsWith(Suffix, StringComparison.Ordinal)))
        {
            string expected = "Try" + method.Name[..^Suffix.Length];
            if (!names.Contains(expected))
            {
                yield return $"{type.FullName}.{method.Name} needs a public sibling named {expected}";
            }
        }
    }

    /// <summary>A deliberately non-conforming type: <c>LoadOrThrow</c> has no <c>TryLoad</c>.</summary>
    public static class FailingFixture
    {
        /// <summary>Gets a value or throws; the missing sibling is the point of this fixture.</summary>
        /// <returns>A value.</returns>
        public static int LoadOrThrow()
        {
            return 1;
        }

        /// <summary>Gets a value or throws; its sibling exists.</summary>
        /// <returns>A value.</returns>
        public static int ReadOrThrow()
        {
            return 1;
        }

        /// <summary>The sibling of <see cref="ReadOrThrow"/>.</summary>
        /// <param name="value">The value.</param>
        /// <returns>Always true.</returns>
        public static bool TryRead(out int value)
        {
            value = 1;
            return true;
        }
    }

    /// <summary>A conforming type.</summary>
    public static class PassingFixture
    {
        /// <summary>Gets a value or throws.</summary>
        /// <returns>A value.</returns>
        public static int ReadOrThrow()
        {
            return 1;
        }

        /// <summary>The sibling of <see cref="ReadOrThrow"/>.</summary>
        /// <param name="value">The value.</param>
        /// <returns>Always true.</returns>
        public static bool TryRead(out int value)
        {
            value = 1;
            return true;
        }
    }
}
