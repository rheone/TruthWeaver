# Nullable flow analysis attributes

`System.Diagnostics.CodeAnalysis` attributes that let a method's *body* teach the nullable-flow
analyzer facts it can't infer from signature and flow alone — since C# 8.0 / .NET Core 3.0
(nullable reference types feature). These only affect compile-time nullable warnings; they have
zero runtime behavior.

Use these whenever a member's nullability contract depends on something other than its plain
static return/parameter types — an output that's non-null only when the return value says so
(`TryParse`-style), a property that's guaranteed non-null only after another member runs
(`IsInitialized` gates `Value`), or a method that never returns normally at all.

## The `TryXxx` pattern — `[NotNullWhen]` / `[MaybeNullWhen]`

```csharp
public static bool TryParse(string? input, [NotNullWhen(true)] out Config? result)
{
    if (input is null) { result = null; return false; }
    result = new Config(input);
    return true;
}
```

`[NotNullWhen(true)]` tells the analyzer: when this method returns `true`, treat `result` as
non-null from that point on. `[MaybeNullWhen(bool)]` is the mirror for a parameter that's
guaranteed non-null on entry but might become null within the method under that return outcome.

**When to add it proactively**: every `TryXxx(..., out T result)` method — without it, callers
get a nullable-warning on `result` even right after checking the `bool`, which trains people to
suppress warnings with `!` instead of trusting the analyzer.

## `[NotNull]` / `[MaybeNull]` — unconditional override

```csharp
[return: NotNull]
public string GetName() => _name ?? throw new InvalidOperationException();

[MaybeNull]
public T Value => _hasValue ? _value : default;
```

`[NotNull]` on a return value or `out`/`ref` parameter asserts it's never null *regardless of the
input*, overriding what the plain type annotation would otherwise say (useful when the plain
signature must stay nullable for API-shape reasons but the real guarantee is stronger).
`[MaybeNull]` is the reverse — a non-nullable-typed member that can, in practice, produce `null`.

## `[AllowNull]` / `[DisallowNull]` — input-side overrides

```csharp
private string _name = "";

[AllowNull]
public string Name
{
    get => _name;
    set => _name = value ?? "";
}
```

`[AllowNull]` on a non-nullable-typed setter/parameter says callers *may* pass `null` even though
the static type says otherwise (the member normalizes it internally, as above). `[DisallowNull]`
is the reverse: a nullable-typed parameter that the analyzer should flag if a literal `null` is
passed, even though the type would otherwise allow it.

## `[MemberNotNull]` / `[MemberNotNullWhen]`

```csharp
[MemberNotNull(nameof(_connection))]
private void EnsureConnected()
{
    _connection ??= OpenConnection();
}

public bool IsReady { get; private set; }

[MemberNotNullWhen(true, nameof(_connection))]
private bool TryGetConnection() => _connection is not null;
```

Tells the analyzer that after this method returns (unconditionally, or when it returns the given
bool), the named field/property is guaranteed non-null — the standard fix for the classic
"field is definitely assigned by an init helper called from every constructor, but the analyzer
can't see that" warning, without resorting to `= null!;` on the field declaration.

**When to add it proactively**: any `EnsureXxx`/`Initialize`-style helper called from multiple
constructors or an `OnActivated`-style lifecycle hook, where a field is null until that helper
runs and the analyzer has no way to see the connection between the helper and the field.

## `[DoesNotReturn]` / `[DoesNotReturnIf]`

```csharp
[DoesNotReturn]
private static void ThrowInvalid(string reason) => throw new InvalidOperationException(reason);

public string Describe(Item? item)
{
    if (item is null) ThrowInvalid("item required");
    return item.Name; // no nullable warning: analyzer knows the branch above never returns
}
```

`[DoesNotReturn]` marks a method that always throws or otherwise never returns normally (mirrors
what the analyzer already knows about `throw` expressions, extended to helper methods that wrap
a `throw`). `[DoesNotReturnIf(bool)]` is the conditional form for guard methods like
`Debug.Assert`-style helpers that only sometimes throw.

**When to add it proactively**: every dedicated throw-helper (see also `[StackTraceHidden]` in
[debugging-diagnostics.md](debugging-diagnostics.md) — the two are commonly paired on the same
method) — without `[DoesNotReturn]`, code after a call to the helper still gets flagged as
possibly using an unassigned/null value, defeating the point of centralizing the throw.

## Fallback / no-op behavior

These require C# 8.0 and the nullable-reference-types feature (`#nullable enable` /
`<Nullable>enable</Nullable>`) to have any effect; the attribute types themselves live in
`System.Diagnostics.CodeAnalysis` and are compiled into the BCL from .NET Core 3.0 / .NET
Standard 2.1 onward. On .NET Framework or older .NET Standard targets without nullable reference
types, these attributes have nothing to attach their meaning to — there is no lesser fallback;
omit them, and consider the `Nullable` NuGet polyfill package only if you need nullable
annotations on a library that still needs to *build* against an older target (the package
supplies the attribute types without requiring the runtime feature to be active).
