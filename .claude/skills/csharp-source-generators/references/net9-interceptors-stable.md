# .NET 9 SDK — Interceptors Stabilize; `GetInterceptableLocation` (November 2024)

Interceptors moved from opt-in preview to a stable, always-available compiler feature in the
.NET 9.0.2xx SDK band, and the semantic model gained `GetInterceptableLocation`, replacing the
[previous tier](net8-interceptors-preview.md)'s raw `(filePath, line, column)` triple with an
opaque, version-resilient encoded location. A generator calls
`semanticModel.GetInterceptableLocation(invocationSyntax)` to obtain an `InterceptableLocation`
value, whose `.Data` (a base64-encoded, checksum-backed string) becomes the constructor argument
for a new, single-argument `[InterceptsLocation(string)]` overload.

## Syntax

```csharp
InterceptableLocation location = semanticModel.GetInterceptableLocation(invocationSyntax)!;

string source = $$"""
    namespace MyApp.Generators
    {
        file static class Interceptors
        {
            [System.Runtime.CompilerServices.InterceptsLocation({{location.Version}}, "{{location.Data}}")]
            // {{location.GetDisplayLocation()}} -- human-readable comment only, not load-bearing
            public static T Deserialize<T>(this string json) => /* generated fast path */ default!;
        }
    }
    """;
```

## Basic use case: no more opt-in property, no more line/column fragility

The consuming project needs no `InterceptorsPreviewNamespaces` property at this tier — interceptors
work by default. The encoded `InterceptableLocation.Data` is checksum-based rather than a raw
position, so an unrelated edit elsewhere in the file that shifts line numbers no longer silently
breaks a previously-emitted interceptor the way the raw triple did.

## Advanced use case: `GetDisplayLocation()` for generator-authoring diagnostics only

`InterceptableLocation.GetDisplayLocation()` embeds the call site's full file path as a
human-readable comment for developers debugging the generator's own output — it is documented as
unsuitable for anything checked into source control or embedded in build output that needs
reproducible/deterministic builds across machines, since two machines building from the same commit
can have the project at a different absolute path. Keep it to a `//` comment, as shown above; never
make it part of an identifier or a value the generated code executes.

## Requirements and restrictions

- Requires the .NET 9 SDK (9.0.2xx band or later) at build time for the stabilized, opt-in-free
  form; a generator that must also support .NET 8 consumers needs to multi-target and branch
  between the two `[InterceptsLocation]` overloads (see
  [specialized/generator-project-setup-and-packaging.md](../specialized/generator-project-setup-and-packaging.md)
  for the general multi-SDK-targeting pattern).
- `GetInterceptableLocation` is called on the `SemanticModel`, not the syntax node directly — it
  needs semantic context to resolve which overload of an invocation is actually being intercepted.

## Fallback

Below the .NET 9 SDK, use
[the .NET 8 preview tier](net8-interceptors-preview.md)'s `(string filePath, int line, int column)`
constructor and the `InterceptorsPreviewNamespaces` opt-in property instead — functionally
equivalent, but line/column-fragile and requiring the consumer to opt in explicitly.
