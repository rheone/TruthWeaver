# Testing

Most BCL attributes affect behavior a caller can observe without reflection at all (an
`[Obsolete]` member still compiles and runs; a `[DebuggerDisplay]` string only shows up in a
debugger). Testing them means asserting on that observable behavior directly, or, for attributes
whose only effect is metadata a framework reads later, asserting the metadata itself via
reflection.

## Asserting an attribute is present with the expected values

```csharp
var attribute = typeof(Order)
    .GetProperty(nameof(Order.LegacyId))!
    .GetCustomAttribute<ObsoleteAttribute>();

attribute.Should().NotBeNull();
attribute!.IsError.Should().BeFalse();
attribute.Message.Should().Contain("Use Id instead");
```

This is the right test when the attribute's presence and configured values are themselves the
contract — a build step, a source generator, or a runtime reflection-based consumer somewhere else
in the system depends on that exact attribute and those exact constructor arguments being there.

## Testing `[Conditional]` behavior

A `[Conditional("DEBUG")]` method call is compiled out entirely when the symbol isn't defined —
this is a compile-time effect, not a runtime branch, so it cannot be asserted on with an ordinary
unit test running against the compiled assembly under test. Verify it instead by compiling a small
test fixture assembly twice (once with the symbol defined, once without) and asserting on whether
the call site's IL contains the call — this is a build/tooling-level test, not something worth
writing for routine day-to-day `[Conditional]` usage.

## Testing nullable-flow attributes (`NotNullWhen`, `MemberNotNull`, etc.)

These attributes only affect the compiler's static nullable-flow analysis — they have zero runtime
effect on their own. There is nothing to unit test at runtime; the only meaningful verification is
that the annotated method's *actual* null-return behavior matches what the attribute promises
(e.g. a `[return: NotNullWhen(true)]` method that can still return `null` when it returns `true` is
a real bug, but one only caught by a test that actually calls the method and checks its return
value against its boolean result — the attribute itself doesn't add a runtime guard).

## Testing `[CallerMemberName]`/`[CallerLineNumber]`/`[CallerArgumentExpression]`

These are ordinary compile-time value substitution — test them the same way as any other method
whose behavior depends on its arguments, by calling the method from a known call site and asserting
on the captured value:

```csharp
[Fact]
public void Log_CapturesCallingMemberName()
{
    var logger = new TestLogger();

    logger.Log("message"); // relies on [CallerMemberName] to capture "Log_CapturesCallingMemberName"

    logger.LastCapturedMemberName.Should().Be(nameof(Log_CapturesCallingMemberName));
}
```

## Testing `[SuppressMessage]`/`[UnconditionalSuppressMessage]` suppressions

A suppression attribute is a claim that a specific analyzer warning is a false positive at this
exact call site — the only real "test" is periodically re-running the analyzer to confirm the
suppressed code path still needs the suppression (a refactor can silently remove the actual reason
a warning was suppressed, leaving a suppression attribute hiding a since-reintroduced real issue).
There is no ordinary unit test for this; treat it as something a periodic analyzer re-run or code
review catches, not something a test suite asserts on directly.
