# Exception Filters — the `when` Clause (C# 6.0)

C# 6.0 shipped July 2015 with Visual Studio 2015 and added the `when` keyword to `catch` clauses:
a boolean expression, evaluated after the exception's declared type matches but **before the
stack unwinds**, that decides whether that clause handles the exception. The underlying CLR
capability had existed since CLR 1.0 (Visual Basic and F# could already express it via IL); C# 6.0
was the first C# release to expose it as syntax. A filter that evaluates to `false` lets the
search for a handler continue exactly as if that `catch` clause's type hadn't matched at all —
including moving on to a later `catch` clause for the *same* exception type with a different
filter.

## Syntax

```csharp
catch (ExceptionType ex) when (booleanExpression)
{
    // runs only if the type matches AND booleanExpression is true
}
```

## Basic use case

```csharp
public static void ProcessPayment(Payment payment)
{
    try
    {
        _gateway.Charge(payment);
    }
    catch (GatewayException ex) when (ex.StatusCode == 429)
    {
        _retryQueue.Enqueue(payment); // rate-limited: retry later
    }
    catch (GatewayException ex) when (ex.StatusCode >= 500)
    {
        _retryQueue.Enqueue(payment); // transient server error: also retry
    }
    catch (GatewayException ex)
    {
        throw new PaymentFailedException($"Payment for {payment.Id} was rejected.", ex);
    }
}
```

Several `catch` clauses can share the same exception type as long as each (but possibly the last)
carries a distinguishing `when` filter — the runtime tries them top to bottom, and the first clause
whose type matches and whose filter (if any) returns `true` handles the exception.

## Advanced use case: filtering without discarding diagnostic detail

```csharp
public static Order LoadOrder(int orderId)
{
    try
    {
        return _repository.Load(orderId);
    }
    catch (SqlException ex) when (LogAndContinue(ex, orderId))
    {
        throw; // never actually reached: LogAndContinue always returns false
    }

    static bool LogAndContinue(SqlException ex, int orderId)
    {
        Logger.LogError(ex, "Failed to load order {OrderId}", orderId);
        return false; // "handle" nothing -- just observe, then let the search continue
    }
}
```

A filter method that always returns `false` is a legitimate pattern for side-effecting on an
exception (logging it with full context) without actually handling it — see
[../specialized/exception-filters-in-depth.md](../specialized/exception-filters-in-depth.md) for
why this is strictly better than a `catch { Log(ex); throw; }` block for that purpose.

## Requirements and restrictions

- The filter expression must be of type `bool` (or implicitly convertible to it) and runs with the
  exception's fields and the call stack at the point of the `throw` still intact — no stack
  unwinding happens unless some clause's filter (or an unguarded clause) actually matches.
  Debuggers configured to break on a thrown exception (not just an unhandled one) will show the
  original throw site while a filter is evaluating.
  See [../specialized/exception-filters-in-depth.md](../specialized/exception-filters-in-depth.md)
  for the full implications of this ordering.
- A filter can call any method, including one with side effects (as in the advanced example above)
  — the compiler places no restriction on what a filter expression contains beyond it being a
  valid `bool` expression.
- If a `catch` clause has a filter, it's allowed to appear before a broader unfiltered clause for
  the same or a less-derived exception type — filters change the normal "most-derived-first"
  ordering requirement from [csharp1-try-catch-finally.md](csharp1-try-catch-finally.md), because
  the filter (not just the type) now disambiguates which clause applies.
- This tier only adds the `when` clause itself — pattern matching *inside* a filter expression
  (`when (ex is ArgumentException or FormatException)`) depends on the general pattern-matching
  features of whichever C# version is targeted, not on anything specific to exception handling;
  see [../specialized/exception-filters-in-depth.md](../specialized/exception-filters-in-depth.md)
  for what that looks like in practice.

## Fallback

Below C# 6.0, there's no `when` clause — express the same condition as an `if` inside an unguarded
`catch (ExceptionType ex)` block, and re-`throw;` when the condition doesn't apply:

```csharp
catch (GatewayException ex)
{
    if (ex.StatusCode != 429)
    {
        throw;
    }
    _retryQueue.Enqueue(payment);
}
```

The key behavioral difference to be aware of when porting *forward* out of this workaround: the
`if`-inside-`catch` form has already unwound the stack by the time the condition is evaluated, so
any debugging or diagnostic value that depended on the original stack state is already lost — see
[csharp1-try-catch-finally.md](csharp1-try-catch-finally.md) for the baseline `catch` behavior this
falls back to.
