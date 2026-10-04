# Testing Your Test Infrastructure

xUnit itself runs *your* tests, but the test infrastructure you build on top of it — a custom
`DataAttribute`, a shared fixture, a custom `ITestOutputHelper`-based helper — is itself code that
can be wrong, and a bug there silently weakens or breaks every test built on it. Verify it directly
rather than trusting it because "the tests pass."

## Testing a custom `DataAttribute`

A custom `[Theory]` data source (a class deriving from `DataAttribute`, or a `MemberDataAttribute`-fed
method) should be tested for what data it actually yields — instantiate it directly and enumerate
its output, the same way you'd test any other method:

```csharp
public class OrderScenariosAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        yield return new object[] { 100m, 0m, 100m };
        yield return new object[] { 100m, 10m, 90m };
    }
}

[Fact]
public void OrderScenarios_YieldsExpectedCaseCount()
{
    var attribute = new OrderScenariosAttribute();

    var cases = attribute.GetData(null!).ToList();

    cases.Should().HaveCount(2);
    cases[1].Should().BeEquivalentTo(new object[] { 100m, 10m, 90m });
}
```

Testing the data source directly catches a bug in the source itself (a missing case, a
transposed expected value) that would otherwise only surface as a confusingly-wrong failure message
on whichever `[Theory]` consumes it, far from the actual defect.

## Testing a shared fixture's setup/teardown

A fixture implementing `IAsyncLifetime` (or a class-fixture used via `IClassFixture<T>`) has its own
setup and teardown logic that can fail silently if written incorrectly — e.g. a fixture that
appears to reset state but actually shares a mutable field across the tests using it:

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        ConnectionString = await TestDatabase.CreateAsync();
    }

    public Task DisposeAsync() => TestDatabase.DropAsync(ConnectionString);
}

[Fact]
public async Task DatabaseFixture_ProvidesAUsableConnectionString()
{
    await using var fixture = new DatabaseFixture();
    await fixture.InitializeAsync();

    fixture.ConnectionString.Should().NotBeNullOrEmpty();

    await using var connection = new SqlConnection(fixture.ConnectionString);
    await connection.OpenAsync(); // proves the string is actually usable, not just non-empty
}
```

Asserting the connection actually opens (not just that the string is non-empty) is what catches a
fixture that constructs a plausible-looking but broken connection string.

## Common pitfall: a fixture that leaks state between tests

`IClassFixture<T>` shares one fixture instance across every test in the class — a fixture holding
mutable state that one test modifies and a later test depends on being pristine produces test-order
-dependent failures that are hard to diagnose (tests pass individually, fail only in a full run, or
vice versa). Write a fixture-focused test that runs two independent operations against the same
fixture instance and asserts neither observes the other's side effects, if the fixture's own
correctness under reuse is ever in doubt.
