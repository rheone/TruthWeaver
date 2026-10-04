# Try/Catch/Finally Baseline (C# 1.0)

C# 1.0 shipped January 2002 with Visual Studio .NET 2002 and defined exception handling exactly
as it still works today at its core: a `try` block, zero or more typed `catch` clauses evaluated
top to bottom, and an optional `finally` block that runs on every path out of `try` (normal
completion, a `return`/`break`/`continue`/`goto`, or an unhandled exception propagating through).
The language rule from day one: a `throw` statement's operand, and a `catch` clause's declared
type, must be (or derive from) `System.Exception`. This is the baseline every later tier in this
skill still builds on unchanged.

## Syntax

```csharp
try
{
    // code that might throw
}
catch (SpecificException ex)
{
    // handle SpecificException and anything derived from it
}
catch (Exception ex)
{
    // handle anything else derived from System.Exception
}
finally
{
    // always runs: normal completion, early exit, or propagating exception
}
```

## Basic use case

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

A `catch` clause with no declared variable (`catch (FormatException) { ... }`) is legal when the
exception object itself isn't needed. A `catch` clause with no type at all (`catch { ... }`) is
also legal — as of C# 2.0 it catches everything a typed `catch (Exception)` would, but see
[csharp2-runtimewrappedexception.md](csharp2-runtimewrappedexception.md) for why that wasn't
always true and what changed.

## Advanced use case: multiple typed catches, ordered most-derived first

```csharp
public static void SaveOrder(Order order)
{
    try
    {
        Database.Insert(order);
    }
    catch (DuplicateKeyException ex) // derives from DatabaseException
    {
        throw new InvalidOperationException($"Order {order.Id} already exists.", ex);
    }
    catch (DatabaseException ex)
    {
        throw new InvalidOperationException("Order could not be saved.", ex);
    }
    finally
    {
        order.LastSaveAttempt = DateTime.UtcNow;
    }
}
```

The compiler rejects a `catch` clause placed after a `catch` for one of its base types — reaching
that clause would be provably impossible, so the more-derived type must come first.

## Requirements and restrictions

- Every `catch` clause's declared type must be `System.Exception` or a type derived from it; the
  compiler enforces this at compile time. This has never changed in C#, at any version — what has
  changed, in C# 2.0, is whether something *not* derived from `Exception` can reach a C# `catch`
  clause at all when it originates from a non-C# caller. See
  [csharp2-runtimewrappedexception.md](csharp2-runtimewrappedexception.md).
- A `catch` clause with no exception type present matches anything that reaches it and, if used,
  must be the last clause.
- `finally` runs even if a `catch` block itself throws, and even if the `try` block exits via
  `return` — the only cases it's skipped are abrupt process termination (`Environment.FailFast`,
  a fatal runtime error such as `StackOverflowException`) rather than any code-level exit path.
- Unlike some other C-family languages, C# has no exception specification on method signatures —
  any method can throw any `Exception`-derived type without declaring it.

## Fallback

This is the first tier — there is no earlier C# version and no workaround to fall back to.
Exception handling via `try`/`catch`/`finally` is part of C# 1.0 itself.
