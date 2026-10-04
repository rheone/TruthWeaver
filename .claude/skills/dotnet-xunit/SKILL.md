---
name: dotnet-xunit
description: Guidance on xUnit.net, a third-party unit testing framework for C#/.NET (current stable release xunit.v3 4.0.1; the legacy xunit v2 line is still widely deployed). Covers [Fact]/[Theory] test discovery, the constructor/IDisposable per-test lifecycle model, IClassFixture/ICollectionFixture shared-context fixtures, data-driven tests via InlineData/MemberData/ClassData, the Assert.* assertion catalog and fluent assertion library alternatives, and test collection/parallelization behavior. Use when writing, reviewing, or debugging xUnit test classes, choosing between Fact and Theory, wiring up shared setup/teardown across tests, or diagnosing why tests run in an unexpected order or in parallel.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# xUnit.net

Guidance on xUnit.net, a third-party unit testing framework for .NET. Current stable release as of
this writing: **xunit.v3 4.0.1** (core framework 3.2.2). Organized by concern/topic, not by xUnit
version — xUnit's attribute-based model has been stable across its major lines, so each reference
file notes a version-specific fact inline rather than splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Writing your first test, or deciding Fact vs. Theory | `[Fact]`, `[Theory]`, test discovery, project setup | [references/core-concepts.md](references/core-concepts.md) |
| Sharing setup/teardown across tests in a class or across classes | Constructor/`IDisposable`, `IClassFixture<T>`, `ICollectionFixture<T>`, `[Collection]` | [references/test-lifecycle.md](references/test-lifecycle.md) |
| Running the same test logic against multiple inputs | `[InlineData]`, `[MemberData]`, `[ClassData]` | [references/data-driven-tests.md](references/data-driven-tests.md) |
| Choosing what to assert with, and how | `Assert.*` catalog, fluent assertion library options | [references/assertions.md](references/assertions.md) |
| Tests are running in an unexpected order, colliding, or you need to isolate/serialize a group | Test collections, `[Collection]`, parallelization, `ITestOutputHelper` | [references/parallelization-and-collections.md](references/parallelization-and-collections.md) |
| Verifying a custom `DataAttribute` or shared fixture actually behaves correctly | Testing data sources directly, testing fixture setup/teardown | [references/testing-your-test-infrastructure.md](references/testing-your-test-infrastructure.md) |

## Quick start

```csharp
public class OrderCalculatorTests
{
    [Fact]
    public void Total_WithNoDiscount_ReturnsSubtotal()
    {
        var calculator = new OrderCalculator();

        var total = calculator.CalculateTotal(subtotal: 100m, discountPercent: 0m);

        Assert.Equal(100m, total);
    }

    [Theory]
    [InlineData(100, 10, 90)]
    [InlineData(50, 50, 25)]
    public void Total_WithDiscount_AppliesPercentage(decimal subtotal, decimal discountPercent, decimal expected)
    {
        var calculator = new OrderCalculator();

        var total = calculator.CalculateTotal(subtotal, discountPercent);

        Assert.Equal(expected, total);
    }
}
```

xUnit discovers every public method carrying `[Fact]` or `[Theory]` in every public class in the
test assembly automatically — you register nothing, and you add no test-suite boilerplate beyond
the attribute itself.

A fluent assertion library you pair with xUnit is a separate package researched on its own terms —
see [references/assertions.md](references/assertions.md), which flags that some carry a
non-standard license.

## Out of scope

- Third-party fluent assertion libraries' full API surface — [references/assertions.md](references/assertions.md)
  covers only what's relevant to choosing one, not each library's complete method catalog.
- Mocking/substitution libraries used inside test bodies — orthogonal to xUnit's own job of
  discovering, running, and reporting tests.
- Database or external-resource reset/cleanup strategies between tests — a narrow, deep domain of
  its own with different failure modes (connection lifetime, transactional isolation) than xUnit's
  test-execution model.
- `xunit.v3`'s stand-alone-executable architecture and build/CI wiring details beyond what
  [references/core-concepts.md](references/core-concepts.md) needs to explain discovery.
