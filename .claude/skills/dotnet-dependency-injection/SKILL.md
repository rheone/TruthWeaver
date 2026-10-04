---
name: dotnet-dependency-injection
description: Guidance on Microsoft.Extensions.DependencyInjection — the built-in .NET DI container (IServiceCollection, IServiceProvider, ServiceDescriptor). Covers AddSingleton/AddScoped/AddTransient lifetime semantics and disposal, instance and factory-delegate registration, keyed services (AddKeyedSingleton/Scoped/Transient), TryAdd/TryAddEnumerable for library-safe defaults, open generics, resolving IEnumerable<T>, constructor-injection conventions, ValidateOnBuild/ValidateScopes and common startup DI errors, the IOptions<T>/IOptionsSnapshot<T>/IOptionsMonitor<T> configuration-binding trio, ASP.NET Core builder.Services vs. standalone generic-host usage, the IServiceProviderFactory<TContainerBuilder> extension point for third-party containers, common pitfalls (captive dependencies, service locator anti-pattern, over-registering as singleton), and testing with a minimal ServiceCollection. Use when registering services, choosing a lifetime, resolving dependencies, debugging a DI startup exception, wiring IOptions, or deciding between the built-in container and a third-party one.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# Dependency Injection (Microsoft.Extensions.DependencyInjection)

Guidance on the built-in .NET dependency injection container — `Microsoft.Extensions.DependencyInjection`
(current stable release: **10.0.12**, shipping alongside **.NET 10**, an LTS release; part of the
[dotnet/runtime](https://github.com/dotnet/runtime) repository under the .NET Foundation).
The abstractions (`IServiceCollection`, `IServiceProvider`, `ServiceDescriptor`, …) live in the companion
`Microsoft.Extensions.DependencyInjection.Abstractions` package, versioned in lockstep.

This is a lightweight, constructor-injection-only container built into the framework — no property
injection, no interception/AOP, no XML config. It is the default container for ASP.NET Core and the
.NET generic host, and it is designed to be replaceable by a third-party container via a documented
extension point (see [references/extensibility-and-hosting.md](references/extensibility-and-hosting.md)).

## Pick your reference file by situation

| You're doing this... | Reference file |
| --- | --- |
| Learning the core types (`IServiceCollection`, `ServiceDescriptor`, `IServiceProvider`, `IServiceScope`/`IServiceScopeFactory`) and how `BuildServiceProvider` works | [references/core-concepts.md](references/core-concepts.md) |
| Choosing `AddSingleton` vs `AddScoped` vs `AddTransient`, or reasoning about disposal | [references/lifetimes.md](references/lifetimes.md) |
| Registering an existing instance, a factory delegate, a keyed service, an open generic, or using `TryAdd`/`TryAddEnumerable` for library-safe defaults | [references/registration-patterns.md](references/registration-patterns.md) |
| Injecting `IEnumerable<T>` for multiple implementations, or dealing with multiple/ambiguous constructors | [references/resolution-and-constructors.md](references/resolution-and-constructors.md) |
| Enabling `ValidateOnBuild`/`ValidateScopes`, or decoding a startup DI exception | [references/validation-and-errors.md](references/validation-and-errors.md) |
| Binding configuration through DI with `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>` | [references/options-pattern.md](references/options-pattern.md) |
| Wiring services in ASP.NET Core (`builder.Services`) vs. a standalone console/worker app (generic host), or plugging in a third-party container via `IServiceProviderFactory<TContainerBuilder>` | [references/extensibility-and-hosting.md](references/extensibility-and-hosting.md) |
| Avoiding captive dependencies, the service-locator anti-pattern, or singleton over-registration | [references/pitfalls.md](references/pitfalls.md) |
| Building a minimal `ServiceCollection` for tests, or validating all registrations resolve in test setup | [references/testing.md](references/testing.md) |

## Quick start

The most common shape — register services, build a provider (or let a host build one), resolve via
constructor injection:

```csharp
var services = new ServiceCollection();

services.AddSingleton<IClock, SystemClock>();          // one instance for the app's lifetime
services.AddScoped<IOrderRepository, OrderRepository>(); // one instance per scope (e.g. per HTTP request)
services.AddTransient<IValidator, OrderValidator>();     // a new instance every time it's requested

using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true, // fail fast at startup if any registration can't be constructed
    ValidateScopes = true,  // throw if a scoped/transient service is resolved from the root provider
});

using IServiceScope scope = provider.CreateScope();
var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
```

The single most common miss: registering a scoped service and then injecting it into a singleton
(directly or transitively) — a **captive dependency**. With `ValidateScopes = true` (the ASP.NET Core
default in the Development environment) this throws at resolution time; see
[references/pitfalls.md](references/pitfalls.md) and
[references/validation-and-errors.md](references/validation-and-errors.md).

## Out of scope

- Third-party DI containers (Autofac and similar) — this skill covers only the built-in
  `Microsoft.Extensions.DependencyInjection` container and the generic extension point
  (`IServiceProviderFactory<TContainerBuilder>`) that lets any third-party container plug into the
  same hosting model. It does not document any specific third-party product's API.
- AOP/interception, property injection, and XML-based configuration — not supported by this
  container by design.
- `IConfiguration`/configuration-provider mechanics (JSON files, environment variables, etc.) beyond
  what's needed to explain how `IOptions<T>` binds through DI — configuration source and provider
  details are a separate concern from the DI container itself.
