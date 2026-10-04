---
name: csharp-exception-handling
description: Reference for C# exception handling — try/catch/finally fundamentals, the C# 1.0 rule that thrown/caught types derive from System.Exception (and the C# 2.0 RuntimeWrappedException change to what a catch clause can observe from non-C# callers), C# 6.0 exception filters (`when`), C# 7.0 throw expressions, custom exception design, AggregateException and flattening, ExceptionDispatchInfo and throw;-vs-throw-ex; re-throw patterns, and generic exception-handling helpers (Try<T>/Result<T>, generic retry-with-filter). Use when writing, reviewing, or porting try/catch/finally code, designing a custom exception type or hierarchy, choosing between throw; and throw ex;, handling AggregateException from parallel or blocking-Task code, re-throwing an exception across a thread boundary, writing an exception filter, or asserting thrown exceptions in tests. Covers C# 1.0 through the latest .NET 11 release candidate; note that async-specific exception-unwrapping behavior (await vs. .Wait()/.Result) is a separate concern from what this skill covers.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Exception Handling

One throughline: `try`/`catch`/`finally` and the rule that a thrown or caught type derives from
`System.Exception` have been in C# since 1.0 and still compile unchanged today; every later tier
adds either a new place to write `throw` (an expression, not just a statement) or a new way to
narrow which `catch` clause applies (a boolean `when` filter) without changing what a caught
exception fundamentally is. The C# 1.0 baseline in
[references/csharp1-try-catch-finally.md](references/csharp1-try-catch-finally.md) still compiles
unchanged on every later target. Note: `await`'s single-exception unwrapping versus
`.Wait()`/`.Result`'s `AggregateException` wrapping is async-specific behavior this skill doesn't
cover — it applies once code becomes `async`, orthogonal to the general exception-handling
mechanics documented here.

## Quick start (works everywhere, C# 1.0+)

```csharp
public static Order ParseOrder(string raw)
{
    try
    {
        return OrderParser.Parse(raw);
    }
    catch (FormatException ex)
    {
        throw new InvalidOperationException($"Order data was malformed: {raw}", ex);
    }
    finally
    {
        AuditLog.RecordParseAttempt(raw);
    }
}
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets, so pick the
highest tier you need and it points you downward as required.

| Target | C# language version | Reference file |
| --- | --- | --- |
| Any target | C# 1.0+ | [references/csharp1-try-catch-finally.md](references/csharp1-try-catch-finally.md) — `try`/`catch`/`finally`, the `Exception`-derivation rule; the universal baseline |
| .NET Framework 2.0+ / CLR 2.0+ | C# 2.0+ | [references/csharp2-runtimewrappedexception.md](references/csharp2-runtimewrappedexception.md) — `catch (Exception ex)` now observes non-CLS throws from other .NET languages via `RuntimeWrappedException` |
| VS 2015+ / .NET Framework 4.6+, .NET Core 1.x+ | C# 6.0+ | [references/csharp6-exception-filters.md](references/csharp6-exception-filters.md) — the `when` exception filter clause |
| VS 2017+ / .NET Framework 4.6.2+, .NET Core 1.x+ | C# 7.0+ | [references/csharp7-throw-expressions.md](references/csharp7-throw-expressions.md) — `throw` as an expression (`??`, `?:`, expression-bodied members) |

**C# 3.0–5.0 and C# 8–15 note:** none of these releases changed exception-handling syntax or
availability — there's no `references/csharp3-*.md` through `csharp5-*.md`, or `csharp8-*.md`
through `csharp15-*.md` file, because nothing in this domain shipped in those versions. Pattern
matching used *inside* a `when` filter (`when (ex is ArgumentException or FormatException)`) is an
application of C#'s general pattern-matching syntax, not a distinct exception-handling feature —
see [specialized/exception-filters-in-depth.md](specialized/exception-filters-in-depth.md).
`AggregateException` and `ExceptionDispatchInfo` are BCL types gated by target-framework version
(.NET Framework 4.0 and 4.5, respectively), not C# language version — see their specialized files
below for that distinction.

## Specialized patterns

- [specialized/exception-filters-in-depth.md](specialized/exception-filters-in-depth.md) — the `when` clause runs before stack unwinding, the logging-and-rethrow anti-pattern it replaces, and pattern-matching syntax inside a filter
- [specialized/custom-exception-design.md](specialized/custom-exception-design.md) — when to create a custom exception type, the standard constructor shape, hierarchy design, and current (post-.NET 8) serialization guidance
- [specialized/aggregateexception-and-flattening.md](specialized/aggregateexception-and-flattening.md) — `AggregateException` from parallel/blocking-Task code, `Flatten()`, and `Handle()`
- [specialized/exceptiondispatchinfo-and-rethrow-patterns.md](specialized/exceptiondispatchinfo-and-rethrow-patterns.md) — `throw;` vs. `throw ex;`, and `ExceptionDispatchInfo` for re-throwing across a thread/queue boundary with the original stack trace intact
- [specialized/generic-exception-handling-helpers.md](specialized/generic-exception-handling-helpers.md) — a generic `Result<T>`/`Try<T>` wrapper and a generic retry helper built on an exception filter
- [specialized/testing-with-exception-handling.md](specialized/testing-with-exception-handling.md) — exception handling as a test-authoring concern: `Assert.Throws<T>`/`Assert.ThrowsAsync<T>` across frameworks, asserting on exception properties (not just type/message text), asserting an exception was *not* thrown, and custom exception-assertion helpers
