# Parallelization and Collections

## Default parallelization behavior

xUnit runs test **classes** in parallel against each other by default, within a single test
assembly. Test methods within the same class run sequentially against each other. This is the
opposite default of frameworks that run everything sequentially unless you opt in to parallelism —
with xUnit, isolation between classes is something you verify holds, not something you add.

This default is why shared mutable state (a static field, a file on disk, a fixed external
resource) touched by tests in two different classes is a common source of flaky failures: those two
classes may genuinely execute at the same moment.

## Test collections

Every test class belongs to a **collection**. By default, xUnit puts each class into its own
implicit, unnamed collection, which is why classes run in parallel with each other — they're in
different collections. Classes explicitly tagged with the same `[Collection("name")]` attribute
join one named collection, and **classes in the same collection never run their tests in parallel
with each other** — xUnit runs a collection's tests sequentially with respect to other tests in
that same collection.

You use this to serialize any group of test classes that must not run concurrently: classes
sharing a fixture via `ICollectionFixture<T>` are automatically in the same collection (see
[test-lifecycle.md](test-lifecycle.md)), and you can put unrelated classes in the same named
collection purely to force sequential execution when they touch a shared external resource with no
fixture involved:

```csharp
[Collection("Serial")]
public class FirstSerialTests { /* ... */ }

[Collection("Serial")]
public class SecondSerialTests { /* ... */ }
```

## Disabling parallelization

To turn off cross-class parallelism for an entire assembly, add an assembly-level attribute:

```csharp
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

This runs every test class in the assembly sequentially, which is the right call when a suite has
enough shared-state coupling that fixing it individually costs more than the slower run time buys
back — treat it as a deliberate, assembly-wide decision rather than a per-test workaround.

`CollectionBehavior` also controls how many collections run concurrently
(`MaxParallelThreads`) and whether test classes default into one collection per class or one
collection per test assembly (`CollectionBehavior.CollectionPerAssembly`).

## `ITestOutputHelper`

Writing to `Console.WriteLine` from inside a test does not reliably appear in the runner's output
when tests run in parallel, because output from concurrently-running tests interleaves. Accept an
`ITestOutputHelper` through the test class constructor instead — xUnit injects it, and it attributes
output to the correct test regardless of what else is running concurrently:

```csharp
public class OrderCalculatorTests
{
    private readonly ITestOutputHelper _output;

    public OrderCalculatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Total_LogsIntermediateValue()
    {
        var total = 100m * 0.9m;
        _output.WriteLine($"Computed total: {total}");

        Assert.Equal(90m, total);
    }
}
```
