namespace TruthWeaver.Architecture.Tests;

using System.Reflection;
using NetArchTest.Rules;
using TruthWeaver.Compilation;

/// <summary>
/// Asserts the four-package boundary rules from
/// <see href="../../docs/adr/0004-package-boundaries-and-extensibility.md">ADR-0004</see> hold at the
/// assembly-dependency level, not merely by convention — a stray <c>ProjectReference</c> or a
/// misplaced <c>using</c> that widens one of these packages' dependency surface fails the build
/// instead of waiting for a reviewer to notice.
/// </summary>
public sealed class PackageBoundaryTests
{
    private static readonly Assembly Abstractions = typeof(TruthWeaver.Abstractions.TruthValue).Assembly;
    private static readonly Assembly Core = typeof(RuleCompiler<>).Assembly;
    private static readonly Assembly Yaml = typeof(TruthWeaver.Yaml.YamlRuleExtensions).Assembly;
    private static readonly Assembly JsonDataSources = typeof(TruthWeaver.DataSources.Json.JsonDataSource).Assembly;
    private static readonly Assembly Predicates = typeof(TruthWeaver.Predicates.StringPredicates).Assembly;
    private static readonly Assembly Testing = typeof(TruthWeaver.Testing.DecisionAssertions).Assembly;

    // NetArchTest matches a forbidden namespace as a plain string prefix, so the bare "TruthWeaver"
    // string below would also match "TruthWeaver.Abstractions" itself (a false self-dependency) —
    // every TruthWeaver (Core) namespace is spelled out instead of relying on that shared prefix.
    private static readonly string[] CoreNamespaces =
    [
        "TruthWeaver.Analysis",
        "TruthWeaver.Ast",
        "TruthWeaver.Building",
        "TruthWeaver.Compilation",
        "TruthWeaver.DependencyInjection",
        "TruthWeaver.Diagnostics",
        "TruthWeaver.Diffing",
        "TruthWeaver.Evaluation",
        "TruthWeaver.Json",
        "TruthWeaver.Logging",
        "TruthWeaver.Metrics",
        "TruthWeaver.Parsing",
        "TruthWeaver.Printing",
        "TruthWeaver.Registry",
    ];

    [Fact]
    public void Abstractions_has_no_dependency_on_any_other_truthweaver_assembly()
    {
        TestResult result = Types
            .InAssembly(Abstractions)
            .Should()
            .NotHaveDependencyOnAny([.. CoreNamespaces, "TruthWeaver.Yaml", "TruthWeaver.DataSources", "TruthWeaver.Predicates", "TruthWeaver.Testing"])
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Predicates_depends_on_abstractions_alone_never_the_parser_compiler_or_analyzer()
    {
        TestResult result = Types
            .InAssembly(Predicates)
            .That()
            .ResideInNamespace("TruthWeaver.Predicates")
            .ShouldNot()
            .HaveDependencyOnAny("TruthWeaver.Ast", "TruthWeaver.Compilation", "TruthWeaver.Evaluation", "TruthWeaver.Analysis")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Testing_depends_on_abstractions_alone_never_the_parser_compiler_or_analyzer()
    {
        TestResult result = Types
            .InAssembly(Testing)
            .That()
            .ResideInNamespace("TruthWeaver.Testing")
            .ShouldNot()
            .HaveDependencyOnAny("TruthWeaver.Ast", "TruthWeaver.Compilation", "TruthWeaver.Evaluation", "TruthWeaver.Analysis")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Core_has_no_dependency_on_yaml_predicates_or_testing()
    {
        TestResult result = Types
            .InAssembly(Core)
            .Should()
            .NotHaveDependencyOnAny("TruthWeaver.Yaml", "TruthWeaver.DataSources", "TruthWeaver.Predicates", "TruthWeaver.Testing")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    /// <summary>
    /// ADR-0006 decision 14: the core package takes no JSONPath dependency (and so none of its transitive packages); only
    /// <c>TruthWeaver.DataSources.Json</c> does.
    /// </summary>
    [Fact]
    public void Core_and_abstractions_reference_no_jsonpath_assembly()
    {
        string[] jsonAssemblies = ["JsonPath.Net", "Json.More"];

        foreach (Assembly assembly in new[] { Core, Abstractions })
        {
            string[] referenced = [.. assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];
            Assert.DoesNotContain(referenced, name => jsonAssemblies.Contains(name, StringComparer.Ordinal));
        }
    }

    /// <summary>The JSON data source package depends on the abstractions alone: never the core compiler, the YAML package or the predicates.</summary>
    [Fact]
    public void JsonDataSources_depends_on_abstractions_alone()
    {
        TestResult result = Types
            .InAssembly(JsonDataSources)
            .That()
            .ResideInNamespace("TruthWeaver.DataSources.Json")
            .ShouldNot()
            .HaveDependencyOnAny([.. CoreNamespaces, "TruthWeaver.Yaml", "TruthWeaver.Predicates", "TruthWeaver.Testing"])
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Yaml_has_no_dependency_on_predicates_or_testing()
    {
        TestResult result = Types
            .InAssembly(Yaml)
            .Should()
            .NotHaveDependencyOnAny("TruthWeaver.Predicates", "TruthWeaver.Testing")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result)
    {
        return result.IsSuccessful
            ? string.Empty
            : "Offending types: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []);
    }
}
