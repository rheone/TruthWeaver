---
name: csharp-visitor-pattern
description: Reference for the Visitor design pattern in C# — the classic double-dispatch form (an IVisitor interface with a Visit overload per visited type, and an Accept method on each visited type), a generic IVisitor<TResult> returning a typed result instead of accumulating state, the pattern's extension tension (new operations are free, new visited types force every existing visitor to change), and C# switch-expression pattern matching over a sealed hierarchy as a lighter-weight alternative when full double dispatch isn't warranted. Use when adding an operation over a hierarchy of types without modifying those types, deciding between double dispatch and a switch expression for a closed hierarchy, or reviewing a visitor implementation for correct dispatch.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Visitor Pattern

Visitor lets you add a new operation over a hierarchy of types without modifying any of those
types' classes — each type gains one `Accept` method, and every new operation becomes a new class
implementing a shared visitor interface, entirely external to the hierarchy.

## Quick start

```csharp
public interface IShapeVisitor<TResult>
{
    TResult Visit(Circle circle);
    TResult Visit(Rectangle rectangle);
}

public interface IShape
{
    TResult Accept<TResult>(IShapeVisitor<TResult> visitor);
}

public sealed class Circle : IShape
{
    public double Radius { get; init; }
    public TResult Accept<TResult>(IShapeVisitor<TResult> visitor) => visitor.Visit(this);
}

public sealed class AreaVisitor : IShapeVisitor<double>
{
    public double Visit(Circle circle) => Math.PI * circle.Radius * circle.Radius;
    public double Visit(Rectangle rectangle) => rectangle.Width * rectangle.Height;
}
```

For a sealed, closed hierarchy where full double dispatch is more machinery than the problem needs,
a `switch` expression over the type does the same job with less ceremony — see
[references/pattern-matching-alternative.md](references/pattern-matching-alternative.md).

## Pick your reference file

| Situation | Reference file |
| --- | --- |
| Writing the classic double-dispatch form (`Accept`/`Visit`, `void` result) | [references/classic-double-dispatch-visitor.md](references/classic-double-dispatch-visitor.md) |
| A visitor that returns a computed value instead of accumulating state | [references/generic-visitor-typed-result.md](references/generic-visitor-typed-result.md) |
| Weighing whether the hierarchy will gain more types or more operations over time | [references/adding-visited-types-vs-adding-visitors.md](references/adding-visited-types-vs-adding-visitors.md) |
| Deciding between full double dispatch and a `switch` expression for a closed hierarchy | [references/pattern-matching-alternative.md](references/pattern-matching-alternative.md) |
| Testing a visitor's logic and its dispatch correctness | [references/testing-visitors.md](references/testing-visitors.md) |
| Adding a new visitor (operation) without touching the visited hierarchy | [references/extending-visitors.md](references/extending-visitors.md) |
