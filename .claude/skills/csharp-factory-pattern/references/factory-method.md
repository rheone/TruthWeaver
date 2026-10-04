# Factory Method

Factory Method centralizes object creation behind a single creation point — an interface with a
`Create` method, or a static factory method on the type itself — instead of scattering `new`
expressions for a type across the codebase. This gives creation logic a single place to change when
it grows beyond a plain constructor call.

## Interface-based factory method

```csharp
public interface INotificationFactory
{
    INotification Create(NotificationRequest request);
}

public sealed class NotificationFactory : INotificationFactory
{
    public INotification Create(NotificationRequest request) => request.Channel switch
    {
        NotificationChannel.Email => new EmailNotification(request.Recipient, request.Body),
        NotificationChannel.Sms => new SmsNotification(request.Recipient, request.Body),
        NotificationChannel.Push => new PushNotification(request.Recipient, request.Body),
        _ => throw new NotSupportedException($"Unsupported channel: {request.Channel}")
    };
}
```

A consumer depends on `INotificationFactory`, not on `EmailNotification`, `SmsNotification`, or
`PushNotification` directly — adding a new channel means changing the factory's `switch`, not every
call site that creates a notification.

```csharp
public sealed class NotificationSender
{
    private readonly INotificationFactory _factory;

    public NotificationSender(INotificationFactory factory) => _factory = factory;

    public void Send(NotificationRequest request)
    {
        var notification = _factory.Create(request);
        notification.Dispatch();
    }
}
```

## Static factory method

When the creation logic doesn't need to vary at runtime — no swapping implementations, no DI
substitution — a static factory method on the type itself is simpler than a separate factory
interface and class:

```csharp
public sealed class EmailNotification
{
    public string Recipient { get; }
    public string Body { get; }

    private EmailNotification(string recipient, string body)
    {
        Recipient = recipient;
        Body = body;
    }

    public static EmailNotification ForRecipient(string recipient, string body)
    {
        if (!recipient.Contains('@'))
        {
            throw new ArgumentException("Recipient must be a valid email address", nameof(recipient));
        }

        return new EmailNotification(recipient, body);
    }
}
```

The private constructor forces every caller through `ForRecipient`, which is the single place that
validates the recipient and can absorb future construction logic (defaulting, normalization) without
any caller needing to change. This is the right shape when there's exactly one way to build the
type and no need to substitute a different creation strategy for testing or configuration.

## Why not just call `new` everywhere

A constructor call is fine as long as three things stay true: there's exactly one way to build the
type, no caller needs a substitutable creation strategy, and the constructor's parameter list is
stable. A Factory Method earns its cost once any of those stops holding — the type has more than one
concrete variant to choose between (see [abstract-factory.md](abstract-factory.md) for families of
related variants), the choice of variant depends on a runtime input, or callers need to substitute a
fake creation strategy in tests without controlling every constructor argument by hand. See
[factory-vs-new-vs-di.md](factory-vs-new-vs-di.md) for the full decision list, including when
constructor injection alone is enough and a factory would just be extra indirection.
