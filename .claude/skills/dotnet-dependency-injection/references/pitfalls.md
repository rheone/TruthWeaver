# Common pitfalls

## Captive dependencies (scoped-in-singleton)

A **captive dependency** is a shorter-lived service (typically scoped, sometimes transient) that gets
captured — held onto via a constructor-injected field — by a longer-lived service (typically
singleton). Because the singleton is constructed once and never re-constructed, the captured instance
lives for the entire app lifetime too, defeating whatever per-scope behavior it was meant to have
(e.g. a `DbContext` meant to represent one request's unit of work now silently represents *every*
request that singleton ever touches, sharing state — and, for many disposable resources, throwing
`ObjectDisposedException` the moment the *first* scope that created it is disposed, since the scope
disposes it out from under the still-alive singleton).

```csharp
// WRONG: OrderCache is a singleton capturing a scoped repository.
public sealed class OrderCache(IOrderRepository repository) // repository is scoped
{
    // repository here is really just the FIRST scope's instance, forever.
}

services.AddSingleton<OrderCache>();
services.AddScoped<IOrderRepository, SqlOrderRepository>();
```

**Fix**: inject `IServiceScopeFactory` instead, and create a scope each time the scoped dependency is
actually needed:

```csharp
public sealed class OrderCache(IServiceScopeFactory scopeFactory)
{
    public async Task RefreshAsync()
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        // use repository within this scope's lifetime only
    }
}
```

Or, if the dependency doesn't actually need to be scoped (no per-request state), reconsider whether
it should be registered as a singleton instead — see "over-registering as singleton" below for the
inverse mistake.

`ValidateScopes = true` catches the *direct* form of this mistake at resolution time (see
[validation-and-errors.md](validation-and-errors.md)) — but it can't catch every transitive shape
(e.g. a singleton capturing an `IServiceProvider` and calling `GetService` lazily bypasses static
detection); code review and the discipline of "singletons only depend on singletons" remain necessary.

## The service-locator anti-pattern

Injecting `IServiceProvider` itself into a class, and calling `GetService<T>()`/`GetRequiredService<T>()`
from within its methods instead of declaring real constructor dependencies, is the **service locator**
anti-pattern:

```csharp
// AVOID: hides real dependencies behind a generic resolver.
public sealed class OrderService(IServiceProvider provider)
{
    public void Process()
    {
        var repository = provider.GetRequiredService<IOrderRepository>(); // hidden dependency
        var validator = provider.GetRequiredService<IValidator>();        // hidden dependency
    }
}
```

This defeats the main benefits of DI: dependencies are no longer visible in the constructor
signature (so they can't be verified by the compiler, can't be unit-tested by just constructing the
class with fakes, and don't show up in `ValidateOnBuild`'s walk in a way that clearly names this class
as the consumer). Prefer explicit constructor parameters:

```csharp
public sealed class OrderService(IOrderRepository repository, IValidator validator)
{
    public void Process() { /* repository and validator are explicit, testable dependencies */ }
}
```

**Legitimate exceptions**: `IServiceScopeFactory` for creating scopes on demand (see above), and
factory/orchestration code whose entire job is generic object composition (the hosting infrastructure
itself, a plugin loader resolving types not known at compile time). If a class's *primary purpose* is
resolving other services dynamically, that's a deliberate factory role, not an accidental service
locator; if it's incidental — a normal business-logic class reaching into the container instead of
declaring what it needs — it's the anti-pattern.

## Over-registering as singleton

Registering everything as `AddSingleton` "because it's simpler" or "because it's slightly faster to
resolve" causes two distinct problems:

1. **Thread-safety burden**: every singleton is shared across every concurrent scope/request. A
   service that was designed assuming single-threaded, per-request use (holding mutable instance
   state, wrapping a non-thread-safe resource like many database client types) becomes a source of
   race conditions the moment it's shared as a singleton, even though nothing about registering it
   that way raised a compile error.
2. **It forecloses genuinely scoped dependencies**: once a service is a singleton, it can never
   validly depend on anything scoped (see "captive dependencies" above) — so over-registering as
   singleton early tends to cascade, forcing every future dependency of that service to also avoid
   scoped lifetimes, or to route around the singleton via `IServiceScopeFactory` indirection that
   wouldn't have been needed if the lifetime had matched the service's actual nature.

**Guidance**: default to `Scoped` for anything that touches per-request/per-unit-of-work state (data
access, anything wrapping a request-correlated resource), reserve `Singleton` for genuinely stateless
services or services that explicitly manage their own thread-safe shared state (caches, clients
designed for singleton reuse, configuration snapshots), and use `Transient` for cheap, stateless,
short-lived helpers. Choosing the lifetime that matches the service's actual nature — not the
lifetime that's easiest to wire up — avoids both the captive-dependency trap and unnecessary
thread-safety work.
