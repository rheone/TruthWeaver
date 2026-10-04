# Service validation and common startup errors

`BuildServiceProvider` accepts a `ServiceProviderOptions` with two independent flags that catch DI
mistakes early rather than letting them surface as confusing runtime failures deep in request
handling.

```csharp
var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true,
});
```

In an ASP.NET Core / generic-host app, both are turned on **by default when the host environment is
`Development`**, and off in other environments (for startup-time performance) — you don't need to set
them explicitly there unless you want them enabled in all environments, e.g. to catch registration
mistakes in a staging/CI environment too.

## `ValidateOnBuild`

When `true`, `BuildServiceProvider()` itself eagerly walks **every non-open-generic registration** in
the collection, attempting to resolve each one, and throws `AggregateException` (or
`InvalidOperationException` for a single failure) immediately if any of them can't be constructed —
missing dependency, captive dependency, ambiguous constructor, etc. Without it, each problem only
surfaces the first time something actually *requests* that particular service, which for a rarely-hit
code path might not happen until well after deployment.

**Trade-off**: this makes `BuildServiceProvider()` itself slower (it constructs — and immediately
discards — every registered service once) and it runs at process startup, so enabling it in
production trades a slightly slower cold start for catching config-only mistakes before traffic is
served, rather than during it.

## `ValidateScopes`

When `true`, enforces two rules at resolution time (not at build time):

1. A scoped service **cannot be resolved from the root `IServiceProvider`** — only from within a
   created `IServiceScope`. Doing so throws `InvalidOperationException`.
2. A singleton (or the root provider) **cannot capture** a scoped service as a captured dependency —
   this is the check that catches captive dependencies (see [pitfalls.md](pitfalls.md)) — attempting
   to resolve a scoped service while constructing a singleton throws `InvalidOperationException` with
   a message to the effect of *"Cannot resolve scoped service '...' from root provider."*

Without `ValidateScopes`, both of these silently "work" in a way that's actually wrong: the scoped
service gets built once, cached as if it were a singleton, and reused forever from the root's own
internal resolution cache — no exception, just quietly incorrect lifetime behavior that can leak
per-request state across requests.

## Reading the common exceptions

**Missing registration** — thrown from `GetRequiredService<T>()`, or from resolving a constructor
parameter whose type isn't registered:

```
System.InvalidOperationException: Unable to resolve service for type 'MyApp.IOrderRepository'
while attempting to activate 'MyApp.OrderService'.
```

Read it as: *something* (named at the end) needed the type named right after "resolve service for
type" and nothing satisfies it. Fix: register the missing type, or check for a typo in which
interface/implementation pair was registered.

**Captive dependency / invalid scope capture** — thrown when a longer-lived service (typically
singleton) depends, directly or transitively, on a scoped service, with `ValidateScopes = true`:

```
System.InvalidOperationException: Cannot consume scoped service 'MyApp.IOrderRepository' from
singleton 'MyApp.ReportCache'.
```

Read it as: the container is refusing to let a singleton hold onto a scoped instance forever. Fix:
either lower the singleton's lifetime to scoped (if that's actually correct), or inject
`IServiceScopeFactory` into the singleton and create a scope on demand each time it needs the scoped
service, rather than capturing it as a constructor-injected field (see [pitfalls.md](pitfalls.md)).

**Ambiguous constructor** — thrown when multiple public constructors are all fully satisfiable and
tied for the most parameters (see [resolution-and-constructors.md](resolution-and-constructors.md)):

```
System.InvalidOperationException: Unable to activate type 'MyApp.ReportGenerator'. The following
constructors are ambiguous: ...
```

Fix: reduce to a single public constructor, or restructure so only one candidate constructor is
fully satisfiable by what's registered.

**Circular dependency** — thrown when resolving service A requires resolving service B which
(directly or transitively) requires resolving A again:

```
System.InvalidOperationException: A circular dependency was detected for the service of type
'MyApp.IA'.
```

Fix: break the cycle — usually by introducing a factory/lazy indirection (e.g. injecting
`Func<IB>` or `IServiceProvider` and resolving `B` lazily instead of eagerly in the constructor) or
by reconsidering whether the two services should really depend on each other directly at all.
