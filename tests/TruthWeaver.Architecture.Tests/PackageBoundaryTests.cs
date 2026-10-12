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
    private static readonly Assembly ToolCore = typeof(TruthWeaver.Tool.Core.RuleLoader).Assembly;

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

    /// <summary>Abstractions has no dependency on any other TruthWeaver assembly.</summary>
    [Fact]
    public void Abstractions_Dependencies_ExcludeOtherTruthWeaverAssemblies_Test()
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

    /// <summary>Predicates depends on Abstractions alone.</summary>
    [Fact]
    public void Predicates_Dependencies_AreAbstractionsOnly_Test()
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
    public void Testing_Dependencies_ExcludeYamlPredicatesAndJsonDataSources_Test()
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

    /// <summary>Core has no dependency on the Yaml, Predicates or Testing packages.</summary>
    [Fact]
    public void Core_Dependencies_ExcludeYamlPredicatesAndTesting_Test()
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
    public void Core_Dependencies_ExcludeJsonPathAssembly_Test()
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
    public void JsonDataSources_Dependencies_AreAbstractionsOnly_Test()
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
    public void Yaml_References_JsonDataSourcePackageOnly_Test()
    {
        string[] referenced = [.. Yaml.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];

        Assert.Contains("TruthWeaver.DataSources.Json", referenced);
        Assert.DoesNotContain("Meziantou.Framework.JsonPath", referenced);
        Assert.DoesNotContain("JsonPath.Net", referenced);
        Assert.DoesNotContain("Json.More", referenced);
    }

    /// <summary>Yaml has no dependency on the Predicates or Testing packages.</summary>
    [Fact]
    public void Yaml_Dependencies_ExcludePredicatesAndTesting_Test()
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
    public void Generators_Dependencies_ExcludeTruthWeaverAssemblies_Test()
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

    /// <summary>
    /// <c>TruthWeaver.Tool.Core</c> is the UI-free core of the command line and terminal UI. It references only TruthWeaver
    /// assemblies and the libraries they bring, never a command-line or terminal UI library, and no test support.
    /// </summary>
    [Fact]
    public void ToolCore_References_ExcludeUiCommandLineAndTestingAssemblies_Test()
    {
        string[] forbidden = ["System.CommandLine", "Terminal.Gui", "TruthWeaver.Testing", "TruthWeaver.Predicates"];

        string[] referenced = [.. ToolCore.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];

        Assert.Contains("TruthWeaver", referenced);
        Assert.DoesNotContain(referenced, name => forbidden.Contains(name, StringComparer.Ordinal));
    }

    /// <summary>
    /// The command line is the only project with <c>System.CommandLine</c>, and it builds on the tool core. No package and
    /// not the tool core references the command line.
    /// </summary>
    [Fact]
    public void Tool_References_ToolCoreAndCommandLineOnlyThere_Test()
    {
        // Loaded by name: the command line is an executable, and its types are internal.
        Assembly tool = Assembly.Load("TruthWeaver.Tool");

        string[] referenced = [.. tool.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];

        Assert.Contains("TruthWeaver.Tool.Core", referenced);
        Assert.Contains("System.CommandLine", referenced);
        foreach (Assembly assembly in new[] { Abstractions, Core, Yaml, JsonDataSources, Predicates, Testing, ToolCore })
        {
            string[] names = [.. assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty)];
            Assert.DoesNotContain("TruthWeaver.Tool", names);
            Assert.DoesNotContain("System.CommandLine", names);
        }
    }

    /// <summary>No shipping package depends on the tool core: the tools build on the packages, never the reverse.</summary>
    [Fact]
    public void ShippedPackages_References_ExcludeToolCore_Test()
    {
        foreach (Assembly assembly in new[] { Abstractions, Core, Yaml, JsonDataSources, Predicates, Testing })
        {
            Assert.DoesNotContain(
                ToolCore.GetName().Name,
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
