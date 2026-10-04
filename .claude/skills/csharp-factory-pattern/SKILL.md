---
name: csharp-factory-pattern
description: Reference for the Factory Method and Abstract Factory design patterns in C# — a factory method as a single creation-point abstraction (an interface with a Create method, or a static factory method), Abstract Factory for creating families of related objects that must stay mutually consistent, a generic IFactory<T>/IFactory<TArg, T> interface for reducing boilerplate across many unrelated factories, resolving a factory delegate through DI registration, and the decision list for when a factory is warranted versus calling new directly or relying on constructor injection alone. Use when object creation needs to vary at runtime, a family of related objects must be created consistently together, a long-lived consumer needs fresh instances of a short-lived dependency on demand, or reviewing whether a factory is overkill for a given type.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Factory Pattern

Factory Method centralizes how a single type (or one of several interchangeable variants) gets
constructed, behind an interface's `Create` method or a static factory method. Abstract Factory
extends that idea to a whole family of related types that must be created consistently together.

## Quick start

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
        _ => throw new NotSupportedException($"Unsupported channel: {request.Channel}")
    };
}
```

Not every type needs this — a plain `new` call or constructor injection is often enough; see
[references/factory-vs-new-vs-di.md](references/factory-vs-new-vs-di.md) before reaching for a
factory.

## Pick your reference file

| Situation | Reference file |
| --- | --- |
| Centralizing creation of one type (or interchangeable variants) behind a single point | [references/factory-method.md](references/factory-method.md) |
| Creating a family of related objects that must stay mutually consistent | [references/abstract-factory.md](references/abstract-factory.md) |
| Reducing boilerplate across many unrelated factory types | [references/generic-factory-interface.md](references/generic-factory-interface.md) |
| Resolving a factory (or factory delegate) through a DI container | [references/factory-registration-via-di.md](references/factory-registration-via-di.md) |
| Deciding whether a factory is warranted at all | [references/factory-vs-new-vs-di.md](references/factory-vs-new-vs-di.md) |
| Testing a factory's output or a consumer of one | [references/testing-factories.md](references/testing-factories.md) |
| Adding a new product without breaking existing consumers | [references/extending-factories.md](references/extending-factories.md) |
