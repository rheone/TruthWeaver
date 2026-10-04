# .NET 10 SDK / Roslyn 4.14 — `AddEmbeddedAttributeDefinition` (November 2025)

Fixes a long-standing multi-project papercut: a generator that defines its own marker attribute
(say, `[Greetable]`) and emits its definition once per compilation via
`RegisterPostInitializationOutput` produces a working build for a single project, but breaks when
two projects in the same solution both reference the generator *and* one references the other via
`[InternalsVisibleTo]` — the compiler sees two distinct internal `Greetable` types with the same
fully-qualified name and reports `CS0436` ("type conflicts with imported type").

`IncrementalGeneratorPostInitializationContext.AddEmbeddedAttributeDefinition()` (paired with
marking the generated attribute type `[Embedded]`) tells the compiler the type is intentionally
invisible outside the *current* compilation, so the same generator running in multiple
`InternalsVisibleTo`-linked projects no longer collides.

## Syntax

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    context.RegisterPostInitializationOutput(static ctx =>
    {
        ctx.AddEmbeddedAttributeDefinition();

        ctx.AddSource("GreetableAttribute.g.cs", """
            namespace MyApp.Generators
            {
                [global::Microsoft.CodeAnalysis.Embedded]
                [System.AttributeUsage(System.AttributeTargets.Class)]
                internal sealed class GreetableAttribute : System.Attribute { }
            }
            """);
    });

    // ... rest of the pipeline, e.g. ForAttributeWithMetadataName("MyApp.Generators.GreetableAttribute", ...)
}
```

## Basic use case: marker attribute shared safely across `InternalsVisibleTo`-linked projects

Before this tier, the workaround was to ship the marker attribute from a separate, ordinarily-
referenced shared assembly instead of generating it — extra packaging, and a real assembly
reference the generator's own `netstandard2.0` output otherwise wouldn't need. With
`AddEmbeddedAttributeDefinition`, the generator keeps emitting its own marker attribute per
compilation, and the `[Embedded]` marking is what prevents the `CS0436` collision, with no separate
shared-attributes package required.

## Requirements and restrictions

- Call `AddEmbeddedAttributeDefinition()` once per `RegisterPostInitializationOutput` registration,
  before (or alongside) emitting the attribute type itself — it adds the
  `Microsoft.CodeAnalysis.EmbeddedAttribute` definition the `[Embedded]` marking depends on.
- Only apply `[Embedded]` to types meant to stay invisible outside the current project — it is not
  a general "hide this generated type" switch, specifically the fix for the marker-attribute
  duplication case.
- Requires the .NET 10 SDK (Roslyn 4.14+) at build time; a generator that must also support older
  SDKs needs to branch on whether the API is available (see
  [specialized/generator-project-setup-and-packaging.md](../specialized/generator-project-setup-and-packaging.md)).

## Fallback

Below the .NET 10 SDK, keep emitting the marker attribute via plain
`RegisterPostInitializationOutput` without `[Embedded]`/`AddEmbeddedAttributeDefinition` (from
[the `IIncrementalGenerator` tier](net6-iincrementalgenerator.md)) and accept the `CS0436` risk in
solutions with `InternalsVisibleTo`-linked projects that both reference the generator — or move the
marker attribute into a separately-referenced shared assembly instead of generating it, as a
manual workaround for the same problem.
