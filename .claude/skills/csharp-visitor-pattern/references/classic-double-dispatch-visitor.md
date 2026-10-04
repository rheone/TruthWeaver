# Classic Double-Dispatch Visitor

The Visitor pattern lets you add a new operation over a hierarchy of types without editing any of
those types' own classes beyond an `Accept` method each one already carries. It works by *double
dispatch*: the call first dispatches on the concrete type of the element being visited (through
virtual `Accept`), then dispatches again on the concrete type of the visitor (through overload
resolution on `Visit`), so the pair of concrete types together select the correct code path — a
single virtual call in C# can only dispatch on one type at a time.

## Shape

The visited hierarchy declares an `Accept` method that every concrete type implements identically
in form, differing only in which `Visit` overload it calls:

```csharp
public interface IShapeVisitor
{
    void Visit(Circle circle);
    void Visit(Rectangle rectangle);
    void Visit(Triangle triangle);
}

public interface IShape
{
    void Accept(IShapeVisitor visitor);
}

public sealed class Circle : IShape
{
    public double Radius { get; init; }
    public void Accept(IShapeVisitor visitor) => visitor.Visit(this);
}

public sealed class Rectangle : IShape
{
    public double Width { get; init; }
    public double Height { get; init; }
    public void Accept(IShapeVisitor visitor) => visitor.Visit(this);
}

public sealed class Triangle : IShape
{
    public double Base { get; init; }
    public double Height { get; init; }
    public void Accept(IShapeVisitor visitor) => visitor.Visit(this);
}
```

A concrete visitor implements one operation across every type in the hierarchy:

```csharp
public sealed class AreaCalculatorVisitor : IShapeVisitor
{
    public double TotalArea { get; private set; }

    public void Visit(Circle circle) => TotalArea += Math.PI * circle.Radius * circle.Radius;
    public void Visit(Rectangle rectangle) => TotalArea += rectangle.Width * rectangle.Height;
    public void Visit(Triangle triangle) => TotalArea += 0.5 * triangle.Base * triangle.Height;
}
```

Calling it over a heterogeneous collection needs no type check anywhere in the calling code:

```csharp
var visitor = new AreaCalculatorVisitor();
foreach (var shape in shapes)
{
    shape.Accept(visitor);
}
Console.WriteLine(visitor.TotalArea);
```

## Why `Accept` calls `visitor.Visit(this)` and not `visitor.Visit((object)this)`

The double dispatch depends entirely on `this` being statically typed as the concrete class inside
each `Accept` override — inside `Circle.Accept`, `this` has compile-time type `Circle`, so overload
resolution picks `Visit(Circle)` at compile time for that call site. If `Accept` were written once
on a shared base class using a generically-typed `this`, every override would resolve to the same
`Visit` overload and the pattern would stop working — this is exactly why every concrete visited
type needs its own `Accept` override, even though every override's body looks identical.

## When a visitor with no return value isn't enough

`IShapeVisitor` above returns `void` and accumulates a result as visitor state
(`AreaCalculatorVisitor.TotalArea`). That works, but forces every visitor to be stateful even when
the "state" is really just a single computed value being threaded through. A generic visitor that
returns a value directly from each `Visit` call removes that friction — see
[generic-visitor-typed-result.md](generic-visitor-typed-result.md).
