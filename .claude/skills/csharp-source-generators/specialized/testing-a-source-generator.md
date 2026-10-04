# Testing a Source Generator

A source generator's entire job is producing text, so testing one is centrally about testing *what
it produces and when it re-produces it*: does it emit the expected source for given input, does it
report the right diagnostics, and does its incremental pipeline actually skip work it should skip.
Assumes [net6-iincrementalgenerator.md](../references/net6-iincrementalgenerator.md) and, for the
tracking-name assertions below, [net7-forattributewithmetadataname.md](../references/net7-forattributewithmetadataname.md).

## Basic: driving a generator directly with `CSharpGeneratorDriver`

No test-only package is required for the core assertion pattern — build a `Compilation` from a
string of input source, run the generator against it with `GeneratorDriver`, and inspect the
resulting syntax trees:

```csharp
[Fact]
public void GreeterGenerator_Test_EmitsGreetMethodForAttributedClass()
{
    const string source = """
        using MyApp.Generators;

        [Greetable]
        public partial class Widget { }
        """;

    SyntaxTree inputTree = CSharpSyntaxTree.ParseText(source);
    CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Tests",
        syntaxTrees: [inputTree],
        references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(new GreeterGenerator());
    driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

    string generated = outputCompilation.SyntaxTrees
        .Single(t => t.FilePath.EndsWith("Widget.g.cs"))
        .ToString();

    Assert.Contains("public string Greet()", generated);
}
```

`RunGeneratorsAndUpdateCompilation` (not `RunGenerators`) is what generator-authoring guidance
recommends for tests, because it also returns the *updated* compilation with the generated trees
merged in — needed to then feed that compilation to the C# compiler's own diagnostics for an
end-to-end "does the generated code actually compile" check.

## Basic: asserting the generated source compiles cleanly

Generating syntactically-plausible-looking text that doesn't actually compile is the most common
generator bug class, and it's invisible to a string-equality assertion on the generated source
alone — assert on `outputCompilation.GetDiagnostics()` too:

```csharp
ImmutableArray<Diagnostic> diagnostics = outputCompilation.GetDiagnostics();

Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
```

## Advanced: snapshot testing generated output

For a generator with many candidate inputs, hand-writing a string-`Contains` assertion per test
doesn't scale; a snapshot library (e.g. one built for generator testing) captures the full set of
generated files and diagnostics for a given input once, and fails a later run if the generated text
changes without the stored snapshot being updated to match — turning "did this refactor change what
we emit" into a reviewable diff instead of a hand-maintained assertion list.

```csharp
[Fact]
public Task GreeterGenerator_Test_MatchesSnapshot()
{
    GeneratorDriver driver = CSharpGeneratorDriver.Create(new GreeterGenerator());
    driver = driver.RunGenerators(CreateCompilation(Source));

    return Verify(driver); // snapshot-testing helper; compares against a stored .verified.cs file
}
```

## Advanced: asserting the incremental pipeline actually caches

Correct *output* is necessary but not sufficient for an `IIncrementalGenerator` — a pipeline stage
that recomputes on every keystroke because its model isn't properly equatable defeats the entire
point of the API (see
[specialized/incremental-pipeline-and-equatable-models.md](incremental-pipeline-and-equatable-models.md)).
`GeneratorDriverRunResult.TrackedOutputSteps` and named steps from `WithTrackingName` let a test
assert that re-running the driver against an *unrelated* edit reuses cached output instead of
recomputing it:

```csharp
[Fact]
public void GreeterGenerator_Test_DoesNotRegenerateOnUnrelatedEdit()
{
    GeneratorDriver driver = CSharpGeneratorDriver.Create(new GreeterGenerator());

    Compilation compilation1 = CreateCompilation(Source);
    driver = driver.RunGenerators(compilation1);

    // Re-parse with a trivial change to an unrelated part of the file (e.g. a comment) --
    // the [Greetable] class itself is untouched.
    Compilation compilation2 = compilation1.ReplaceSyntaxTree(
        compilation1.SyntaxTrees.First(),
        CSharpSyntaxTree.ParseText(Source + "\n// unrelated comment"));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation2).GetRunResult();

    IncrementalGeneratorRunStep step = result.Results[0].TrackedOutputSteps
        .SelectMany(kvp => kvp.Value)
        .Single(s => s.Outputs.Any());

    Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
}
```

`IncrementalStepRunReason` values distinguish `New` (first time seen), `Unchanged` (recomputed, but
produced an equal value — the comparer ran, output was reused), `Modified` (recomputed, produced a
different value), and `Cached` (the input itself was identical, so the stage didn't even
recompute). A generator whose steps report `Modified` for edits that shouldn't have affected them
has a cache-invalidation bug worth chasing down before it ships.

## Fallback

`CSharpGeneratorDriver` and `RunGeneratorsAndUpdateCompilation` are available from the same
`Microsoft.CodeAnalysis.CSharp` package version the generator itself targets, so this testing
approach applies at every tier in this skill; `WithTrackingName`/`TrackedOutputSteps`-based
incrementality assertions specifically need
[net7-forattributewithmetadataname.md](../references/net7-forattributewithmetadataname.md)'s
tracking-name API (Roslyn 4.3+) — below that, test only the generated output and diagnostics, not
which pipeline stages re-ran.
