# NSubstitute

NSubstitute is a third-party mocking library for .NET with a fluent, non-recording API for creating
test doubles. This skill covers creating substitutes, configuring what they return, matching
arguments, and verifying which calls a substitute actually received.

## When to reach for it

- Creating a test double for an interface, class, or delegate a unit under test depends on.
- Configuring a substitute member to return a specific value, throw, or return different values on
  successive calls.
- Asserting whether a member was called, how many times, or with which arguments.
- A substitute isn't returning what you configured, or a verification always fails for reasons that
  aren't obvious.
- Verifying a shared substitute-factory helper or a custom argument matcher you wrote works
  correctly.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing or
debugging test code that creates or verifies calls against an NSubstitute substitute. You can also
invoke it directly as `/dotnet-nsubstitute`.

## What it covers

| Topic | Reference |
| --- | --- |
| Substitute.For for interfaces, classes, delegates | [references/core-concepts.md](references/core-concepts.md) |
| Returns, ReturnsForAnyArgs, sequences, throwing | [references/configuring-return-values.md](references/configuring-return-values.md) |
| Arg.Any, Arg.Is, Arg.Do, compound matchers | [references/argument-matchers.md](references/argument-matchers.md) |
| Received, DidNotReceive, ReceivedWithAnyArgs | [references/verifying-calls.md](references/verifying-calls.md) |
| Configuring and verifying async members | [references/async-support.md](references/async-support.md) |
| Substitute.ForPartsOf and CallBase | [references/partial-substitutes.md](references/partial-substitutes.md) |
| Common mistakes with matchers, non-virtual members, over-verification | [references/common-pitfalls.md](references/common-pitfalls.md) |
| Testing shared substitute-factory helpers and custom matchers | [references/testing-your-test-doubles.md](references/testing-your-test-doubles.md) |

## Example prompts

- "Create a substitute for IOrderRepository that returns a specific order for one ID."
- "Verify this service called Save exactly once with an order whose status is Completed."
- "Why does this Received() assertion keep failing even though the method was clearly called?"
