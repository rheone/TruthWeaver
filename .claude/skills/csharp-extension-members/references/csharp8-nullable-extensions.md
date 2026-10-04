# Nullable Reference Types on Extension Methods (C# 8.0+ / .NET Core 3.0+)

C# 8 (.NET Core 3.0, 2019) added nullable reference type annotations (`string?` vs. `string`) as
compiler-checked hints. They apply to extension method receivers and parameters exactly like any
other method parameter — there's no extension-specific mechanic — but a few patterns come up
often enough to call out.

## Enable it

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

## Guard-style extension methods take a nullable receiver

```csharp
using System.Diagnostics.CodeAnalysis;

public static class ValidationExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) =>
        string.IsNullOrEmpty(value);

    public static T ThrowIfNull<T>([NotNull] this T? value, string? paramName = null) where T : class =>
        value ?? throw new ArgumentNullException(paramName);
}
```

`[NotNullWhen]` / `[NotNull]` (from `System.Diagnostics.CodeAnalysis`) let the compiler narrow the
caller's nullability after the call:

```csharp
string? name = GetName();
name.ThrowIfNull(nameof(name));
Console.WriteLine(name.Length); // no CS8602 — compiler knows name is not-null here
```

## Nullable annotations don't change runtime behavior

A classic (pre-8) extension method with `this string value` compiles fine and can still be called
on a `null` reference without an immediate `NullReferenceException` — extension methods are just
static method calls under the hood, so `((string)null!).IsNullOrBlank()` executes until the body
actually dereferences the receiver. Nullable annotations make a `null` receiver an explicit,
opt-in compiler *warning* (CS8604 at the call site, or CS8625 in the body) instead of silent —
they are a static-analysis layer, not a runtime null-check.

## Fallback

Everything here is ordinary C# 8 nullable-annotation syntax layered onto
[csharp3-extension-methods.md](csharp3-extension-methods.md)'s classic form; drop the `?` and the
attributes and the code is valid back to C# 3.0.
