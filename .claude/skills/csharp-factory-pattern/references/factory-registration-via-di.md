# Factory Registration via DI

A dependency injection container can supply a factory delegate as a resolvable dependency, letting a
consumer request "a way to create a T" without depending on the container itself or on a
hand-written factory class.

## Registering a factory delegate

```csharp
services.AddTransient<EmailNotification>();
services.AddSingleton<Func<string, string, EmailNotification>>(serviceProvider =>
    (recipient, body) =>
    {
        var notification = new EmailNotification(recipient, body);
        return notification;
    });
```

```csharp
public sealed class NotificationSender
{
    private readonly Func<string, string, EmailNotification> _createEmail;

    public NotificationSender(Func<string, string, EmailNotification> createEmail)
    {
        _createEmail = createEmail;
    }

    public void SendWelcomeEmail(string recipient) =>
        _createEmail(recipient, "Welcome!").Dispatch();
}
```

`NotificationSender` depends only on `Func<string, string, EmailNotification>` — it never sees
`IServiceProvider`, so it can't accidentally resolve unrelated services or hide dependencies that
should be visible in its constructor signature. This is the key discipline for factory-via-DI: the
factory delegate's own signature documents exactly what it needs and produces, unlike a raw
`IServiceProvider` injected as a general-purpose service locator.

## Registering an interface-based factory

The interface form from [factory-method.md](factory-method.md) registers exactly like any other
service — resolving it through the interface, not the concrete factory class, keeps the consumer
substitutable:

```csharp
services.AddSingleton<INotificationFactory, NotificationFactory>();
services.AddTransient<NotificationSender>();
```

## Resolving a fresh transient instance per call, from within a singleton

A common reason to inject a factory instead of the type directly: a long-lived (singleton) consumer
needs a fresh instance of a short-lived (transient or scoped) dependency on every operation, not once
at construction time. Injecting the dependency directly would freeze it at whatever instance existed
when the singleton was first constructed; injecting a factory defers creation to the moment it's
actually needed:

```csharp
services.AddSingleton<NotificationSender>();
services.AddTransient<EmailNotification>();
services.AddSingleton<Func<EmailNotification>>(serviceProvider =>
    () => serviceProvider.GetRequiredService<EmailNotification>());
```

The factory closes over `IServiceProvider` once, at registration time, inside the one place that's
allowed to touch it directly — every consumer of `Func<EmailNotification>` still only sees the
delegate's own signature, not the container.

## A registration mistake to avoid

Injecting `IServiceProvider` directly into a class and calling `GetService`/`GetRequiredService`
from inside business logic reintroduces the service-locator anti-pattern a factory delegate exists to
avoid — it hides the class's real dependencies behind a general-purpose resolver instead of
expressing them in the constructor signature (as a specific `Func<>` or a specific factory interface
does), and it makes the class resolve anything registered in the container, not just what it's
supposed to depend on. Reserve direct `IServiceProvider` injection for the registration lambda itself
(as in the transient-from-singleton example above), never for application or domain logic.
