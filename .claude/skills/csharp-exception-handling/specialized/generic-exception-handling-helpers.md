# Generic Exception-Handling Helpers

Generic helper types and methods that wrap `try`/`catch` for reuse across unrelated call sites —
a `Result<T>`/`Try<T>` outcome wrapper, and a generic retry helper built on an exception filter.
This file assumes familiarity with generic type/method syntax itself; it only covers how exception
handling composes with it. Builds on
[../references/csharp1-try-catch-finally.md](../references/csharp1-try-catch-finally.md) and
[../references/csharp6-exception-filters.md](../references/csharp6-exception-filters.md).

## Basic: a generic `Result<T>` that turns a caught exception into a return value

```csharp
public readonly struct Result<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public Exception? Error { get; }

    private Result(bool success, T? value, Exception? error)
    {
        Success = success;
        Value = value;
        Error = error;
    }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(Exception error) => new(false, default, error);

    public static Result<T> Try(Func<T> operation)
    {
        try
        {
            return Ok(operation());
        }
        catch (Exception ex)
        {
            return Fail(ex);
        }
    }
}
```

```csharp
Result<Order> result = Result<Order>.Try(() => OrderParser.Parse(raw));
if (!result.Success)
{
    Logger.LogWarning(result.Error, "Order parse failed.");
    return;
}
ProcessOrder(result.Value!);
```

`Result<T>.Try` is generic over the *return type* of the operation, not over the exception type —
it deliberately catches `Exception` broadly, on the premise that any failure of `operation` should
become a `Result<T>.Fail` rather than propagate. This trades the caller's ability to selectively
handle specific exception types for a uniform, allocation-light way to express "this might fail" in
call sites that don't want `try`/`catch` at every use.

## Advanced: a generic retry helper parameterized over which exception to retry on

```csharp
public static class Retry
{
    public static T OnException<T, TException>(
        Func<T> operation,
        int maxAttempts,
        Func<TException, bool>? shouldRetry = null)
        where TException : Exception
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return operation();
            }
            catch (TException ex) when (attempt < maxAttempts && (shouldRetry is null || shouldRetry(ex)))
            {
                Logger.LogWarning(ex, "Attempt {Attempt} of {MaxAttempts} failed, retrying.", attempt, maxAttempts);
            }
        }
    }
}
```

```csharp
Order order = Retry.OnException<SqlException>(
    () => _repository.Load(orderId),
    maxAttempts: 3,
    shouldRetry: ex => ex.Number is 1205 or -2); // deadlock or timeout error numbers
```

`TException` is constrained to `Exception` (required — see
[csharp1-try-catch-finally.md](../references/csharp1-try-catch-finally.md), since a `catch` clause
can never declare an unconstrained type parameter otherwise), and the `when` filter combines the
generic caller-supplied `shouldRetry` predicate with the attempt-count check — the filter runs
before any unwinding, so `Retry.OnException`'s own stack frame is still live when `shouldRetry`
inspects the exception, exactly as it would be for any other exception filter (see
[exception-filters-in-depth.md](exception-filters-in-depth.md)).

A generic method's type parameter can be used directly as a `catch` clause's exception type (as
`TException` is here) as long as it's constrained to `Exception` or a subtype — an unconstrained
type parameter cannot appear in a `catch` clause, since the compiler can't verify it derives from
`Exception` at the declaration site.

## Fallback

Both patterns compile on [C# 1.0](../references/csharp1-try-catch-finally.md) as far as generics
and `try`/`catch` are concerned; the retry helper's `when` filter specifically needs
[C# 6.0](../references/csharp6-exception-filters.md). Below C# 6.0, move the combined
attempt-count/`shouldRetry` condition inside the `catch` block and re-`throw;` when it's false:

```csharp
catch (TException ex)
{
    if (attempt >= maxAttempts || (shouldRetry != null && !shouldRetry(ex)))
    {
        throw;
    }
    Logger.LogWarning(ex, "Attempt {Attempt} of {MaxAttempts} failed, retrying.", attempt, maxAttempts);
}
```
