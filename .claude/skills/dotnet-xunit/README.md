# xUnit.net

xUnit.net is a third-party unit testing framework for .NET built around plain attributes rather than
inherited base classes. This skill covers writing `[Fact]` and `[Theory]` tests, sharing setup and
teardown across tests, and diagnosing tests that run in an unexpected order or interfere with each
other under parallel execution.

## When to reach for it

- Writing your first test in a class and deciding between `[Fact]` and `[Theory]`.
- Sharing expensive setup across tests in a class, or across multiple test classes.
- Running the same test logic against a table of inputs with `[InlineData]` or `[MemberData]`.
- Tests fail intermittently, run in an order you didn't expect, or seem to interfere with each other.
- Verifying that a custom `DataAttribute` or shared fixture you wrote actually behaves correctly.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing or
reviewing xUnit test classes. You can also invoke it directly as `/dotnet-xunit`.

## What it covers

| Topic | Reference |
| --- | --- |
| Fact/Theory, test discovery, project setup | [references/core-concepts.md](references/core-concepts.md) |
| Constructor/IDisposable lifecycle, IClassFixture, ICollectionFixture | [references/test-lifecycle.md](references/test-lifecycle.md) |
| InlineData, MemberData, ClassData | [references/data-driven-tests.md](references/data-driven-tests.md) |
| The Assert catalog and fluent assertion alternatives | [references/assertions.md](references/assertions.md) |
| Test collections and parallelization behavior | [references/parallelization-and-collections.md](references/parallelization-and-collections.md) |
| Verifying a custom data attribute or fixture behaves correctly | [references/testing-your-test-infrastructure.md](references/testing-your-test-infrastructure.md) |

## Example prompts

- "Should this test be a Fact or a Theory, given it only asserts one case right now?"
- "Set up an IClassFixture so these tests share one expensive setup instead of rebuilding it per test."
- "These tests pass individually but fail when run together: what's colliding?"
