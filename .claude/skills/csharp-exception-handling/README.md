# C# Exception Handling

Helps you write, review, or port C# exception-handling code: `try`/`catch`/`finally`, exception
filters, throw expressions, custom exception design, and patterns for `AggregateException` and
re-throwing across boundaries. Async-specific exception-unwrapping behavior (`await` vs.
`.Wait()`/`.Result`) is a separate, orthogonal concern this skill doesn't cover.

## When to reach for it

- Designing a custom exception type or exception hierarchy
- Choosing between `throw;` and `throw ex;` when re-throwing
- Writing an exception filter (`when` clause) instead of a nested `if` inside `catch`
- Unwrapping or flattening an `AggregateException` from parallel or blocking-`Task` code
- Asserting that a specific exception is thrown in a test

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting
`try`/`catch`/`finally` code, or designing exception types.

## What it covers

| Topic | Reference |
| --- | --- |
| `try`/`catch`/`finally` fundamentals and the `Exception`-derivation rule | [references/csharp1-try-catch-finally.md](references/csharp1-try-catch-finally.md) |
| `RuntimeWrappedException` auto-wrapping | [references/csharp2-runtimewrappedexception.md](references/csharp2-runtimewrappedexception.md) |
| Exception filters (the `when` clause) | [references/csharp6-exception-filters.md](references/csharp6-exception-filters.md) |
| `throw` as an expression | [references/csharp7-throw-expressions.md](references/csharp7-throw-expressions.md) |

## Example prompts

- "Should I use `throw;` or `throw ex;` here to preserve the stack trace?"
- "Write an exception filter that only catches this error when the status code is 429."
- "How do I flatten and inspect the inner exceptions of this `AggregateException`?"
