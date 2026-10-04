# Core concepts

The four types you need to understand before anything else in this skill makes sense.

## `IServiceCollection`

`Microsoft.Extensions.DependencyInjection.IServiceCollection` — a simple `IList<ServiceDescriptor>`
(it literally implements `IList<ServiceDescriptor>`). It has no behavior of its own beyond being a
list; every `AddXxx`/`TryAddXxx` method you call is an extension method that appends (or
conditionally appends) a `ServiceDescriptor` to it.

```csharp
IServiceCollection services = new ServiceCollection();
services.AddSingleton<IClock, SystemClock>();
// services now contains one ServiceDescriptor entry
```

Because it's just a list, registration order matters for some behaviors (see "last registration
wins" below) but not for others (constructor parameter resolution doesn't care what order services
were registered in).

## `ServiceDescriptor`

Each entry in the collection is a `ServiceDescriptor` describing:

- **Service type** — the type callers ask for (usually an interface).
- **Implementation** — one of: an implementation *type* (constructed by the container), an
  implementation *instance* (provided up front), or an implementation *factory* delegate
  (`Func<IServiceProvider, object>`, or `Func<IServiceProvider, object?, object>` for keyed
  services).
- **Lifetime** — `ServiceLifetime.Singleton`, `Scoped`, or `Transient`.
- **Service key** (optional, since keyed services) — an arbitrary `object?` that distinguishes
  multiple registrations of the same service type.

You rarely construct a `ServiceDescriptor` directly — the `AddSingleton`/`AddScoped`/`AddTransient`
extension methods on `IServiceCollection` build one for you — but constructing one explicitly is
occasionally useful for advanced scenarios (e.g. inspecting or replacing existing registrations):

```csharp
var descriptor = ServiceDescriptor.Scoped<IOrderRepository, OrderRepository>();
services.Add(descriptor);
```

## `BuildServiceProvider` and `IServiceProvider`

`BuildServiceProvider()` (an extension method on `IServiceCollection`) compiles the accumulated
`ServiceDescriptor` list into an `IServiceProvider` — the actual resolver. Once built, further
mutation of the original `IServiceCollection` has no effect on the provider that was already built.

```csharp
ServiceProvider provider = services.BuildServiceProvider();
```

`BuildServiceProvider` optionally takes a `ServiceProviderOptions` to control validation behavior
(`ValidateOnBuild`, `ValidateScopes` — see [validation-and-errors.md](validation-and-errors.md)).

`IServiceProvider` exposes one core member:

```csharp
object? GetService(Type serviceType);
```

Everything else — `GetRequiredService<T>()`, `GetService<T>()`, `GetServices<T>()`,
`GetKeyedService<T>(key)`, `GetRequiredKeyedService<T>(key)` — is an extension method built on top
of that single method. `GetRequiredService<T>()` throws `InvalidOperationException` if nothing is
registered for `T`; `GetService<T>()` returns `null` instead.

The concrete `ServiceProvider` class returned by `BuildServiceProvider()` implements `IDisposable`
(and `IAsyncDisposable`) — disposing it disposes every singleton (and any still-open root-level
scoped instances) it created. Always dispose (or `using`) a built `ServiceProvider` you own.

## `IServiceScope` and `IServiceScopeFactory`

A **scope** is a unit of "how long a scoped service instance lives." The root `IServiceProvider`
itself defines the root scope; `IServiceScopeFactory.CreateScope()` creates a nested scope with its
own resolution cache for scoped services.

```csharp
IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

using (IServiceScope scope = scopeFactory.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    // repository (and anything else scoped, resolved within this scope) is disposed
    // when the `using` block ends and the scope is disposed.
}
```

`IServiceScope.ServiceProvider` is a distinct `IServiceProvider` from the root one — scoped services
resolved through it are cached per-scope, and everything resolved within the scope (that implements
`IDisposable`/`IAsyncDisposable`) is disposed when the scope itself is disposed. ASP.NET Core creates
one scope per HTTP request automatically; in a console app or background worker, you create scopes
explicitly wherever "per unit of work" boundaries make sense (e.g. per queued message, per batch
job iteration).

`IServiceScopeFactory` is itself registered in the container automatically — you can inject it into
any service that needs to create scopes on demand (a classic pattern for singletons that need to
periodically produce a scoped unit of work; see [pitfalls.md](pitfalls.md) for why you must not just
inject the scoped dependency directly into that singleton instead).
