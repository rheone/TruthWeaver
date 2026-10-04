# The options pattern: `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`

The options pattern (`Microsoft.Extensions.Options`, brought in transitively wherever
`Microsoft.Extensions.DependencyInjection` config-binding helpers are used) is the standard way
strongly-typed configuration is delivered through the DI container. All three interfaces wrap the
*same* underlying bound options type `T` — a plain class with settable properties — but differ in
**when the value is (re)computed** and **what lifetime the wrapper itself has**.

## Setup

```csharp
public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
}

services.AddOptions<SmtpOptions>()
    .Bind(configuration.GetSection("Smtp"))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // fail at host startup, not on first use, if validation fails
```

`AddOptions<T>()` (and the shorter `services.Configure<SmtpOptions>(configuration.GetSection("Smtp"))`
form) registers the machinery needed for all three consumption interfaces below — you configure once
and then choose whichever of the three interfaces fits the consumer.

## `IOptions<T>` — singleton, computed once

```csharp
public sealed class MailSender(IOptions<SmtpOptions> options)
{
    private readonly SmtpOptions _options = options.Value;
}
```

- Registered as a **singleton**.
- `.Value` is computed **once**, the first time it's accessed, and cached for the app's lifetime —
  it does **not** pick up configuration changes made after that first access (e.g. a reloaded
  `appsettings.json` via `reloadOnChange: true`).
- Safe to inject into singletons (it's a singleton itself, so no captive-dependency issue).
- Use for configuration that's genuinely static for the process lifetime, or when you deliberately
  don't want to react to config reloads mid-run.

## `IOptionsSnapshot<T>` — scoped, recomputed per scope

```csharp
public sealed class OrderService(IOptionsSnapshot<SmtpOptions> options)
{
    private readonly SmtpOptions _options = options.Value; // fresh per scope (e.g. per HTTP request)
}
```

- Registered as **scoped**.
- `.Value` is recomputed once per scope — every new scope (e.g. every HTTP request in ASP.NET Core)
  gets a value reflecting the *current* configuration state at the start of that scope, but it's
  still fixed for the duration of that one scope.
- **Cannot** be injected into a singleton — same captive-dependency rule as any scoped service (see
  [pitfalls.md](pitfalls.md) and [validation-and-errors.md](validation-and-errors.md)). Attempting to
  do so throws with `ValidateScopes = true`, or silently misbehaves without it.
- Use for configuration you want to be able to change (e.g. via a reloaded config file) and have
  picked up on the next unit of work, without needing live, mid-request updates.
- Also supports **named options** (`options.Get("SomeName")`) for scenarios with multiple named
  configurations of the same options type.

## `IOptionsMonitor<T>` — singleton, live-updating, with change notifications

```csharp
public sealed class BackgroundMailWorker(IOptionsMonitor<SmtpOptions> monitor)
{
    public void Send()
    {
        SmtpOptions current = monitor.CurrentValue; // always the latest value, computed on demand
    }

    public IDisposable Watch() => monitor.OnChange(updated =>
    {
        // invoked every time the underlying configuration changes
    });
}
```

- Registered as a **singleton** — safe to inject into other singletons, including long-running
  background services.
- `.CurrentValue` always reflects the latest configuration, recomputed whenever the source changes
  (not just once, not just per-scope) — this is what you want for a `BackgroundService` or any other
  long-lived singleton that needs to observe config changes without restarting or re-scoping.
- Provides `OnChange(Action<T>)` for explicit change notification/callback, on top of just reading
  the current value.
- Also supports named options via `monitor.CurrentValue` overloads that take a name.

## Picking between them

| Interface | Lifetime | Value freshness | Use when |
| --- | --- | --- | --- |
| `IOptions<T>` | Singleton | Computed once, never updates | Static config; consumer is (or is fine acting like) a singleton |
| `IOptionsSnapshot<T>` | Scoped | Recomputed once per scope | Per-request/per-unit-of-work config that may change between units of work |
| `IOptionsMonitor<T>` | Singleton | Always current, with change notifications | Long-lived singletons/background services that need live config updates |

A common mistake is injecting `IOptionsSnapshot<T>` into a singleton (compiles fine, fails or
misbehaves at resolution/runtime because of the lifetime mismatch — see
[pitfalls.md](pitfalls.md)) when `IOptionsMonitor<T>` was the interface actually wanted for that
consumer.
