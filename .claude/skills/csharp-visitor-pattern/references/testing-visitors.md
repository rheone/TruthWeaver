# Testing Code Built on Visitor

A visitor is a plain class implementing a plain interface — testing one means constructing it,
calling `Accept` on each concrete element type you care about, and asserting on the result (or the
visitor's accumulated state, for a `void` visitor). No mocking is usually needed at all, because a
visitor's only real collaborator is the element it's visiting, and elements in a visited hierarchy
are typically simple data holders with no dependencies of their own.

## Testing a typed-result visitor

Construct the visitor once, call `Accept` (or `Visit` directly) per concrete type, and assert on the
return value — cover one test per concrete type in the hierarchy, since each corresponds to a
distinct code path (`Visit` overload) that no other test can exercise.

```csharp
public class AreaVisitorTests
{
    private readonly AreaVisitor _visitor = new();

    [Fact]
    public void Visit_Circle_ComputesPiRSquared()
    {
        var result = _visitor.Visit(new Circle { Radius = 2 });
        Assert.Equal(Math.PI * 4, result, precision: 10);
    }

    [Fact]
    public void Visit_Rectangle_ComputesWidthTimesHeight()
    {
        var result = _visitor.Visit(new Rectangle { Width = 3, Height = 4 });
        Assert.Equal(12, result);
    }

    [Fact]
    public void Visit_Triangle_ComputesHalfBaseTimesHeight()
    {
        var result = _visitor.Visit(new Triangle { Base = 6, Height = 5 });
        Assert.Equal(15, result);
    }
}
```

Calling `Visit` directly (rather than going through `shape.Accept(visitor)`) is enough to test the
visitor's own logic — double dispatch is a mechanism for the calling code to reach the right
overload without a type check, not logic inside the visitor that itself needs testing.

## Testing that `Accept` dispatches to the right overload

Double dispatch's correctness — that `circle.Accept(visitor)` really does call `Visit(Circle)` and
not some other overload — is worth one focused test per concrete visited type, using a visitor whose
only job is to record which overload ran:

```csharp
public class ShapeAcceptDispatchTests
{
    private sealed class RecordingVisitor : IShapeVisitor<string>
    {
        public string Visit(Circle circle) => nameof(Circle);
        public string Visit(Rectangle rectangle) => nameof(Rectangle);
        public string Visit(Triangle triangle) => nameof(Triangle);
    }

    [Theory]
    [MemberData(nameof(ShapesWithExpectedTypeNames))]
    public void Accept_DispatchesToMatchingVisitOverload(IShape shape, string expectedTypeName)
    {
        var result = shape.Accept(new RecordingVisitor());
        Assert.Equal(expectedTypeName, result);
    }

    public static IEnumerable<object[]> ShapesWithExpectedTypeNames()
    {
        yield return new object[] { new Circle { Radius = 1 }, nameof(Circle) };
        yield return new object[] { new Rectangle { Width = 1, Height = 1 }, nameof(Rectangle) };
        yield return new object[] { new Triangle { Base = 1, Height = 1 }, nameof(Triangle) };
    }
}
```

This test class catches the specific bug class Visitor is prone to: a copy-pasted `Accept` override
that calls the wrong `Visit` overload (most often, one `Accept` implementation left calling
`visitor.Visit(this)` where `this`'s static type was accidentally left as a base or interface type
instead of the concrete class, which silently resolves to the wrong overload or fails to compile
depending on the surrounding code).

## Testing a `void`, state-accumulating visitor

Construct the visitor, call `Accept` for every element in the scenario, then assert on the visitor's
exposed accumulated state:

```csharp
[Fact]
public void AreaCalculatorVisitor_AccumulatesTotalAreaAcrossShapes()
{
    var visitor = new AreaCalculatorVisitor();
    new Circle { Radius = 1 }.Accept(visitor);
    new Rectangle { Width = 2, Height = 2 }.Accept(visitor);

    Assert.Equal(Math.PI + 4, visitor.TotalArea, precision: 10);
}
```

Test accumulation across at least two calls, not just one — a visitor that resets state instead of
accumulating it, or that only handles the first `Accept` call correctly, passes a single-call test
but fails in real use, where a visitor is applied across an entire collection.
