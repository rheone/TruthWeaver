# The Incremental Pipeline and Equatable Models, in Depth

`IIncrementalGenerator`'s entire performance case rests on one assumption the driver cannot verify
for you: that a pipeline stage's output type has correct, value-based equality. The driver decides
whether to reuse a cached downstream result by comparing the new output of an upstream stage to its
previously cached output with `Equals`; if that comparison is wrong (or is reference equality on a
type that should compare by value), the driver either recomputes work it didn't need to, or —worse—
skips work it needed to redo. Assumes
[net6-iincrementalgenerator.md](../references/net6-iincrementalgenerator.md).

## Basic: why a plain class in the pipeline defeats caching

A pipeline stage that returns an ordinary reference type gets reference equality by default — two
separately-constructed instances holding identical data compare unequal, so the driver treats every
run as a change and never actually caches anything, even though nothing relevant changed:

```csharp
// Wrong: two instances with identical Name/Greeting never compare equal.
public class GreetableType
{
    public string Name { get; init; } = "";
    public string Greeting { get; init; } = "";
}
```

```csharp
// Right: a record's synthesized Equals compares by value.
public record GreetableType(string Name, string Greeting);
```

Swapping the class for a `record` (or a `readonly struct` implementing `IEquatable<T>`) is usually
the whole fix for this specific mistake — the driver needs nothing more than correct `Equals`/
`GetHashCode` to start caching correctly.

## Basic: never let an `ISymbol` or `SyntaxNode` cross a pipeline boundary

The much more common and less obvious mistake: `ITypeSymbol`, `INamedTypeSymbol`, `SyntaxNode`, and
`Compilation` are all tied to a specific compilation snapshot. Even though some of these types
implement `Equals` in a way that *looks* value-based, holding one in a cached pipeline value pins an
entire compilation's syntax trees and symbol tables in memory, and — because a new compilation
snapshot produces new symbol instances even for semantically-unchanged code — comparisons against a
stale cached instance can spuriously report "changed" on every keystroke anywhere in the file,
silently disabling caching for that stage without an obvious symptom in the generated output itself:

```csharp
// Wrong: extracting only the symbol, not the data actually needed from it.
IncrementalValuesProvider<INamedTypeSymbol> candidates = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyApp.Generators.GreetableAttribute",
        predicate: static (n, _) => n is ClassDeclarationSyntax,
        transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
```

```csharp
// Right: extract a small, equatable model with only the data the output actually needs.
public record GreetableType(string Namespace, string Name, EquatableArray<string> TypeParameterNames);

IncrementalValuesProvider<GreetableType> candidates = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyApp.Generators.GreetableAttribute",
        predicate: static (n, _) => n is ClassDeclarationSyntax,
        transform: static (ctx, _) =>
        {
            var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
            return new GreetableType(
                symbol.ContainingNamespace.ToDisplayString(),
                symbol.Name,
                new EquatableArray<string>(symbol.TypeParameters.Select(tp => tp.Name).ToArray()));
        });
```

The rule of thumb: pull every piece of data the generator will eventually need for the *string it
emits* out of the symbol immediately, inside the `transform` delegate that already has access to
it, and let nothing holding a reference to the compilation itself survive into a later pipeline
stage.

## Advanced: `ImmutableArray<T>` still isn't equatable by value on its own

A collected `IncrementalValuesProvider<T>.Collect()` produces an `ImmutableArray<T>` — but
`ImmutableArray<T>`'s default `Equals` is reference equality on the underlying array, not an
element-wise comparison, so two collected arrays with identical elements still compare unequal and
silently defeat caching downstream of a `Collect()` call:

```csharp
public readonly struct EquatableArray<T>(T[] items) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly T[] _items = items;

    public bool Equals(EquatableArray<T> other) => _items.AsSpan().SequenceEqual(other._items);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (T item in _items)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}
```

Wrap a `Collect()`'d array in a small equatable wrapper like this (or use one from an established
generator-authoring helper library) any time collected data crosses a further pipeline stage —
`Select`, `Combine`, or `RegisterSourceOutput` all trigger the same comparison.

## Advanced: `WithComparer` for a case the default `Equals` gets wrong on purpose

Occasionally a stage's natural equality is *too* strict for caching purposes — for example, a model
that includes a diagnostic-only field (a source `Location` used purely for error reporting) whose
change shouldn't itself invalidate the generated output. `WithComparer` supplies a custom
`IEqualityComparer<T>` for just that one stage's caching decision, independent of the type's own
`Equals`:

```csharp
candidates.WithComparer(GreetableTypeComparer.IgnoringLocation);
```

## Fallback

None — this file applies to every `IIncrementalGenerator` regardless of which later tier's APIs it
also uses; equatable modeling is the baseline correctness requirement for the pipeline introduced in
[net6-iincrementalgenerator.md](../references/net6-iincrementalgenerator.md) itself.
