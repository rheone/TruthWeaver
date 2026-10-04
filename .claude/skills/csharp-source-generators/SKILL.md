---
name: csharp-source-generators
description: Reference for authoring Roslyn C# source generators — ISourceGenerator (the original API) and IIncrementalGenerator (the modern, pipeline-based API), GeneratorExecutionContext vs. IncrementalGeneratorInitializationContext, ISyntaxReceiver/ISyntaxContextReceiver, SyntaxValueProvider/IncrementalValuesProvider pipeline stages including ForAttributeWithMetadataName, emitting source with AddSource/RegisterSourceOutput, reporting Diagnostics from a generator, incremental caching and equatable intermediate models, generator project setup (OutputItemType="Analyzer", IsRoslynComponent, EnforceExtendedAnalyzerRules), packaging a generator in a NuGet package, interceptors, and reading/emitting generic type parameters and constraints in generated code — from the pre-.NET-5-SDK T4/external-codegen era through .NET 10's AddEmbeddedAttributeDefinition, organized by .NET SDK/Roslyn package version rather than C# language version. Use when writing, reviewing, or debugging a Roslyn source generator, choosing between ISourceGenerator and IIncrementalGenerator, building an incremental pipeline, diagnosing why a generator's caching isn't working, reporting a diagnostic from generated code, setting up or packaging a generator project, or unit-testing a generator's output. Covers the pre-.NET-5 workaround pattern through the current .NET 11 release candidate.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Source Generators

**Version axis note:** source generators are gated by the
**.NET SDK / Roslyn (`Microsoft.CodeAnalysis`) package version** at *build* time, not by the
consuming project's C# `LangVersion`. `ISourceGenerator` and `IIncrementalGenerator` are compiler
extension points, versioned with the compiler host that loads them — a project can target an old
`LangVersion` and still use a generator built against a recent Roslyn package, as long as the SDK
building it is new enough. Reference files below are therefore named and ordered by .NET SDK
version (`net5-`, `net6-`, ...), each tied to the Roslyn package version it shipped with, not by
C# language version.

One throughline across every tier: a generator inspects the compilation being built and calls
`AddSource` (however it's ultimately exposed) to add new C# source that compiles alongside the
user's own code — the mechanics of *how* a generator is invoked and *how much re-computation it
does per edit* are what change between tiers; `AddSource`'s basic contract (a hint name plus a
string of source) does not.

## Quick start (works everywhere, .NET 6 SDK+)

```csharp
[Generator]
public class GreeterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node);

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

## Pick your reference file

Each reference file states its own fallback, so pick the highest tier your minimum-supported SDK
needs and it points you downward as required.

| Target SDK | Roslyn package | Reference file |
| --- | --- | --- |
| Below .NET 5 SDK | — (no generator API) | [references/pre-net5-external-codegen.md](references/pre-net5-external-codegen.md) — T4 templates or an external, MSBuild-driven codegen tool |
| .NET 5 SDK+ | Roslyn 3.8+ | [references/net5-isourcegenerator.md](references/net5-isourcegenerator.md) — `ISourceGenerator`, `GeneratorExecutionContext`, `ISyntaxReceiver`/`ISyntaxContextReceiver`, project setup |
| .NET 6 SDK+ | Roslyn 4.0+ | [references/net6-iincrementalgenerator.md](references/net6-iincrementalgenerator.md) — `IIncrementalGenerator`, the pipeline API, `RegisterSourceOutput`; the current baseline for new generators |
| .NET 7 SDK+ | Roslyn 4.3+ | [references/net7-forattributewithmetadataname.md](references/net7-forattributewithmetadataname.md) — `ForAttributeWithMetadataName`, `WithTrackingName` |
| .NET 8 SDK+ | Roslyn 4.8+ | [references/net8-interceptors-preview.md](references/net8-interceptors-preview.md) — interceptors, preview-only, opt-in via `InterceptorsPreviewNamespaces` |
| .NET 9 SDK+ | Roslyn 4.12+ | [references/net9-interceptors-stable.md](references/net9-interceptors-stable.md) — interceptors stabilize; `GetInterceptableLocation` replaces the raw file/line/column form |
| .NET 10 SDK+ | Roslyn 4.14+ | [references/net10-embedded-attribute-definitions.md](references/net10-embedded-attribute-definitions.md) — `AddEmbeddedAttributeDefinition` fixes marker-attribute duplication across `InternalsVisibleTo`-linked projects |

**.NET 11 / C# 15 note:** verified via search this session — no generator-API-specific change was
found for the .NET 11 SDK as of its RC (September 2026). C# 15 language features shipping alongside
it (native unions, collection-expression arguments) are language surface, not generator-API surface
— use the .NET 10 tier's APIs unchanged.

## Specialized patterns

- [specialized/testing-a-source-generator.md](specialized/testing-a-source-generator.md) — driving a generator with `CSharpGeneratorDriver`, asserting on generated source and diagnostics, snapshot testing, asserting the incremental pipeline actually caches
- [specialized/incremental-pipeline-and-equatable-models.md](specialized/incremental-pipeline-and-equatable-models.md) — why cache correctness depends on value equality, the `ISymbol`/`SyntaxNode`-leaking-into-cached-state mistake, `EquatableArray<T>`, `WithComparer`
- [specialized/diagnostics-from-a-generator.md](specialized/diagnostics-from-a-generator.md) — `DiagnosticDescriptor`, severity, reporting from `ISourceGenerator` and `IIncrementalGenerator`, mapping a diagnostic back to the user's source `Location`
- [specialized/generator-project-setup-and-packaging.md](specialized/generator-project-setup-and-packaging.md) — `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, multi-targeting a generator across Roslyn versions, packaging a generator inside a NuGet package alongside consumer code
- [specialized/generics-in-generated-code.md](specialized/generics-in-generated-code.md) — reading a type's type parameters/constraints off its symbol, emitting a generic partial type or method, de-duplicating closed generic instantiations with `SymbolEqualityComparer`
