# Adding a New Product Without Breaking Existing Code

Extending a factory means adding a new thing it can produce. How cheap that is depends on which
factory shape you're extending — a plain Factory Method's `switch` grows a new arm; an Abstract
Factory's family interface requires updating every existing concrete factory.

## Adding a case to a Factory Method

```csharp
public sealed class NotificationFactory : INotificationFactory
{
    public INotification Create(NotificationRequest request) => request.Channel switch
    {
        NotificationChannel.Email => new EmailNotification(request.Recipient, request.Body),
        NotificationChannel.Sms => new SmsNotification(request.Recipient, request.Body),
        NotificationChannel.Push => new PushNotification(request.Recipient, request.Body),
        NotificationChannel.Slack => new SlackNotification(request.Recipient, request.Body), // new
        _ => throw new NotSupportedException($"Unsupported channel: {request.Channel}")
    };
}
```

No consumer of `INotificationFactory` changes — `NotificationSender` still calls `_factory
.Create(request)` exactly as before, unaware that a new channel now exists. Add the new concrete
type (`SlackNotification`), add its arm to the `switch`, and add a test for that new arm (see
[testing-factories.md](testing-factories.md)); nothing else in the codebase needs to change.

## Adding a new concrete family to an Abstract Factory

```csharp
public sealed class HighContrastThemeComponentFactory : IUiComponentFactory
{
    public IButton CreateButton() => new HighContrastButton();
    public ICheckbox CreateCheckbox() => new HighContrastCheckbox();
    public IScrollbar CreateScrollbar() => new HighContrastScrollbar();
}
```

This is Abstract Factory's cheap direction: a whole new family is one new class implementing the
existing `IUiComponentFactory` interface, with zero changes to `DarkThemeComponentFactory`,
`LightThemeComponentFactory`, or any consumer that depends on `IUiComponentFactory` rather than a
specific concrete factory.

## Adding a new product to an Abstract Factory's family (the expensive direction)

Adding `IMenu CreateMenu()` to `IUiComponentFactory` requires implementing `CreateMenu()` in **every**
existing concrete factory — `DarkThemeComponentFactory`, `LightThemeComponentFactory`, and
`HighContrastThemeComponentFactory` all fail to compile until updated, the same way an interface
member addition affects every implementer of any interface. This is unavoidable and is, in fact, the
guarantee working correctly: it is exactly what stops a family from silently missing a product it's
supposed to provide. Treat a compiler error here as the intended safety net, not friction to route
around with a default-implemented interface member unless every existing family genuinely has
nothing meaningful to produce for the new product — which is rare for Abstract Factory, since its
whole purpose is families that provide complete, consistent sets.

## Extending a generic `IFactory<TArg, T>` consumer

Because `IFactory<TArg, T>` is closed over concrete type arguments per registration, adding a new
factory for a different `T` is purely additive — a new class implementing `IFactory<TArg, T>` for
its own type arguments, registered independently of every other closed instantiation already in use.
No existing `IFactory<TArg, T>` implementation, nor the generic interface itself, needs to change.

## Registering a new factory implementation

Whichever shape is being extended, the new concrete factory (or new `switch` arm's target type)
needs to be registered or wired in wherever existing ones are — a DI registration line, a
composition root, or a factory-selector dictionary, exactly as covered in
[factory-registration-via-di.md](factory-registration-via-di.md). This registration step is the only
place outside the factory's own file that a purely additive extension (a new `switch` arm, a new
family) ever touches.
