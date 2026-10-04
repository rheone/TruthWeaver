# C# Source Generators

Helps you write, review, or debug a Roslyn source generator, choosing between `ISourceGenerator`
and `IIncrementalGenerator`, building an incremental pipeline, reporting diagnostics, and setting up
or packaging the generator project. Organized by .NET SDK/Roslyn version rather than C# language
version, since a generator's API surface is gated by the compiler host that loads it.

## When to reach for it

- Choosing between the legacy `ISourceGenerator` API and the modern `IIncrementalGenerator` pipeline
- Building an incremental pipeline with `ForAttributeWithMetadataName` and `RegisterSourceOutput`
- Diagnosing why a generator's incremental caching isn't working (unnecessary re-runs on every keystroke)
- Reporting a `Diagnostic` from a generator, mapped back to the user's source
- Setting up or packaging a generator project for NuGet distribution

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or debugging a
Roslyn source generator.

## What it covers

| Topic | Reference |
| --- | --- |
| No generator API: T4 templates or external codegen | [references/pre-net5-external-codegen.md](references/pre-net5-external-codegen.md) |
| `ISourceGenerator`, `GeneratorExecutionContext`, `ISyntaxReceiver` | [references/net5-isourcegenerator.md](references/net5-isourcegenerator.md) |
| `IIncrementalGenerator` and the pipeline API | [references/net6-iincrementalgenerator.md](references/net6-iincrementalgenerator.md) |
| `ForAttributeWithMetadataName`, `WithTrackingName` | [references/net7-forattributewithmetadataname.md](references/net7-forattributewithmetadataname.md) |
| Interceptors (preview, opt-in) | [references/net8-interceptors-preview.md](references/net8-interceptors-preview.md) |
| Interceptors (stable) | [references/net9-interceptors-stable.md](references/net9-interceptors-stable.md) |
| `AddEmbeddedAttributeDefinition` | [references/net10-embedded-attribute-definitions.md](references/net10-embedded-attribute-definitions.md) |

## Example prompts

- "Should I write this as an `ISourceGenerator` or an `IIncrementalGenerator`?"
- "Why does my incremental generator re-run its whole pipeline on every keystroke?"
- "Report a diagnostic from my generator that points back to the attribute the user wrote."
