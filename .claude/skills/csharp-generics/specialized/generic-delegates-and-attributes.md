# Generic Delegates and Generic Attributes

## Generic delegates

### BCL delegates: `Func<>`, `Action<>`, `Predicate<T>`

```csharp
Func<int, int, int> add = (a, b) => a + b;
Action<string> log = message => Console.WriteLine(message);
Predicate<Order> isPending = o => o.Status == OrderStatus.Pending;
```

`Func<T1, ..., TResult>` (up to 16 input parameters) and `Action<T1, ...>` cover almost every
callback shape; reach for a custom delegate only when a named type clarifies intent beyond what
`Func`/`Action` communicate on their own.

### Custom generic delegates

```csharp
public delegate TResult Transformer<TInput, TResult>(TInput input);

public static IEnumerable<TResult> Map<TInput, TResult>(
    IEnumerable<TInput> source, Transformer<TInput, TResult> transform)
{
    foreach (var item in source)
    {
        yield return transform(item);
    }
}
```

A named delegate type documents the role (`Transformer`) that a bare `Func<TInput, TResult>`
parameter leaves implicit — worth it mainly in a public API where the name carries meaning to
callers.

### Variant custom delegates

```csharp
public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
```

See [variance-in-depth.md](variance-in-depth.md) for the full rules; the same input/output
position check that applies to interfaces applies to delegate parameter and return positions.

## Generic attributes (C# 11+)

```csharp
public class RequiresPermissionAttribute<TPermission> : Attribute where TPermission : IPermission
{
}

[RequiresPermission<AdminPermission>]
public void DeleteAccount(int accountId) { /* ... */ }
```

### Reading a generic attribute back via reflection

```csharp
var attribute = typeof(AccountService)
    .GetMethod(nameof(AccountService.DeleteAccount))!
    .GetCustomAttributes()
    .OfType<Attribute>()
    .First(a => a.GetType().IsGenericType &&
                a.GetType().GetGenericTypeDefinition() == typeof(RequiresPermissionAttribute<>));

Type permissionType = attribute.GetType().GetGenericArguments()[0];
```

Reflection over a generic attribute reads its type argument via `GetGenericArguments()` on the
attribute instance's runtime type — there's no shortcut equivalent to reading a constructor
argument, since the information lives in the type itself, not in a property.

### Restrictions

- The type argument must be closed (fully specified) at every usage site — `[RequiresPermission<T>]`
  where `T` is an unbound generic parameter of the enclosing declaration is not allowed.
- The attribute class itself still follows every ordinary attribute rule (derives from
  `Attribute`, usage-target restrictions via `[AttributeUsage]`, etc.) — genericity adds a type
  parameter, it doesn't relax anything else.

## Fallback

Custom/BCL generic delegates: no version floor beyond generics itself — valid from C# 2.0 (though
`Func<>`/`Action<>` gained their higher-arity overloads gradually through C# 3.0–4.0). Generic
attributes: use the `Type`-parameter constructor pattern from
[csharp11-generic-math-and-attributes.md](../references/csharp11-generic-math-and-attributes.md#fallback)
below C# 11.
