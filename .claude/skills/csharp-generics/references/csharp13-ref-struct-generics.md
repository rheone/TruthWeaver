# `allows ref struct` (C# 13 / .NET 9)

Before C# 13, a `ref struct` type (`Span<T>`, `ReadOnlySpan<T>`, and custom `ref struct` types)
could never be used as a type argument — `List<Span<int>>` and `SomeGeneric<Span<int>>` both
failed to compile, because the compiler couldn't guarantee the generic code wouldn't do something
unsafe with a stack-only value (store it in a field, box it, capture it in a closure). C# 13
(.NET 9, November 2024) adds an **anti-constraint** that opts a specific generic parameter into
allowing it.

## Syntax

```csharp
public class Buffer<T> where T : allows ref struct
{
    public void Process(scoped T value)
    {
        // ...
    }
}

Buffer<Span<int>> buffer = new(); // now legal
```

`allows ref struct` is additive, not restrictive — unlike every other constraint kind, it widens
what `T` may be rather than narrowing it, which is why it's called an anti-constraint. It combines
with ordinary constraints: `where T : IProcessable, allows ref struct`.

## Why `scoped` shows up alongside it

Once `T` might be a `ref struct`, the compiler enforces ref-safety rules on every use of a `T`
value — it may not escape the current call stack (be stored in a field, returned past its safe
scope, or captured by a delegate) unless proven safe. `scoped T` on a parameter tells the compiler
"this instance won't outlive the method," which is required to do anything useful with a `T` that
might be `Span<int>`.

## Basic use case: a generic algorithm over `Span<T>`-shaped inputs

```csharp
public static class BufferOps
{
    public static int Sum<T>(scoped T buffer) where T : allows ref struct, IReadOnlySpanLike<int>
    {
        int total = 0;
        foreach (var item in buffer.AsSpan())
        {
            total += item;
        }
        return total;
    }
}
```

(`IReadOnlySpanLike<int>` is illustrative — the BCL doesn't ship a single interface `Span<T>`
implements, since `ref struct` types can't implement interfaces at all before C# 13's related
ref-struct-interfaces feature; see the caveat below.)

## Advanced use case: passing ref struct state through a callback

```csharp
public static class SpanCallback
{
    public static TResult WithState<TState, TResult>(TState state, Func<TState, TResult> callback)
        where TState : allows ref struct =>
        callback(state);
}

ReadOnlySpan<char> chars = "hello";
int length = SpanCallback.WithState(chars, s => s.Length);
```

Before C# 13, `TState` here could never be `ReadOnlySpan<char>` — the state-passed-through-a-
generic-callback pattern (common in high-performance parsing code) had to fall back to `object`
boxing or a non-generic overload per state shape.

## Fallback

No equivalent before C# 13 — a generic type or method needing to accept a `ref struct` either
avoids generics for that parameter (a non-generic overload taking `Span<T>` directly) or accepts
the type unconstrained and simply cannot be instantiated with a `ref struct` type argument (the
compiler rejects the attempt with CS0306 pre-13).
