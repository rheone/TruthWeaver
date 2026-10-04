# Classic Extension Methods (C# 3.0+ / .NET Framework 3.5 through .NET 9)

Introduced in C# 3.0 (.NET Framework 3.5, 2007) to support LINQ's fluent query syntax (`Where`,
`Select`, etc. over `IEnumerable<T>`). This is the baseline syntax that still compiles, unchanged,
on every later C# version including 14 and 15 — the two syntaxes are binary-compatible and
coexist in the same codebase. See
[specialized/resolution-and-coexistence-rules.md](../specialized/resolution-and-coexistence-rules.md)
for the precedence and mixing rules.

## Rules

1. Declared in a `static class` (the class itself needs nothing special — only certain *methods*
   inside it do).
2. The method is `static`.
3. The **first parameter** is prefixed with `this`, naming the type being extended (the
   *receiver*).
4. Called with instance-method syntax — `receiver.Method(args)` — even though it compiles to
   `Namespace.ExtensionClass.Method(receiver, args)`.
5. An extension method never overrides or hides a real member — a member actually declared on the
   type, or on any base type/interface it implements, always wins over an extension method with
   the same signature, silently.
6. Only visible for `.`-call resolution when its namespace is imported (`using Acme.Text;`), or
   the caller is in the same namespace, or via `using static`. Without that, call it fully
   qualified.

## Basic use case

```csharp
namespace Acme.Text;

public static class StringExtensions
{
    public static bool IsNullOrBlank(this string? value) =>
        string.IsNullOrWhiteSpace(value);

    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
```

```csharp
using Acme.Text;

string? title = GetTitle();
if (title.IsNullOrBlank())
{
    title = "Untitled";
}
Console.WriteLine(title.Truncate(40));
```

`this string? value` above — a nullable annotation on a receiver is legal syntax at any C#
version, but it's only *enforced* (nullable warnings) starting C# 8 with
`<Nullable>enable</Nullable>`. See [csharp8-nullable-extensions.md](csharp8-nullable-extensions.md).

## Advanced use case: extending a type you don't own

Extension methods are how you add behavior to a sealed type, a third-party type, or
`IEnumerable<T>` itself, without a wrapper class or inheritance:

```csharp
public static class EnumerableAcmeExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class =>
        source.Where(item => item is not null)!;

    public static (List<T> matches, List<T> rest) Partition<T>(
        this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var matches = new List<T>();
        var rest = new List<T>();
        foreach (var item in source)
        {
            (predicate(item) ? matches : rest).Add(item);
        }
        return (matches, rest);
    }
}
```

```csharp
var (adults, minors) = people.Partition(p => p.Age >= 18);
```

`Partition` is a **generic** extension method (`<T>`), inferred from the `IEnumerable<T> source`
argument at the call site — no `<Person>` needed. See
[specialized/generic-extension-members.md](../specialized/generic-extension-members.md) for
constraints, multiple type parameters, and extending open generic types like
`Dictionary<TKey, TValue>`.

## What classic extension methods cannot do

- No extension **properties**, **indexers**, or **operators** — only methods.
- No **static** extension members callable as `Type.Member()` (only instance-shaped
  `instance.Member()`).
- Cannot access private members of the extended type.
- Cannot be `virtual`/`override`/`abstract`.

All four gaps are closed by extension *members* in C# 14 — see
[csharp14-extension-members.md](csharp14-extension-members.md). There is no way to express
extension properties/statics/operators pre-C#14; the only pre-14 workaround is a
differently-named method (`GetFoo()` instead of a `Foo` property).

## Fallback

Works down to .NET Framework 3.5. Below that, see
[pre-csharp3-no-extensions.md](pre-csharp3-no-extensions.md).
