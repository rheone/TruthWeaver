# Pattern Matching as a Lighter Alternative

For a **sealed, closed hierarchy** — one where every possible subtype is known and fixed within the
same compilation unit — a `switch` expression over the type hierarchy gets the same "one operation,
every type handled, compiler-checked completeness" outcome as a Visitor, without an `Accept` method,
a `Visit` interface, or a class per operation.

## Shape

```csharp
public abstract record Shape;
public sealed record Circle(double Radius) : Shape;
public sealed record Rectangle(double Width, double Height) : Shape;
public sealed record Triangle(double Base, double Height) : Shape;

public static class ShapeCalculations
{
    public static double Area(Shape shape) => shape switch
    {
        Circle c => Math.PI * c.Radius * c.Radius,
        Rectangle r => r.Width * r.Height,
        Triangle t => 0.5 * t.Base * t.Height,
        _ => throw new NotSupportedException($"Unhandled shape type: {shape.GetType()}")
    };

    public static string Describe(Shape shape) => shape switch
    {
        Circle c => $"Circle(r={c.Radius})",
        Rectangle r => $"Rectangle({r.Width}x{r.Height})",
        Triangle t => $"Triangle(b={t.Base}, h={t.Height})",
        _ => throw new NotSupportedException($"Unhandled shape type: {shape.GetType()}")
    };
}
```

Marking the base type `abstract` and every case `sealed` (or using a `sealed` hierarchy with a
private-protected constructor) means the compiler can, in principle, reason about exhaustiveness —
in practice the C# compiler does not currently emit a hard error for a missing case in a type-pattern
switch over a closed hierarchy the way it does for an exhaustive enum switch, so the `_ =>
throw new NotSupportedException(...)` arm stays as a deliberate runtime guard rather than something
you can safely omit and rely on the compiler to catch. Static analysis tooling that layers on top of
the compiler can flag a missing case at build time — keep the discard-arm regardless, so behavior at
runtime is a clear failure rather than an incorrect fallthrough if that tooling isn't active.

## Comparing the two extension directions

- **New operation** (`Area`, `Describe`, `Perimeter`): a Visitor and a `switch`-based static method
  cost about the same — one new file/class either way, or one new static method either way.
- **New type** (`Polygon`): a Visitor's compiler-enforced update (every existing `IShapeVisitor`
  implementation fails to compile until patched) has no equivalent guarantee in the `switch` form
  unless every `switch` is written without a `_` discard arm and instead lists every known type
  explicitly with a compiler warning enabled for non-exhaustive switches — and even then, each
  `switch` method must be found and updated by hand; nothing forces every one of them to be visited
  the way an interface member forces every implementing class. A `switch`-based approach is a
  reasonable trade only when the hierarchy is genuinely closed and stable enough that a new case
  showing up is rare, and when the operations are scattered across a smaller number of `switch`
  sites that a maintainer can realistically re-check by hand.

## When to prefer pattern matching over full double dispatch

- The hierarchy is sealed and every subtype lives in the same assembly (or is otherwise guaranteed
  not to be extended from outside your control) — an open hierarchy that other assemblies can add
  types to cannot be exhaustively pattern-matched at all, since new types could appear that the
  `switch` was never compiled against.
- The operations over the hierarchy are simple expressions, not multi-step algorithms with their own
  state — a `switch` arm is one expression; a `Visit` method can be an arbitrarily large method body
  with its own locals and control flow.
- You don't need the operation itself to be an injectable, swappable object — a `switch`-based
  static method is not a value that can be passed around, stored in a field, or resolved from a DI
  container the way an `IShapeVisitor` implementation can.

When any of these doesn't hold — an open hierarchy, third-party extensibility, an operation that
itself needs to be swapped as a unit — the interface-based Visitor form is the more appropriate
tool, because it is specifically built to keep working when the hierarchy is extended somewhere the
`switch` statement's author never sees.
