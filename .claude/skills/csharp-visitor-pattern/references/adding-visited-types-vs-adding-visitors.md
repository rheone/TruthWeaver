# The Extension Tension: New Visited Types vs. New Visitors

Visitor makes one direction of extension free and the other expensive, and the choice of hierarchy
shape is really a bet on which direction of change is going to happen more often.

## Adding a new visitor is free

Once `IShape`, `Accept`, and the concrete visited types (`Circle`, `Rectangle`, `Triangle`) exist,
adding a new operation over the whole hierarchy means writing exactly one new class that implements
`IShapeVisitor` (or `IShapeVisitor<TResult>`) — no existing file changes:

```csharp
public sealed class PerimeterVisitor : IShapeVisitor<double>
{
    public double Visit(Circle circle) => 2 * Math.PI * circle.Radius;
    public double Visit(Rectangle rectangle) => 2 * (rectangle.Width + rectangle.Height);
    public double Visit(Triangle triangle) => throw new NotSupportedException(
        "Perimeter requires all three side lengths; Triangle only models base and height.");
}
```

This is Visitor's core payoff: a codebase that adds new *operations* over a stable set of types far
more often than it adds new *types* benefits enormously, because every new operation is fully
isolated in its own file and cannot break an existing operation by construction.

## Adding a new visited type is expensive

Adding a new shape — `Polygon` — means:

1. Adding `Visit(Polygon polygon)` to `IShapeVisitor` (and to `IShapeVisitor<TResult>` if both
   exist).
2. Implementing that new overload in **every** existing visitor — `AreaVisitor`, `DescriptionVisitor`,
   `PerimeterVisitor`, and any other visitor written so far — even ones that have nothing meaningful
   to compute for a polygon.
3. Adding `Polygon` itself with its own `Accept` implementation.

Step 2 is the expensive part, and it does not fail at compile time in every language — but in C#,
adding a member to an interface does break every class implementing that interface until it's
updated, which is actually a safety net here: the compiler enumerates every visitor you forgot to
update as a build error, rather than letting `Polygon` silently fall through to no-op behavior in an
old visitor. Do not treat this compiler error as friction to route around (for example, by giving
`IShapeVisitor` a default-implemented `Visit(Polygon)` on an interface, or a common no-op base
class) unless every existing visitor genuinely has nothing to do for a polygon — that convenience
reintroduces the silent-fallthrough bug the compiler was otherwise preventing.

## Choosing the hierarchy shape up front

This tension is exactly why Visitor is worth adopting only when you can predict, with reasonable
confidence, that the set of visited types is comparatively stable and the set of operations over
them is what's going to grow. A domain model of shapes that rarely gains a new shape but frequently
gains new reporting/rendering/serialization operations is the textbook fit. A domain model where new
subtypes appear constantly and the operations over them are fixed and few is the textbook case for
inverting the structure instead — putting the operation as a virtual method on each type rather than
as an external visitor — or, for a closed set of types known and fixed within a single compilation
unit, using pattern matching instead of double dispatch entirely (see
[pattern-matching-alternative.md](pattern-matching-alternative.md)).
