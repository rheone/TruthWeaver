# `throw;` vs. `throw ex;`, and `ExceptionDispatchInfo` Across Boundaries

Two related re-throw concerns: the classic `throw;`-vs-`throw ex;` stack-trace distinction (a C#
1.0 fact, not gated by any later version), and `ExceptionDispatchInfo` — a BCL type in
`System.Runtime.ExceptionServices`, shipped in .NET Framework 4.5, not a C# language feature — for
preserving an exception's original stack trace when it must be re-thrown somewhere other than its
original `catch` block, such as after crossing a thread or queuing boundary. Builds on
[../references/csharp1-try-catch-finally.md](../references/csharp1-try-catch-finally.md).

## Basic: `throw;` preserves the original stack trace, `throw ex;` overwrites it

```csharp
try
{
    ParseOrder(raw);
}
catch (FormatException ex)
{
    Logger.LogError(ex, "Parse failed.");
    throw; // preserves ex.StackTrace as originally captured at the throw site inside ParseOrder
}
```

```csharp
try
{
    ParseOrder(raw);
}
catch (FormatException ex)
{
    Logger.LogError(ex, "Parse failed.");
    throw ex; // resets ex.StackTrace to start from THIS line -- the original throw site inside
              // ParseOrder is lost from the stack trace an outer handler or crash report will see
}
```

`throw;` (no operand) re-raises the exception currently being handled without touching its
`StackTrace` property at all. `throw ex;` is a completely ordinary throw statement whose operand
happens to be the caught variable — the runtime treats it exactly like throwing a brand new
exception from that line, so the stack trace captured up to `ParseOrder`'s original failure point
is discarded and replaced with a trace starting at the `throw ex;` line itself. `throw;` is correct
whenever the goal is "propagate this same failure onward"; `throw ex;` is appropriate only when
deliberately re-anchoring the trace to the current location is the intent (rare).

## Advanced: `ExceptionDispatchInfo` re-throws across a boundary `throw;` cannot cross

```csharp
using System.Runtime.ExceptionServices;

private static ExceptionDispatchInfo? _capturedFailure;

public static void RunOnWorkerThread(Action work)
{
    var thread = new Thread(() =>
    {
        try
        {
            work();
        }
        catch (Exception ex)
        {
            // capture here, on the worker thread, at the point of the original failure
            _capturedFailure = ExceptionDispatchInfo.Capture(ex);
        }
    });
    thread.Start();
    thread.Join();

    _capturedFailure?.Throw(); // re-thrown here, on the calling thread -- but with the ORIGINAL
                                // stack trace preserved and appended to, not replaced
}
```

`throw;` only works inside the `catch` block that's currently handling the exception — it has
nothing to re-throw once execution has left that block, crossed to another thread, or been queued
for later. `ExceptionDispatchInfo.Capture(ex)` snapshots the exception's full state (stack trace,
`Data`, and diagnostic information the runtime tracks internally) at the point it's captured;
`.Throw()` later re-raises it as if it had propagated continuously from the original capture point
to wherever `.Throw()` is called — the original stack trace is preserved and the frames between
capture and `.Throw()` are appended to it, rather than discarded the way `throw ex;` would discard
them.

## Advanced: re-throwing the same captured exception from multiple places

```csharp
public static void ValidateAndCapture(Order order, List<ExceptionDispatchInfo> failures)
{
    try
    {
        Validate(order);
    }
    catch (ValidationException ex)
    {
        failures.Add(ExceptionDispatchInfo.Capture(ex)); // capture now, decide whether to throw later
    }
}

// ... elsewhere, once all validations have run:
if (failures.Count == 1)
{
    failures[0].Throw(); // re-throw the single failure with its original stack trace intact
}
else if (failures.Count > 1)
{
    throw new AggregateException(failures.Select(f => f.SourceException));
}
```

`ExceptionDispatchInfo.Capture` is also useful purely as a way to defer the decision of *whether*
to re-throw — collecting several captured failures and deciding afterward whether to propagate one,
several (via `AggregateException`; see
[aggregateexception-and-flattening.md](aggregateexception-and-flattening.md)), or none.

## Fallback

`throw;` vs. `throw ex;` has worked exactly as described since
[C# 1.0](../references/csharp1-try-catch-finally.md) — no version gate at all.
`ExceptionDispatchInfo` requires .NET Framework 4.5 or later as a BCL dependency (a
target-framework requirement, not a C# language-version one — the language-vs-BCL distinction
matters here the same way it does for
[AggregateException](aggregateexception-and-flattening.md)). Below .NET Framework 4.5, there's no
way to preserve an original stack trace across a thread/queue boundary using the BCL alone; the
closest workaround is capturing the formatted `ex.ToString()` (which includes the stack trace as
text) for diagnostic purposes, then throwing a new exception on the far side of the boundary with
that text embedded in its message — a real exception object with the original trace and type
cannot be reconstructed that way, only approximated for logging.
