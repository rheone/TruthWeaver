# Static Extension Members (C# 14+)

Static extension members are called as `Type.Member`, exactly like a real static member declared
on the type — useful for adding factory methods to sealed/third-party types, and for
approximating a `static abstract` interface member on a type you don't control.

## Basic: factory-style static extension method

```csharp
public static class GuidExtensions
{
    extension(Guid)
    {
        public static Guid NewSequential() => Guid.CreateVersion7(); // .NET 9+ API, wrapped for discoverability
    }
}

Guid id = Guid.NewSequential();
```

## Advanced: generic-constrained static member (interface you don't control)

`IParsable<T>` already defines `static abstract TryParse` — normalize it behind a friendlier
"parse or fall back to a default" call, available on every `IParsable<T>` implementation at once:

```csharp
public static class ParsableExtensions
{
    extension<T>(T) where T : IParsable<T>
    {
        public static T ParseOrDefault(string text, T fallback) =>
            T.TryParse(text, null, out var value) ? value : fallback;
    }
}
```

Used from inside another generic method, where `T` is a type parameter constrained the same way:

```csharp
static T Read<T>(string text, T fallback) where T : IParsable<T> =>
    T.ParseOrDefault(text, fallback); // extension static member resolved against the type parameter T

int port = Read("8080", fallback: 80);
```

`ParseOrDefault` becomes available on **every** `IParsable<T>` implementation (`int`, `double`,
`DateTime`, custom types) without touching any of them — the extension is declared once against
the constraint, not against each concrete type.

## Static extension properties

See [extension-properties.md](extension-properties.md#static-extension-properties).

## Fallback

Below C# 14, there's no way to call a static-shaped extension as `Type.Member`. Use a
plainly-named static helper (`GuidUtil.NewSequential()`) from
[csharp3-extension-methods.md](../references/csharp3-extension-methods.md)'s era instead — callers
just can't spell it as if it lived on `Guid` itself.
