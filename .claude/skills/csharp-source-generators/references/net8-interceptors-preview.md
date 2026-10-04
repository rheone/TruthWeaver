# .NET 8 SDK / Roslyn 4.8 — Interceptors, Preview (November 2023)

A generator-adjacent compiler feature, not a new generator API: **interceptors** let a
source-generated method body be substituted for a specific, syntactically-located call site,
without the caller's own source being modified. A generator emits an ordinary static method
carrying `[InterceptsLocation(filePath, line, column)]`, naming the exact call site it replaces;
the compiler then routes that one call to the generated method instead of the originally-written
one. This is how ASP.NET Core's and other AOT-oriented generators rewrite a reflection-based call
into a source-generated one without needing the target method to be `partial` or the call site to
change at all.

This tier shipped as an explicit **preview-only** feature: it required opting in via an MSBuild
property, and Microsoft's own documentation cautioned that non-.NET-SDK projects should treat it as
unstable and subject to change before it stabilized — which it then did at the next tier.

## Syntax

```xml
<!-- Required in the *consuming* project to opt into interceptors at this tier -->
<PropertyGroup>
  <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);MyApp.Generators</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

```csharp
// Generator-emitted code
namespace MyApp.Generators
{
    file static class Interceptors
    {
        [InterceptsLocation("Program.cs", line: 12, column: 9)]
        public static int FastParse(this string s) => int.Parse(s); // a specialized, faster path
    }
}
```

## Basic use case: replacing a known call site with a generated fast path

A generator finds every call to a target method (say, a reflection-based serializer entry point),
records that call's file/line/column from its `Location`, and emits an interceptor for it:

```csharp
Location callLocation = invocationSyntax.GetLocation();
FileLinePositionSpan span = callLocation.GetLineSpan();

string source = $$"""
    namespace MyApp.Generators
    {
        file static class Interceptors
        {
            [System.Runtime.CompilerServices.InterceptsLocation(
                "{{span.Path}}", {{span.StartLinePosition.Line + 1}}, {{span.StartLinePosition.Character + 1}})]
            public static T Deserialize<T>(this string json) => /* generated fast path */ default!;
        }
    }
    """;
```

## Requirements and restrictions

- Requires the consuming project to opt in via `InterceptorsPreviewNamespaces` at this tier — an
  interceptor emitted without the caller opting in its namespace has no effect.
- The `(string filePath, int line, int column)` constructor is fragile: any edit that shifts the
  target call site's line or column (even an unrelated edit earlier in the same file) breaks the
  interception silently, which the next tier's `InterceptableLocation` API fixes.
- Interceptors are a compiler feature usable *by* a generator; they do not replace
  `context.AddSource`-based generation for cases where a `partial` method/type is available to
  extend directly — reach for interceptors specifically when the call site is not something the
  generator can otherwise reach (a BCL method call, a call through an interface it doesn't own).

## Fallback

Below the .NET 8 SDK, interceptors don't exist — emit a wrapper method or extension method instead
and require the consumer to call it explicitly (e.g. `json.FastDeserialize<T>()` instead of relying
on the compiler to reroute `JsonSerializer.Deserialize<T>(json)` transparently).
