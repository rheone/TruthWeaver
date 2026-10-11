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
            .NotHaveDependencyOnAny([
                .. CoreNamespaces,
                "TruthWeaver.Yaml",
                "TruthWeaver.DataSources",
                "TruthWeaver.Predicates",
                "TruthWeaver.Testing",
            ])
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

    /// <summary>
    /// ADR-0004 (amended 2026-10-09): <c>TruthWeaver.Testing</c> now takes a project reference to
    /// <c>TruthWeaver</c> (the compiler/analyzer package) so rule-level testing tools (an equivalence
    /// assertion, a predicate harness, a public rule fuzzer) can live beside <c>DecisionAssertions</c>.
    /// It still never depends on the YAML package, the ready-made predicates, or the JSON data source
    /// package, each of which is optional and orthogonal to testing support.
    /// </summary>
    [Fact]
    public void Testing_depends_on_truthweaver_and_abstractions_alone_never_yaml_predicates_or_json_data_sources()
    {
        TestResult result = Types
            .InAssembly(Testing)
            .That()
            .ResideInNamespace("TruthWeaver.Testing")
            .ShouldNot()
            .HaveDependencyOnAny("TruthWeaver.Yaml", "TruthWeaver.Predicates", "TruthWeaver.DataSources")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Core_has_no_dependency_on_yaml_predicates_or_testing()
    {
        TestResult result = Types
            .InAssembly(Core)
            .Should()
            .NotHaveDependencyOnAny(
                "TruthWeaver.Yaml",
                "TruthWeaver.DataSources",
                "TruthWeaver.Predicates",
                "TruthWeaver.Testing"
            )
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
        string[] jsonAssemblies = ["Meziantou.Framework.JsonPath", "JsonPath.Net", "Json.More"];

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

    /// <summary>
    /// ADR-0006 decision 14: the YAML data source reuses the JSON package's query engine, so the YAML package references that
    /// package and takes no JSONPath library of its own.
    /// </summary>
    [Fact]
    public void Yaml_references_the_json_data_source_package_but_not_the_jsonpath_library_directly()
    {
        string[] referenced = [.. Yaml.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];

        Assert.Contains("TruthWeaver.DataSources.Json", referenced);
        Assert.DoesNotContain("Meziantou.Framework.JsonPath", referenced);
        Assert.DoesNotContain("JsonPath.Net", referenced);
        Assert.DoesNotContain("Json.More", referenced);
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

    /// <summary>
    /// <c>TruthWeaver.Generators</c> runs inside the compiler, so it references no TruthWeaver assembly. The code it
    /// generates names the abstractions and the registry builder in the consuming project instead. No other package
    /// references the generator.
    /// </summary>
    [Fact]
    public void Generators_references_no_truthweaver_assembly_and_no_package_references_it()
    {
        // Loaded by name: the generator types derive from Roslyn types, which this test process does not load.
        Assembly generators = Assembly.Load("TruthWeaver.Generators");

        string[] referenced = [.. generators.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];

        Assert.DoesNotContain(referenced, name => name.StartsWith("TruthWeaver", StringComparison.Ordinal));
        foreach (Assembly assembly in new[] { Abstractions, Core, Yaml, JsonDataSources, Predicates, Testing })
        {
            Assert.DoesNotContain(
                "TruthWeaver.Generators",
                assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)
            );
        }
    }

    private static string Describe(TestResult result)
    {
        return result.IsSuccessful
            ? string.Empty
            : "Offending types: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []);
    }
}
