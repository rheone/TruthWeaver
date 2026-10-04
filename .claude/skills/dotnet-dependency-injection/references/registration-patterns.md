# Registration patterns

Beyond the plain `AddSingleton<TService, TImplementation>()` shape, the container supports several
other registration styles.

## Instance registration

Register an already-constructed object as a singleton. The container never constructs it and never
disposes it automatically unless you opt in.

```csharp
var clock = new SystemClock();
services.AddSingleton<IClock>(clock);
```

By default, an instance registered this way is **not** disposed by the container even if it's
`IDisposable` — the assumption is you own its lifetime since you constructed it yourself. If you want
the container to dispose it anyway, register it as a factory instead (below), or wrap the instance
type so ownership is unambiguous.

## Factory-delegate registration

Register a delegate that builds the implementation, with access to the resolving `IServiceProvider`
(useful when the implementation needs other registered services but you want to control construction
yourself — e.g. picking an implementation based on configuration, or calling a static factory
method):

```csharp
services.AddScoped<IOrderRepository>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()["Db:ConnectionString"];
    return new SqlOrderRepository(connectionString!);
});
```

Unlike a manually-provided instance, an object produced by a factory delegate **is** tracked and
disposed by the container according to its declared lifetime, same as a type-based registration.

For keyed factories, the delegate takes the key as a second parameter:
`Func<IServiceProvider, object?, TImplementation>` (see "Keyed services" below).

## Keyed services

Introduced in .NET 8 (`Microsoft.Extensions.DependencyInjection` 8.0) — register multiple
implementations of the same service type, distinguished by an arbitrary key, without needing wrapper
types or a manual lookup dictionary.

```csharp
services.AddKeyedSingleton<ICache, MemoryCache>("memory");
services.AddKeyedSingleton<ICache, RedisCache>("redis");
```

`AddKeyedSingleton`, `AddKeyedScoped`, and `AddKeyedTransient` mirror the unkeyed methods and support
the same overload shapes: implementation type, instance, and factory (the factory form takes the key
as its second parameter: `Func<IServiceProvider, object?, TImplementation>`).

Resolve a specific keyed registration either via the container directly:

```csharp
var redisCache = provider.GetRequiredKeyedService<ICache>("redis");
```

or via constructor injection using `[FromKeyedServices(key)]` on the parameter:

```csharp
public sealed class CacheWarmer(
    [FromKeyedServices("redis")] ICache cache)
{
    // ...
}
```

Keyed and unkeyed registrations of the same service type are tracked separately — an unkeyed
`GetService<ICache>()` does not see keyed registrations, and vice versa.

## `TryAdd` / `TryAddEnumerable` — library-safe defaults

`TryAddSingleton`, `TryAddScoped`, and `TryAddTransient` register a service **only if no
registration already exists for that exact service type**. This is the standard pattern for library
code that wants to provide a sensible default implementation without clobbering an application's own
registration, regardless of the order the two are called in:

```csharp
// Inside a library's "AddMyLibrary" extension method:
public static IServiceCollection AddMyLibrary(this IServiceCollection services)
{
    services.TryAddSingleton<IClock, SystemClock>();
    return services;
}

// Application code, called before OR after AddMyLibrary — its registration wins either way:
services.AddSingleton<IClock, FakeClock>();
services.AddMyLibrary();
```

`TryAddEnumerable(ServiceDescriptor)` is the equivalent for multi-implementation registrations
(services meant to be resolved via `IEnumerable<T>` — see
[resolution-and-constructors.md](resolution-and-constructors.md)): it adds the descriptor only if no
existing descriptor has the *same service type and same implementation type*, so calling the same
library-registration helper twice doesn't produce duplicate entries in the `IEnumerable<T>`, while
still allowing multiple *different* implementations of the same service type to coexist.

```csharp
services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidator, OrderValidator>());
services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidator, OrderValidator>()); // no-op, duplicate
services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidator, CustomerValidator>()); // added, different impl
```

## Open generics

Register a generic type definition once and let the container construct closed generic types on
demand as they're requested:

```csharp
services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

// Resolving IRepository<Order> or IRepository<Customer> both work, each constructing
// EfRepository<Order> / EfRepository<Customer> respectively — no need to register each
// closed generic type individually.
```

This uses the non-generic `Type`-based overloads of `AddSingleton`/`AddScoped`/`AddTransient` since
you can't write `typeof(IRepository<T>)` without a concrete `T`. It works for any arity of open
generic and composes normally with the rest of the registration surface (factories are not supported
for open generics — the container needs the type definition itself to close over the requested
generic arguments).
