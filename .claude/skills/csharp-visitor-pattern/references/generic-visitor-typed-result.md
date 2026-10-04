# Generic Visitor with a Typed Result

A visitor whose `Visit` overloads each return `TResult` lets `Accept` hand back a value directly,
instead of the visitor accumulating one as mutable internal state. This is usually the more natural
shape once the operation is a pure computation rather than something with a side effect.

## Shape

```csharp
public interface IShapeVisitor<TResult>
{
    TResult Visit(Circle circle);
    TResult Visit(Rectangle rectangle);
    TResult Visit(Triangle triangle);
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

public sealed class Rectangle : IShape
{
    public double Width { get; init; }
    public double Height { get; init; }
    public TResult Accept<TResult>(IShapeVisitor<TResult> visitor) => visitor.Visit(this);
}

public sealed class Triangle : IShape
{
    public double Base { get; init; }
    public double Height { get; init; }
    public TResult Accept<TResult>(IShapeVisitor<TResult> visitor) => visitor.Visit(this);
}
```

A visitor implementation now returns its result per element instead of mutating a field:

```csharp
public sealed class AreaVisitor : IShapeVisitor<double>
{
    public double Visit(Circle circle) => Math.PI * circle.Radius * circle.Radius;
    public double Visit(Rectangle rectangle) => rectangle.Width * rectangle.Height;
    public double Visit(Triangle triangle) => 0.5 * triangle.Base * triangle.Height;
}
```

```csharp
var totalArea = shapes.Sum(shape => shape.Accept(new AreaVisitor()));
```

A second visitor over the same hierarchy needs no changes to `IShape`, `Circle`, `Rectangle`, or
`Triangle` — only a new class implementing `IShapeVisitor<TResult>` closed over whatever result type
that operation needs:

```csharp
public sealed class DescriptionVisitor : IShapeVisitor<string>
{
    public string Visit(Circle circle) => $"Circle(r={circle.Radius})";
    public string Visit(Rectangle rectangle) => $"Rectangle({rectangle.Width}x{rectangle.Height})";
    public string Visit(Triangle triangle) => $"Triangle(b={triangle.Base}, h={triangle.Height})";
}
```

## Making `Accept` non-generic per element instead

`Accept<TResult>` being generic means every visited type's `Accept` method is a generic method,
which is the most flexible shape — one `Accept` method serves every possible `TResult` a future
visitor might need. An alternative is to fix `TResult` at the interface level instead
(`IShape<TResult>` with a non-generic `Accept(IShapeVisitor<TResult> visitor)`), which trades that
flexibility for a hierarchy that only ever returns one result type; reach for that only when the
hierarchy genuinely has one true purpose (e.g. a hierarchy that exists solely to produce strings for
serialization) and never needs a second, differently-typed operation.

## Combining a `void` and a typed visitor

Some operations are naturally side-effecting (rendering to a canvas, writing to a stream) and don't
need a return value; others are naturally pure computations. Keep both `IShapeVisitor` (void) and
`IShapeVisitor<TResult>` (typed) available over the same hierarchy if both operation shapes
genuinely occur — `Accept(IShapeVisitor visitor)` and `Accept<TResult>(IShapeVisitor<TResult>
visitor)` can coexist as two overloads on the same visited interface without conflict, since their
parameter types differ.
