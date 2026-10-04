# .NET 5 SDK / Roslyn 3.8 — `ISourceGenerator` (November 2020)

The original source-generator API, shipped in the .NET 5 SDK alongside C# 9.0. A generator is a
class implementing `ISourceGenerator`, marked `[Generator]`, that the compiler discovers as an
analyzer-like plugin, runs during compilation, and lets add new syntax trees to the compilation
before `csc` emits IL. `ISourceGenerator` re-runs its entire `Execute` method on every
compilation pass — including on every keystroke inside an IDE that has background analysis on —
which is the exact problem the next tier's `IIncrementalGenerator` was built to fix. Microsoft's
current guidance is to write new generators against `IIncrementalGenerator`; this tier exists for
maintaining or reading an existing `ISourceGenerator`-based generator, or when a project's minimum
supported SDK predates .NET 6.

## Project setup

A generator lives in its own class library, targeting `netstandard2.0` (the lowest common
denominator the compiler host can load), referenced from the *consuming* project as an analyzer,
not as an ordinary assembly reference:

```xml
<!-- MyApp.Generators.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IncludeBuildOutput>false</IncludeBuildOutput>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="3.8.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

```xml
<!-- MyApp.csproj, the consuming project -->
<ItemGroup>
  <ProjectReference Include="..\MyApp.Generators\MyApp.Generators.csproj"
                     OutputItemType="Analyzer"
                     ReferenceOutputAssembly="false" />
</ItemGroup>
```

`OutputItemType="Analyzer"` is what makes MSBuild load the referenced project's output as a
compiler plugin instead of linking it as a normal dependency; `ReferenceOutputAssembly="false"`
keeps the generator's own DLL and its `Microsoft.CodeAnalysis` dependency out of the consuming
project's output.

## Syntax

```csharp
[Generator]
public class GreeterGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassDeclarationReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ClassDeclarationReceiver receiver)
        {
            return;
        }

        foreach (INamedTypeSymbol type in receiver.CandidateTypes)
        {
            string source = $$"""
                namespace {{type.ContainingNamespace}};
                partial class {{type.Name}}
                {
                    public string Greet() => "Hello from generated code";
                }
                """;

            context.AddSource($"{type.Name}.g.cs", source);
        }
    }
}
```

## Basic use case: `ISyntaxReceiver` for cheap syntactic filtering

`Initialize` runs once per compilation and is where a generator registers a syntax receiver — an
object whose `OnVisitSyntaxNode` gets called once per syntax node in every syntax tree being
compiled, letting the generator collect candidates cheaply before `Execute` does any real work:

```csharp
internal class ClassDeclarationReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration)
        {
            CandidateClasses.Add(classDeclaration);
        }
    }
}
```

`ISyntaxReceiver` only sees syntax — no semantic model, so it can't resolve an attribute's full
name, only match on its written-out syntax text. `ISyntaxContextReceiver` (used in the example
above via `RegisterForSyntaxNotifications` with a `GeneratorSyntaxContext`-accepting factory) gets
a `SemanticModel` alongside each node, so it can call `context.SemanticModel.GetDeclaredSymbol(...)`
to resolve the actual `INamedTypeSymbol` and check attributes by fully qualified name instead of
matching on written syntax text, which breaks on an aliased `using`.

## Advanced use case: emitting a generic partial type

`AddSource` takes a hint name (must be unique per generator run, typically
`{TypeName}.g.cs`) and a string of complete C# source. Nothing about `ISourceGenerator` restricts
what that source can declare — including a generic partial type whose type parameters and
constraints are read back off the target symbol:

```csharp
foreach (INamedTypeSymbol type in receiver.CandidateTypes)
{
    string typeParams = type.TypeParameters.Length == 0
        ? ""
        : $"<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>";

    string constraints = string.Join(" ", type.TypeParameters
        .Where(tp => tp.HasReferenceTypeConstraint)
        .Select(tp => $"where {tp.Name} : class"));

    string source = $$"""
        namespace {{type.ContainingNamespace}};
        partial class {{type.Name}}{{typeParams}} {{constraints}}
        {
            public override string ToString() => nameof({{type.Name}});
        }
        """;

    context.AddSource($"{type.Name}.g.cs", source);
}
```

## Requirements and restrictions

- The generator project must target `netstandard2.0` — the compiler host loads generators through
  that ABI regardless of what the consuming project targets.
- `Execute` has no built-in caching: on a large solution with an IDE's background compiler running,
  an `Execute` that does real semantic-model work on every syntax-tree visit is the direct cause of
  the "typing lag" `IIncrementalGenerator` (next tier) was introduced to fix.
- The target type the generator adds members to must be declared `partial` in the user's own code,
  or `AddSource` must emit a wholly new type — a generator cannot add members to a non-`partial`
  existing type.

## Fallback

There is no older API fallback — `ISourceGenerator` is the first tier that has one at all. Below
the .NET 5 SDK, use
[the pre-history tier](pre-net5-external-codegen.md)'s T4 templates or external build-time tooling
instead.
