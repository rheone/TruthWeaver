# Non-CLS Exceptions Auto-Wrap to `RuntimeWrappedException` (C# 2.0 / CLR 2.0)

C# 2.0 shipped November 2005 with Visual Studio 2005, alongside CLR 2.0. C# 1.0's rule that a
`throw` operand must derive from `System.Exception` was always a *C# compiler* rule, not a CLR
one — the CLR itself has never required a thrown object to derive from `Exception`, and other
.NET languages (notably C++/CLI) can throw arbitrary objects. Under CLR 1.0/1.1, such a
non-CLS-compliant exception crossing into C# code could only be caught with a typeless
`catch { }` clause; a `catch (Exception ex)` simply didn't match it, silently letting it propagate
past what looked like a catch-all. CLR 2.0 closed this gap: the runtime now automatically wraps
any thrown object that isn't `Exception`-derived in a
`System.Runtime.CompilerServices.RuntimeWrappedException` (which *is* `Exception`-derived) before
it's observable by a `catch` clause, so `catch (Exception ex)` genuinely catches everything from
this version onward.

## Syntax

```csharp
catch (RuntimeWrappedException ex)
{
    object originalPayload = ex.WrappedException; // the non-CLS object that was actually thrown
}
```

## Basic use case

```csharp
public static void CallIntoInteropLibrary()
{
    try
    {
        LegacyCppInterop.Run(); // a C++/CLI component that might throw a non-Exception object
    }
    catch (Exception ex) // catches everything from CLR 2.0 onward, including wrapped non-CLS throws
    {
        Logger.LogError($"Interop call failed: {ex.Message}");
    }
}
```

Before this tier, the equivalent code needed a typeless `catch { }` fallback specifically to avoid
missing non-CLS exceptions; from C# 2.0 / CLR 2.0 onward, `catch (Exception ex)` alone is
sufficient for that purpose.

## Advanced use case: recovering the original non-CLS payload

```csharp
public static void CallIntoInteropLibrary()
{
    try
    {
        LegacyCppInterop.Run();
    }
    catch (RuntimeWrappedException ex)
    {
        // ex.WrappedException is the original object, typed `object` since it isn't Exception-derived
        Logger.LogError($"Interop call threw a non-CLS object: {ex.WrappedException}");
    }
    catch (Exception ex)
    {
        Logger.LogError($"Interop call failed: {ex.Message}");
    }
}
```

Place the more-specific `RuntimeWrappedException` clause first if the original wrapped payload is
needed; otherwise a single `catch (Exception ex)` is enough, since `RuntimeWrappedException` is
just another `Exception`-derived type.

## Requirements and restrictions

- This is purely a CLR/runtime behavior change, not new C# syntax — no new keyword or statement
  form was added; `RuntimeWrappedException` is an ordinary BCL type usable in an ordinary `catch`.
- An assembly can opt back out of auto-wrapping with
  `[assembly: RuntimeCompatibility(WrapNonExceptionThrows = false)]`, restoring the pre-CLR-2.0
  behavior where a `catch (Exception ex)` clause does not observe a non-CLS throw and a typeless
  `catch { }` is needed to guarantee catching it.
- Relevant almost exclusively at interop boundaries with non-C# .NET languages that can throw
  non-`Exception`-derived objects (chiefly C++/CLI); pure C# code can never itself construct or
  throw a non-`Exception`-derived object, at any C# version, so this tier changes nothing about
  what C# code *can throw* — only what a C# `catch` clause can *observe* from elsewhere.

## Fallback

Before CLR 2.0 (i.e., targeting .NET Framework 1.0/1.1), `catch (Exception ex)` does not catch a
non-CLS-compliant throw from another language — add a trailing typeless `catch { }` clause after
the typed clauses (see [csharp1-try-catch-finally.md](csharp1-try-catch-finally.md)) if code calls
into non-C# components that might throw non-`Exception` objects. On that older target there is no
`RuntimeWrappedException` and no way to recover the original payload — the typeless `catch` block
has no access to the thrown object at all.
