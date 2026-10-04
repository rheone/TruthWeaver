# Testing Code Built on a Factory

A factory's contract is "given this input, produce that output" — testing one means calling `Create`
and asserting on what came back: its concrete type, its state, or both. Testing a consumer of a
factory means substituting a factory whose output you control, so the consumer's test never depends
on the factory's own creation logic.

## Testing a factory's output directly

```csharp
public class NotificationFactoryTests
{
    private readonly NotificationFactory _factory = new();

    [Fact]
    public void Create_ForEmailChannel_ReturnsEmailNotification()
    {
        var request = new NotificationRequest(NotificationChannel.Email, "user@example.com", "Hi");

        var result = _factory.Create(request);

        var email = Assert.IsType<EmailNotification>(result);
        Assert.Equal("user@example.com", email.Recipient);
        Assert.Equal("Hi", email.Body);
    }

    [Fact]
    public void Create_ForUnsupportedChannel_Throws()
    {
        var request = new NotificationRequest((NotificationChannel)999, "user@example.com", "Hi");

        Assert.Throws<NotSupportedException>(() => _factory.Create(request));
    }
}
```

Asserting `Assert.IsType<EmailNotification>(result)` — not just that `result` is non-null — is the
core factory assertion: a factory test's job is specifically to confirm the *correct concrete type*
came back for a given input, since that mapping is the entire behavior the factory exists to
encapsulate. A test that only checks for a non-null `INotification` would pass even if the factory
returned the wrong channel's implementation.

## Testing an Abstract Factory's family consistency

```csharp
public class DarkThemeComponentFactoryTests
{
    private readonly DarkThemeComponentFactory _factory = new();

    [Fact]
    public void CreateButton_ReturnsDarkButton() =>
        Assert.IsType<DarkButton>(_factory.CreateButton());

    [Fact]
    public void CreateCheckbox_ReturnsDarkCheckbox() =>
        Assert.IsType<DarkCheckbox>(_factory.CreateCheckbox());

    [Fact]
    public void CreateScrollbar_ReturnsDarkScrollbar() =>
        Assert.IsType<DarkScrollbar>(_factory.CreateScrollbar());
}
```

Test every product method for every concrete family — the actual invariant Abstract Factory
protects (every product from one call to the same factory instance belongs to the same family) is
only demonstrated by confirming each product method independently returns the family-correct type;
a single test that only checks `CreateButton` doesn't demonstrate the other two products are
consistent.

## Testing a consumer with a fake factory

```csharp
public class NotificationSenderTests
{
    [Fact]
    public void Send_DispatchesWhateverTheFactoryProduces()
    {
        var notification = new RecordingNotification();
        var factory = new FixedResultNotificationFactory(notification);
        var sender = new NotificationSender(factory);

        sender.Send(new NotificationRequest(NotificationChannel.Email, "user@example.com", "Hi"));

        Assert.True(notification.WasDispatched);
    }

    private sealed class FixedResultNotificationFactory : INotificationFactory
    {
        private readonly INotification _result;
        public FixedResultNotificationFactory(INotification result) => _result = result;
        public INotification Create(NotificationRequest request) => _result;
    }

    private sealed class RecordingNotification : INotification
    {
        public bool WasDispatched { get; private set; }
        public void Dispatch() => WasDispatched = true;
    }
}
```

This test never constructs a real `EmailNotification`, `SmsNotification`, or any real channel logic
— it only confirms `NotificationSender` dispatches whatever its factory hands it, which is the whole
of `NotificationSender`'s own responsibility. The factory's channel-selection logic belongs entirely
to the factory's own tests above, and should never need to be re-verified from inside a consumer
test.

## Testing a factory delegate (`Func<>`)

```csharp
[Fact]
public void SendWelcomeEmail_UsesInjectedFactoryDelegate()
{
    var recordedArgs = new List<(string Recipient, string Body)>();
    Func<string, string, EmailNotification> createEmail = (recipient, body) =>
    {
        recordedArgs.Add((recipient, body));
        return new EmailNotification(recipient, body);
    };
    var sender = new NotificationSender(createEmail);

    sender.SendWelcomeEmail("user@example.com");

    Assert.Single(recordedArgs);
    Assert.Equal(("user@example.com", "Welcome!"), recordedArgs[0]);
}
```

A delegate-based factory needs no fake class — the test's own lambda both records what it was called
with and produces whatever result the test wants, exactly as a delegate-based strategy's test does.
