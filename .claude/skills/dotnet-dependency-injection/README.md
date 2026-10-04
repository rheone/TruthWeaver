# Dependency Injection (Microsoft.Extensions.DependencyInjection)

Guidance on the built-in .NET dependency injection container: service lifetimes, registration
patterns, resolution and constructor conventions, startup validation, and pitfalls like captive
dependencies.

## When to reach for it

- You're deciding whether a service should be registered as singleton, scoped, or transient.
- You're debugging an exception thrown while building or resolving from the service provider.
- You're reviewing a registration for a captive dependency, a service-locator anti-pattern, or a
  service that should be keyed instead of duplicated.

## Using it

This skill fires automatically when your request involves registering services, choosing a
lifetime, or debugging a DI startup exception. You can also invoke it directly with
`/dotnet-dependency-injection`.

## What it covers

| Topic | Reference |
| --- | --- |
| `IServiceCollection`, `ServiceDescriptor`, `BuildServiceProvider`, `IServiceProvider`, scopes | [references/core-concepts.md](references/core-concepts.md) |
| `AddSingleton`/`AddScoped`/`AddTransient`: semantics and disposal for each | [references/lifetimes.md](references/lifetimes.md) |
| Instance and factory-delegate registration, keyed services, `TryAdd`/`TryAddEnumerable`, open generics | [references/registration-patterns.md](references/registration-patterns.md) |
| Resolving `IEnumerable<T>`, constructor-injection conventions, ambiguous constructors | [references/resolution-and-constructors.md](references/resolution-and-constructors.md) |
| `ValidateOnBuild`/`ValidateScopes`, decoding startup DI exceptions | [references/validation-and-errors.md](references/validation-and-errors.md) |
| `IOptions<T>`/`IOptionsSnapshot<T>`/`IOptionsMonitor<T>` through DI | [references/options-pattern.md](references/options-pattern.md) |
| ASP.NET Core vs. standalone generic-host usage, the third-party container extension point | [references/extensibility-and-hosting.md](references/extensibility-and-hosting.md) |
| Captive dependencies, service-locator anti-pattern, over-registering as singleton | [references/pitfalls.md](references/pitfalls.md) |
| Building a minimal `ServiceCollection` for tests | [references/testing.md](references/testing.md) |

## Example prompts

- "Should this repository be scoped or singleton given it depends on a DbContext?"
- "I'm getting an exception when building the service provider. Help me decode it."
- "Register this cache client as a keyed service so I can resolve two different instances."
