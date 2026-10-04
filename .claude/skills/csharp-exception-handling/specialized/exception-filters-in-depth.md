# Exception Filters in Depth

This file expands on the `when` clause introduced in
[../references/csharp6-exception-filters.md](../references/csharp6-exception-filters.md): the
consequence of it running before stack unwinding, how it composes with general pattern-matching
syntax, and the logging-and-rethrow anti-pattern it replaces.

## Basic: filters run before the stack unwinds — why that matters for diagnostics

```csharp
public static void Handle()
{
    try
    {
        DoWork();
    }
    catch (InvalidOperationException ex) when (ShouldLog(ex))
    {
        // by the time we're inside this block, a matching filter already returned true,
        // but Environment.StackTrace / the debugger's call stack while ShouldLog ran
        // still reflected the ORIGINAL throw site, not this catch block's location.
    }

    static bool ShouldLog(InvalidOperationException ex)
    {
        // This runs with the stack NOT yet unwound: a debugger break-on-throw here shows
        // the frame that actually threw, and Environment.StackTrace captured in here (or
        // any first-chance-exception logging hook) reflects the full original call chain --
        // information a `catch` block's own body no longer has access to once its clause
        // is entered, because entering the block IS what triggers the unwind.
        Logger.LogWarning(ex, "Invalid operation encountered.");
        return true;
    }
}
```

An exception filter is evaluated as part of the runtime's search for a handler, which happens
*before* any handler is entered and the stack is unwound to it. This is why first-chance exception
tooling (Visual Studio's "break when this exception type is thrown," Application Insights'
first-chance handlers, `AppDomain.FirstChanceException`) sees the same call stack a filter would —
neither has been affected by unwinding yet, unlike the body of the `catch` block itself.

## Advanced: the logging-and-rethrow anti-pattern filters replace

```csharp
// Anti-pattern: unwinds the stack just to log, then re-throws, unwinding it a second time
// on the way back out -- and Environment.StackTrace inside LogError already reflects the
// catch block's location, not the original throw site.
catch (Exception ex)
{
    LogError(ex);
    throw;
}
```

```csharp
// Filter form: logs during the search for a handler, before any unwind happens, then
// returns false so the search continues past this clause as if it never matched --
// no unwind occurs here at all unless a later clause actually handles the exception.
catch (Exception ex) when (LogAndContinueSearch(ex))
{
    throw; // unreachable: the filter always returns false
}

static bool LogAndContinueSearch(Exception ex)
{
    LogError(ex);
    return false;
}
```

Both versions end up propagating the exception to the caller, but the filter form does it without
ever entering (and therefore without ever unwinding into) a `catch` block along the way — the
`throw;` in the filter form is dead code kept only because C# requires every `catch` clause to have
a body. This also avoids the (usually negligible, but real) performance cost of unwind-then-rethrow
on a hot path where the exception is expected to almost always propagate.

## Advanced: filters combined with pattern-matching syntax

```csharp
catch (Exception ex) when (ex is ArgumentException or FormatException)
{
    // one clause handling two unrelated exception types via a filter, instead of
    // duplicating the handler body across two separate catch clauses
}
```

A `catch` clause's declared type (`catch (ExceptionType ex)`) has only ever supported a single
concrete type — there's no dedicated "pattern matching in catch clauses" language feature beyond
that. What looks like richer matching in a `catch` is ordinary pattern-matching syntax (`is`,
`or`, property patterns, and so on) used *inside* a `when` filter, exactly as it would be used in
any other boolean expression — its availability and syntax therefore depend on whichever C#
version's pattern-matching features are in scope, not on anything specific to exception handling.

## Fallback

`when` filters themselves need [C# 6.0](../references/csharp6-exception-filters.md); below that,
the logging-and-rethrow anti-pattern's non-filter alternative is an `if`-guarded unguarded `catch`
block as shown in that file's Fallback section — there is no way to inspect an exception before the
stack unwinds on an older target. Whatever pattern syntax appears inside a `when` expression falls
back according to its own version (e.g. `ex is ArgumentException or FormatException` needs the
`or` pattern combinator; on a target that lacks it, use `ex is ArgumentException || ex is
FormatException` instead, which has worked since C# 1.0).
