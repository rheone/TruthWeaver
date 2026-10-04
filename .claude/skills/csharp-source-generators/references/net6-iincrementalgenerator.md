# .NET 6 SDK / Roslyn 4.0 — `IIncrementalGenerator` (November 2021)

The major, still-current generator API. Instead of one `Execute` method that recomputes everything
from the full compilation on every run, `IIncrementalGenerator` has you declare a *pipeline* —
a graph of transformation stages (`Select`, `Where`, `Collect`, `Combine`, ...) built once inside
`Initialize`. Each stage is a pure function over its input; the generator driver caches every
stage's output and, on a re-run triggered by an edit, only re-executes the stages whose *inputs*
actually changed, reusing cached results for everything upstream of an untouched file. This is a
by-design performance change, not an incidental one: it requires every value flowing through the
pipeline to have correct, value-based equality (see
[specialized/incremental-pipeline-and-equatable-models.md](../specialized/incremental-pipeline-and-equatable-models.md)),
because the driver decides whether to skip a stage by comparing its new output to the cached one
with `Equals`.

Microsoft's guidance since this tier shipped: write new generators against `IIncrementalGenerator`.
Reach for [the `ISourceGenerator` tier](net5-isourcegenerator.md) only to maintain an existing one
or to support a minimum SDK below .NET 6.

## Syntax

```csharp
[Generator]
public class GreeterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static c => c is not null);

        context.RegisterSourceOutput(candidates, static (spc, classDecl) =>
        {
            spc.AddSource(
                $"{classDecl.Identifier.Text}.g.cs",
                $$"""
                partial class {{classDecl.Identifier.Text}}
                {
                    public string Greet() => "Hello from generated code";
                }
                """);
        });
    }
}
```

## Basic use case: filter syntactically, resolve semantically, emit

`CreateSyntaxProvider` takes two delegates: a `predicate` that runs against every syntax node in
every file on every keystroke (so it must stay syntax-only and cheap — no semantic model access),
and a `transform` that only runs for nodes the predicate already accepted, where a `SemanticModel`
becomes available through the `GeneratorSyntaxContext`:

```csharp
IncrementalValuesProvider<INamedTypeSymbol> candidates = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
        transform: static (ctx, _) =>
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            return ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        })
    .Where(static symbol => symbol is not null)!;
```

## Advanced use case: combining a value provider with the whole compilation

`Combine` joins a values provider with a single-value provider (like `context.CompilationProvider`
or `context.AnalyzerConfigOptionsProvider`), producing a pipeline stage that has access to both —
useful when the generated code needs a project-wide fact (the assembly's default namespace, a
`.editorconfig`-supplied MSBuild property) alongside each per-type candidate:

```csharp
IncrementalValueProvider<(ImmutableArray<INamedTypeSymbol> Types, Compilation Compilation)> combined =
    candidates.Collect().Combine(context.CompilationProvider);

context.RegisterSourceOutput(combined, static (spc, pair) =>
{
    foreach (INamedTypeSymbol type in pair.Types)
    {
        bool nullableEnabled = pair.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;
        // ... emit source that varies depending on the project's nullable-context setting
    }
});
```

## Requirements and restrictions

- Every delegate passed to a pipeline stage should be `static` (a lambda that can't accidentally
  capture generator-instance state) — the driver's caching assumption is that the same inputs
  always produce the same outputs, which a captured mutable field would silently violate.
- Values flowing between stages must be equatable by value, not by reference — see
  [specialized/incremental-pipeline-and-equatable-models.md](../specialized/incremental-pipeline-and-equatable-models.md)
  for what breaks when a stage's output type is an `ISymbol` or a plain mutable class.
- `context.RegisterPostInitializationOutput` (also new at this tier) adds source that doesn't
  depend on the compilation at all — marker attributes, a shared partial-method contract — exactly
  once, regardless of how many times the pipeline itself re-runs.

## Fallback

Below the .NET 6 SDK, use
[the `ISourceGenerator` tier](net5-isourcegenerator.md)'s `ISyntaxReceiver`/`ISyntaxContextReceiver`
plus a full-recompute `Execute` method instead of a pipeline — the same `AddSource`-based output
step applies; only the entry point and the caching behavior differ.
