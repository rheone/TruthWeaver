namespace TruthWeaver.Generators.Tests.TestSupport;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TruthWeaver.Abstractions;
using TruthWeaver.Registry;

/// <summary>
/// Runs <see cref="PredicateRegistrationGenerator"/> over a source text in memory, the same way the compiler runs it
/// in a build, and returns the generator diagnostics and the compiler diagnostics of the result.
/// </summary>
internal static class GeneratorHarness
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest, DocumentationMode.Parse);

    /// <summary>Runs the generator over <paramref name="source"/>.</summary>
    /// <param name="source">The C# source of one file in the consuming project.</param>
    /// <returns>The result of the run.</returns>
    public static GeneratorRun Run(string source)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Consumer",
            [CSharpSyntaxTree.ParseText(source, ParseOptions)],
            References(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable
            )
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new PredicateRegistrationGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation output,
            out ImmutableArray<Diagnostic> generatorDiagnostics,
            TestContext.Current.CancellationToken
        );

        GeneratorDriverRunResult result = driver.GetRunResult();
        string generated = string.Join(
            "\n",
            result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString())
        );
        ImmutableArray<Diagnostic> compilerErrors =
        [
            .. output.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error),
        ];
        return new GeneratorRun(generatorDiagnostics, compilerErrors, generated);
    }

    private static IEnumerable<MetadataReference> References()
    {
        // The trusted platform assemblies are the framework assemblies of this test process, so the in-memory
        // compilation sees the same framework as a real consuming project.
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        IEnumerable<string> framework = trusted.Split(Path.PathSeparator);

        // AppContext.BaseDirectory, not Assembly.Location: the trim and AOT analyzers of this project flag Location.
        string[] truthWeaver =
        [
            Path.Combine(AppContext.BaseDirectory, typeof(PredicateSchema).Assembly.GetName().Name + ".dll"),
            Path.Combine(AppContext.BaseDirectory, typeof(PredicateRegistryBuilder<>).Assembly.GetName().Name + ".dll"),
        ];
        return framework
            .Concat(truthWeaver)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(p => MetadataReference.CreateFromFile(p));
    }
}
