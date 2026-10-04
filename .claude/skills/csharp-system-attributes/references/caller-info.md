# Caller-info attributes

`System.Runtime.CompilerServices` attributes on optional parameters, filled in by the *compiler*
at each call site with a fact about the call itself — the callee never has to be told this
information explicitly, and callers never pass it.

## `[CallerMemberName]`, `[CallerLineNumber]`, `[CallerFilePath]`

Since .NET Framework 4.5 / C# 5.0.

```csharp
public void Log(string message,
    [CallerMemberName] string memberName = "",
    [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0)
{
    Console.WriteLine($"[{Path.GetFileName(filePath)}:{lineNumber}] {memberName}: {message}");
}
```

The canonical use is `INotifyPropertyChanged`:

```csharp
private string _name = "";
public string Name
{
    get => _name;
    set { _name = value; OnPropertyChanged(); } // no magic string needed
}

private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
```

**When to add it proactively**: any `OnPropertyChanged`/`RaisePropertyChanged`-style method —
without `[CallerMemberName]`, the property name has to be passed as a string literal, which
silently desyncs from the property it names on any rename that doesn't also touch the string.
Also useful for logging helpers where the call site (member/file/line) is more useful than a
manually-typed context tag.

Each parameter must have a default value and be declared `optional`; the attribute only fires
when the argument is *omitted* at the call site — an explicit value passed by the caller wins.

## `[CallerArgumentExpression]`

`System.Runtime.CompilerServices.CallerArgumentExpressionAttribute(string parameterName)` —
since C# 10 / .NET 6.

Captures the *source text* of another argument at the call site, as a string — this is what
powers `ArgumentNullException.ThrowIfNull`'s automatic parameter-name-in-message behavior and
`Debug.Assert`'s ability to print the failing condition's own source.

```csharp
public static void ThrowIfNegative(int value,
    [CallerArgumentExpression(nameof(value))] string? paramName = null)
{
    if (value < 0) throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be >= 0.");
}

ThrowIfNegative(retryCount); // paramName is automatically "retryCount"
```

**When to add it proactively**: any custom guard/assert helper along the lines of
`Debug.Assert(condition)` or a `ThrowIfXxx(value)` validator, so the exception message names the
actual expression the caller wrote rather than a generic parameter name — this is the same
pattern the BCL uses in `ArgumentNullException.ThrowIfNull`, and pairs naturally with
`[DoesNotReturn]`/`[StackTraceHidden]` on the same throw-helper.

## Fallback / no-op behavior

- `[CallerMemberName]` / `[CallerLineNumber]` / `[CallerFilePath]`: .NET Framework 4.5+ / C# 5.0+.
  On an older target, there is no compiler-assisted substitute — pass the information explicitly
  (e.g. `nameof(PropertyName)` for the member-name case), which is exactly the boilerplate these
  attributes exist to remove.
- `[CallerArgumentExpression]`: requires C# 10; the *type* is available from .NET 6 (and via a
  polyfill on some older targets that still compile with a C# 10+ compiler), but without a C# 10+
  language version the attribute has no effect since the compiler feature that populates it
  doesn't exist. Fallback: pass the parameter name as a manual string literal (accepting the
  desync-on-rename risk that `[CallerArgumentExpression]` is meant to eliminate) or fall back to
  `[CallerMemberName]` if member-level (not expression-level) granularity is sufficient.
