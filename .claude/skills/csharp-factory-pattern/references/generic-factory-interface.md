# Generic Factory Interface

A generic `IFactory<T>` reduces the boilerplate of declaring a purpose-named factory interface
(`INotificationFactory`, `IReportFactory`) for every type that needs one, at the cost of a less
self-documenting contract at each call site.

## Shape

```csharp
public interface IFactory<T>
{
    T Create();
}

public interface IFactory<in TArg, out T>
{
    T Create(TArg arg);
}
```

Concrete factories close the generic parameters over their own types:

```csharp
public sealed class EmailNotificationFactory : IFactory<NotificationRequest, INotification>
{
    public INotification Create(NotificationRequest arg) =>
        new EmailNotification(arg.Recipient, arg.Body);
}
```

Infrastructure code can depend on `IFactory<TArg, T>` generically without knowing the concrete types
at compile time — useful for a generic object pool, a generic cache-and-create wrapper, or any
component that hosts many unrelated factories through the same shape:

```csharp
public sealed class CreateOnDemandCache<TArg, T> where T : class
{
    private readonly IFactory<TArg, T> _factory;
    private readonly Dictionary<TArg, T> _cache = new();

    public CreateOnDemandCache(IFactory<TArg, T> factory) => _factory = factory;

    public T GetOrCreate(TArg arg)
    {
        if (!_cache.TryGetValue(arg, out var value))
        {
            value = _factory.Create(arg);
            _cache[arg] = value;
        }

        return value;
    }
}
```

## Generic factory vs. `Func<TArg, T>`

`IFactory<TArg, T>` and `Func<TArg, T>` express the same contract — the difference is entirely
whether the creation logic needs a name, its own dependencies via constructor injection, or more
than one related operation:

```csharp
Func<NotificationRequest, INotification> createEmail =
    request => new EmailNotification(request.Recipient, request.Body);
```

A `Func<>` needs no interface declaration and no implementing class; an `IFactory<TArg, T>`
implementation can take constructor-injected dependencies (a validator, a logger) and gives the
factory's concrete type a stable, discoverable identity for DI registration. Choose between them
using the same reasoning a delegate-based strategy uses against an interface-based one: a factory
with no dependencies and a single creation operation is well served by a bare `Func<>`; a factory
with dependencies, validation logic, or more than one related creation operation is better served by
a named class implementing `IFactory<TArg, T>`.

## When the generic form is worth it over a purpose-named interface

Use `IFactory<TArg, T>` when a codebase has many unrelated factory types and the *number* of
purpose-named interfaces (`INotificationFactory`, `IReportFactory`, `IWidgetFactory`) is itself the
problem being solved — usually inside generic infrastructure that hosts many of them uniformly. A
purpose-named interface remains more self-documenting at any call site that only ever deals with one
specific factory family, since `INotificationFactory.Create(NotificationRequest)` reads its intent
directly where `IFactory<NotificationRequest, INotification>.Create(NotificationRequest)` requires a
reader to already know what that particular closed generic instantiation means in context.
