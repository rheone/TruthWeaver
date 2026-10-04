# Test Lifecycle

## The per-test-method model

xUnit constructs a brand-new instance of the test class for **every** test method it runs — not
once per class, not once per suite. You use the constructor for setup that every test in the class
needs, and `IDisposable.Dispose()` for the matching teardown:

```csharp
public class DatabaseConnectionTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseConnectionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public void Connection_StartsOpen()
    {
        Assert.Equal(ConnectionState.Open, _connection.State);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
```

Because a fresh instance backs every test, fields you set in the constructor never leak state
between tests in the same class — you get test isolation without writing a reset step yourself.
This is xUnit's own lifecycle model: constructor for per-test setup, `Dispose()` for per-test
teardown, with no separate "setup" or "teardown" attribute to apply, and no shared mutable instance
across test methods to guard against.

## `IAsyncLifetime` for async setup/teardown

When setup or teardown needs to `await` (opening a connection asynchronously, starting a
container), implement `IAsyncLifetime` instead of relying on the constructor and `Dispose()`:

```csharp
public class ApiClientTests : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        _client = new HttpClient();
        await _client.GetAsync("https://example.test/warmup");
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetOrders_ReturnsSuccessStatus()
    {
        var response = await _client.GetAsync("https://example.test/orders");

        Assert.True(response.IsSuccessStatusCode);
    }
}
```

xUnit calls `InitializeAsync()` after construction and before the first test method, and
`DisposeAsync()` after the last test method in that instance completes. A constructor cannot
`await`, which is the reason this interface exists rather than an async constructor.

## `IClassFixture<T>` — shared context across tests in one class

When setup is expensive and safe to share read-only across every test in a class (spinning up a
fixture object once rather than per test), implement `IClassFixture<TFixture>`:

```csharp
public class DatabaseFixture : IDisposable
{
    public SqliteConnection Connection { get; }

    public DatabaseFixture()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
    }

    public void Dispose() => Connection.Dispose();
}

public class OrderRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public OrderRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Fixture_ConnectionIsOpen()
    {
        Assert.Equal(ConnectionState.Open, _fixture.Connection.State);
    }
}
```

xUnit constructs `DatabaseFixture` exactly once per test class run, then injects the same instance
into the constructor of every test-class instance it creates for that class's tests. Because the
test class itself is still reconstructed per test, your own fields stay isolated — only the fixture
instance is shared, so mutate it with care: a test that leaves shared fixture state dirty can affect
tests that run after it within the same class.

## `ICollectionFixture<T>` — shared context across multiple classes

When several test classes need to share one expensive fixture (one database, one hosted server),
define a collection and implement `ICollectionFixture<TFixture>` on a marker class, then decorate
every participating test class with `[Collection("...")]`:

```csharp
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

[Collection("Database collection")]
public class OrderRepositoryTests
{
    public OrderRepositoryTests(DatabaseFixture fixture) { /* ... */ }
}

[Collection("Database collection")]
public class CustomerRepositoryTests
{
    public CustomerRepositoryTests(DatabaseFixture fixture) { /* ... */ }
}
```

xUnit constructs `DatabaseFixture` once for the entire collection, shares it across every class
tagged with that collection name, and disposes it once after the last test in the collection
finishes. Classes in the same collection never run their tests in parallel with each other — see
[parallelization-and-collections.md](parallelization-and-collections.md) for what that means for
test ordering and isolation.
