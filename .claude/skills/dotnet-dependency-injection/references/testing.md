# Testing with the container

## Prefer plain constructor injection in unit tests

For a genuine **unit test** — testing one class's logic in isolation — resolving the class under test
through a full `ServiceProvider` is almost always unnecessary indirection. Just construct it directly
with fakes/mocks/stubs passed to its constructor:

```csharp
[Fact]
public void Process_ThrowsWhenOrderIsInvalid()
{
    var repository = new FakeOrderRepository();
    var validator = new AlwaysFailingValidator();
    var sut = new OrderService(repository, validator); // no container involved at all

    Assert.Throws<ValidationException>(() => sut.Process());
}
```

This is faster (no container startup cost), and failures point directly at the class under test
rather than at a resolution-graph error somewhere in a shared test `ServiceCollection`. Reserve the
container itself for tests that are actually about the wiring — integration-style tests, and
tests that assert the registration graph itself is correct (see below).

## A minimal `ServiceCollection` for integration-style tests

When a test genuinely needs to exercise how several real registrations compose together (not just one
class in isolation), build a small, test-specific `ServiceCollection` rather than reusing the whole
application's `Program`/`Startup` registration — register only what the test needs, substituting
fakes for anything expensive or non-deterministic (external HTTP calls, real database connections,
wall-clock time):

```csharp
public static class TestServices
{
    public static ServiceProvider BuildForOrderProcessing()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IClock, FixedClock>();
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<OrderService>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}

[Fact]
public async Task OrderService_PersistsOrder()
{
    await using ServiceProvider provider = TestServices.BuildForOrderProcessing();
    using IServiceScope scope = provider.CreateScope();

    var sut = scope.ServiceProvider.GetRequiredService<OrderService>();
    await sut.PlaceAsync(new Order(/* ... */));

    var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    Assert.NotEmpty(repository.All());
}
```

Keep this registration list deliberately smaller than production's — a test-specific
`ServiceCollection` that only registers what a given test suite actually exercises makes it obvious,
from the registration list alone, exactly what's under test versus faked.

## Asserting the whole registration graph resolves (`ValidateOnBuild`)

A dedicated test (distinct from any test exercising actual behavior) that just builds the *real*
application's `IServiceCollection` with `ValidateOnBuild = true` catches "someone added a service that
depends on something unregistered" or "someone introduced a captive dependency" as a fast, isolated
test failure instead of a production startup crash:

```csharp
[Fact]
public void AllServices_CanBeResolved()
{
    var services = new ServiceCollection();
    Program.ConfigureServices(services); // however the app exposes its real registration logic

    using var provider = services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    });
    // BuildServiceProvider itself throws if anything can't be resolved — reaching this line is the assertion.
}
```

For this to work, structure the app's real startup so registration logic is callable independently of
actually running the host (a static method or small class taking `IServiceCollection`, called both
from `Program` and from this test) — if registration is only reachable by fully starting the host,
extract it first rather than starting the whole app just to validate the graph.

This test complements, rather than replaces, `ValidateOnBuild`/`ValidateScopes` being enabled in
Development at actual runtime (see [validation-and-errors.md](validation-and-errors.md)) — the test
gives fast, CI-visible feedback without needing to actually launch the app.
