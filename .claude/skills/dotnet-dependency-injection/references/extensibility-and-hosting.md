# Hosting integration and the third-party-container extension point

## ASP.NET Core: `builder.Services`

In an ASP.NET Core minimal-hosting app, `WebApplicationBuilder.Services` (an `IServiceCollection`) is
where you register everything; the framework itself registers a large set of its own services
(routing, MVC infrastructure, logging, configuration, `IOptions` machinery, etc.) before your code
runs, and creates one `IServiceScope` per incoming HTTP request automatically — you never create that
scope yourself.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IOrderRepository, SqlOrderRepository>();
builder.Services.AddControllers();

var app = builder.Build(); // builds the IServiceProvider
app.Run();
```

`builder.Build()` calls `BuildServiceProvider()` under the hood (with `ValidateOnBuild`/
`ValidateScopes` defaulted to `true` when `IHostEnvironment.EnvironmentName == "Development"`, `false`
otherwise — see [validation-and-errors.md](validation-and-errors.md)). Per-request scoping is handled
by ASP.NET Core's request-processing middleware, which creates a scope, makes it available via
`HttpContext.RequestServices`, and disposes it at the end of the request.

## Standalone / non-web apps: the generic host

Console apps, worker services, and anything else that isn't an ASP.NET Core web app get the same
`IServiceCollection`-based registration model via the **generic host**
(`Microsoft.Extensions.Hosting`), either through `Host.CreateApplicationBuilder(args)` (the simpler,
newer API) or `Host.CreateDefaultBuilder(args)` (the older `IHostBuilder`-based API):

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddHostedService<QueueProcessor>(); // long-running background service

using IHost host = builder.Build();
await host.RunAsync();
```

There is no automatic per-request scope here (there's no "request") — code that needs scoped
services (e.g. inside a `BackgroundService`) injects `IServiceScopeFactory` (or
`IServiceProvider`, since it's registered too) and creates scopes explicitly around each unit of
work:

```csharp
public sealed class QueueProcessor(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            await repository.ProcessNextBatchAsync(stoppingToken);
        }
    }
}
```

This is the standard, correct pattern for a singleton (`BackgroundService` is registered as a
singleton via `AddHostedService`) that needs scoped dependencies — create a scope on demand rather
than injecting the scoped dependency directly (see [pitfalls.md](pitfalls.md) for why the direct
approach is a captive dependency bug).

## The third-party-container extension point: `IServiceProviderFactory<TContainerBuilder>`

`Microsoft.Extensions.DependencyInjection` is deliberately an **abstraction plus a default
implementation** — `IServiceCollection` and `IServiceProvider` are interfaces, and the concrete
`ServiceProvider` produced by `BuildServiceProvider()` is only the built-in implementation. Both the
ASP.NET Core host and the generic host let you substitute a different container entirely via
`IServiceProviderFactory<TContainerBuilder>`:

```csharp
public interface IServiceProviderFactory<TContainerBuilder> where TContainerBuilder : notnull
{
    TContainerBuilder CreateBuilder(IServiceCollection services);
    IServiceProvider CreateServiceProvider(TContainerBuilder containerBuilder);
}
```

The host calls `CreateBuilder(services)` to convert the accumulated `IServiceCollection` into
whatever container-specific builder type the third-party container uses (`TContainerBuilder`), gives
you a chance to configure that builder further via `ConfigureContainer<TContainerBuilder>(...)`, and
then calls `CreateServiceProvider(containerBuilder)` to get back a standard `IServiceProvider` — so
everything else in the app (constructor injection, `GetRequiredService<T>()`, scopes) keeps working
exactly the same regardless of which container is actually resolving services underneath.

Wiring a custom factory into either host:

```csharp
// Generic host (IHostBuilder-based):
hostBuilder
    .UseServiceProviderFactory(new SomeContainerServiceProviderFactory())
    .ConfigureContainer<SomeContainerBuilder>((context, containerBuilder) =>
    {
        // container-specific configuration, in addition to whatever was registered
        // via IServiceCollection
    });

// HostApplicationBuilder / WebApplicationBuilder (newer builder-based hosts):
builder.ConfigureContainer(new SomeContainerServiceProviderFactory(), containerBuilder =>
{
    // container-specific configuration
});
```

This is the mechanism that lets a third-party container stand in for the built-in one while every
other piece of framework and application code — anything written against `IServiceCollection` and
`IServiceProvider` — remains unaware of which container is actually running. Registrations made via
the standard `IServiceCollection` extension methods (`AddSingleton`, `AddScoped`, etc.) are still
honored; the factory is what translates that registration list into the third-party container's own
configuration surface and back into a conforming `IServiceProvider`.
