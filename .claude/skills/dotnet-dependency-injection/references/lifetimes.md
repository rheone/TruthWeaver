# Lifetimes: Singleton, Scoped, Transient

Three lifetimes, chosen per-registration via `AddSingleton`, `AddScoped`, or `AddTransient` (and
their keyed counterparts — see [registration-patterns.md](registration-patterns.md)).

## `AddSingleton`

One instance for the entire lifetime of the `IServiceProvider` (typically the whole app). Created
the first time it's requested (or immediately, if registered as an instance — see
[registration-patterns.md](registration-patterns.md)), then reused for every subsequent request from
any scope.

```csharp
services.AddSingleton<IClock, SystemClock>();
```

**Disposal**: if the implementation is `IDisposable`/`IAsyncDisposable`, the *root* `ServiceProvider`
disposes it when the root provider itself is disposed (app shutdown). Singletons are never disposed
mid-lifetime just because a scope ends.

**Use for**: stateless services, or services whose internal state is explicitly meant to be shared
and thread-safe across the whole app (caches, configuration snapshots taken at startup, connection
pools that manage their own thread safety).

**Risk**: a singleton must be thread-safe — it will be called concurrently from multiple scopes. It
must also never depend on a scoped or transient service that holds per-request state (see
"captive dependency" in [pitfalls.md](pitfalls.md)).

## `AddScoped`

One instance per scope (`IServiceScope`). Within a single scope, repeated resolutions return the
same instance; a different scope gets its own instance.

```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

**Disposal**: disposed when the *scope* that created it is disposed. In ASP.NET Core, the scope is
created and disposed automatically around each HTTP request, so a scoped `IDisposable` is disposed
at the end of the request. In a console/worker app, you own scope creation and disposal explicitly
(see [core-concepts.md](core-concepts.md)).

**Use for**: per-request or per-unit-of-work state — a `DbContext`, a request-correlation object, a
repository backed by a per-request database connection/transaction.

**Risk**: resolving a scoped service directly from the *root* provider (rather than from a scope) is
invalid — with `ValidateScopes = true` it throws `InvalidOperationException` at resolution time; with
validation off it silently behaves like a singleton (created once, in the root's own resolution
cache, and never re-created per scope) which is a common and hard-to-diagnose bug. See
[validation-and-errors.md](validation-and-errors.md).

## `AddTransient`

A new instance every single time it's requested — even multiple times within the same scope or the
same constructor's parameter list (if two parameters both need the same transient service type,
each gets its own instance unless you've registered a factory or instance instead).

```csharp
services.AddTransient<IValidator, OrderValidator>();
```

**Disposal**: a transient `IDisposable` resolved directly from the container is tracked and disposed
by whichever scope (or the root provider, if resolved from root) it was resolved within — *not*
disposed immediately after use. This means a long-lived scope that resolves many short-lived
transient `IDisposable`s accumulates references to all of them until the scope itself ends — a
memory-retention footgun if a scope is unusually long-lived and transient services are disposable
and numerous.

**Use for**: lightweight, stateless services with no meaningful shared state — validators, mappers,
small stateless helper classes.

## Choosing between them — the governing rule

**A service must not have a lifetime longer than any service it depends on.** A singleton can only
safely depend on other singletons. A scoped service can depend on singletons and other scoped
services (and transients). A transient service can depend on anything. Violating this (a longer-lived
service holding a reference to a shorter-lived one) is a captive dependency — see
[pitfalls.md](pitfalls.md) for the failure mode and [validation-and-errors.md](validation-and-errors.md)
for how to catch it automatically at startup.

## "Last registration wins" for plain resolution

If you register the same service type more than once (regardless of lifetime), `GetService<T>()` /
`GetRequiredService<T>()` resolves to the **last** registration added — earlier ones are not removed
from the collection, but a single-instance resolution only ever sees the last one. All registrations
(not just the last) are returned if you resolve `IEnumerable<T>` instead — see
[resolution-and-constructors.md](resolution-and-constructors.md). This is also why `TryAddXxx` exists
(see [registration-patterns.md](registration-patterns.md)): it registers only if nothing has claimed
that service type yet, rather than silently shadowing an existing registration.
