# Resolving multiple implementations, and constructor conventions

## Resolving multiple implementations via `IEnumerable<T>`

Every registration for a given service type is preserved in the container, even when a plain
`GetService<T>()`/constructor-injected `T` only sees the last one (see "last registration wins" in
[lifetimes.md](lifetimes.md)). To get *all* of them, inject or resolve `IEnumerable<T>` instead of
`T`:

```csharp
services.AddSingleton<IValidator, OrderValidator>();
services.AddSingleton<IValidator, CustomerValidator>();

public sealed class ValidationRunner(IEnumerable<IValidator> validators)
{
    public IEnumerable<string> ValidateAll(object target) =>
        validators.SelectMany(v => v.Validate(target));
}
```

Implementations are returned **in registration order**. This is the standard pattern for
plugin-style extensibility — libraries register their own implementation via `TryAddEnumerable` (see
[registration-patterns.md](registration-patterns.md)) alongside whatever the application registers,
and consumers that want "all of them" ask for `IEnumerable<T>` while consumers that want "the
canonical one" ask for `T` directly.

`GetServices<T>()` is the equivalent extension method for resolving directly from an
`IServiceProvider` rather than via constructor injection.

## Constructor injection conventions

The container resolves a service's dependencies via **constructor injection only** — there is no
property or method injection built in. When constructing `TImplementation` for a registration, the
container:

1. Looks at `TImplementation`'s public constructors.
2. If there is exactly **one** public constructor, uses it, resolving each parameter from the
   container (throwing if any parameter's type isn't registered and has no default value).
3. If there are **multiple** public constructors, the container picks the one whose parameter types
   it can *all* satisfy from the registered services, preferring the constructor with the **most**
   parameters among those it can fully satisfy. If more than one constructor is satisfiable and tied
   for the most parameters, resolution throws `InvalidOperationException` — the ambiguity is not
   silently resolved by declaration order or any other implicit rule.

```csharp
public sealed class ReportGenerator
{
    // If only this constructor's dependencies are registered, it's used automatically.
    public ReportGenerator(IClock clock, IOrderRepository repository) { /* ... */ }

    // If ILogger is ALSO registered, the container prefers this one (more parameters, still
    // fully satisfiable) — this is how "optional extra dependency" constructor overloads work.
    public ReportGenerator(IClock clock, IOrderRepository repository, ILogger<ReportGenerator> logger)
        : this(clock, repository) { /* ... */ }
}
```

**Constructor parameters with default values** count as satisfiable even if the parameter's type
isn't registered — the container supplies the default rather than throwing:

```csharp
public sealed class ReportGenerator(IClock clock, int retryCount = 3) { /* ... */ }
// retryCount doesn't need to be registered; 3 is used if nothing overrides it via a factory.
```

**Practical guidance**: prefer exactly one public constructor per DI-constructed type. Multiple
constructors chosen by "which one the container can satisfy" is a subtle, easy-to-misjudge mechanism
— a new registration added elsewhere in the app can silently shift which constructor gets used. If
you need optional dependencies, prefer nullable parameters with a single constructor, or split the
behavior into a decorator/factory registered explicitly, over relying on constructor-overload
resolution.

## What happens with an unregistered dependency

If a constructor parameter's type has no matching registration (and no default value), resolving
that service throws `InvalidOperationException` with a message naming both the missing service type
and the type that needed it — see [validation-and-errors.md](validation-and-errors.md) for the exact
message shape and how `ValidateOnBuild` surfaces this at startup instead of at first use.
