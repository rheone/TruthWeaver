# `ExpressionVisitor` and Tree Rewriting

`ExpressionVisitor` (introduced alongside the DLR-era node types in
[csharp4-dlr-expression-node-types.md](../references/csharp4-dlr-expression-node-types.md)) is the
standard way to walk an expression tree — visiting every node, optionally producing a new tree with
some nodes replaced. This is how every non-trivial expression-tree consumer works: a LINQ provider
walks your query tree to translate it; a predicate-combining helper walks a sub-tree to rebind its
parameter; a tree-based validation rule walks a tree to check what it does before ever compiling
it.

## Basic: a visitor that swaps one parameter for another

```csharp
public sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;

    public ParameterReplacer(ParameterExpression from, ParameterExpression to)
    {
        _from = from;
        _to = to;
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _from ? _to : base.VisitParameter(node);
}
```

```csharp
Expression<Func<Order, bool>> original = o => o.Total > 100m;
ParameterExpression sharedParameter = Expression.Parameter(typeof(Order), "order");

Expression rebound = new ParameterReplacer(original.Parameters[0], sharedParameter)
    .Visit(original.Body)!;
```

`ExpressionVisitor`'s base `Visit` method dispatches to a `VisitXxx` override per node type
(`VisitParameter`, `VisitBinary`, `VisitMethodCall`, ...); overriding just the one node type you
care about and calling `base.VisitXxx(node)` (or returning the replacement directly, as above) for
everything else is the standard pattern — you never need to override every visit method, only the
ones whose node type you're inspecting or rewriting.

## Basic: a visitor that counts nodes without rewriting anything

```csharp
public sealed class MethodCallCounter : ExpressionVisitor
{
    public int Count { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Count++;
        return base.VisitMethodCall(node);
    }
}

var counter = new MethodCallCounter();
counter.Visit(someExpression.Body);
int callCount = counter.Count;
```

A visitor doesn't have to rewrite anything — returning `base.VisitXxx(node)` unchanged (after
recording something as a side effect, as `Count++` does here) makes the visitor purely
observational. This shape is common for validating what a tree contains before compiling or
translating it — for example, rejecting a tree that calls a method a downstream provider can't
translate, before that provider's own translation fails with a less obvious error.

## Advanced: a generic constant-folding rewriter

```csharp
public sealed class ConstantFolder : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        Expression left = Visit(node.Left);
        Expression right = Visit(node.Right);

        if (left is ConstantExpression { Value: int leftValue } &&
            right is ConstantExpression { Value: int rightValue } &&
            node.NodeType == ExpressionType.Add)
        {
            return Expression.Constant(leftValue + rightValue);
        }

        return node.Update(left, node.Conversion, right);
    }
}

Expression<Func<int, int>> tree = x => x + (2 + 3); // the (2 + 3) sub-tree is constant
Expression<Func<int, int>> folded = (Expression<Func<int, int>>)
    new ConstantFolder().Visit(tree);
// folded's body is now: x + 5
```

`node.Update(...)` — present on every expression node type — rebuilds a node with new children only
if they actually changed, returning the original instance otherwise; this is the idiomatic way to
reconstruct a node after recursively visiting its children, and it's what every `VisitXxx` override
in the BCL's own `ExpressionVisitor` base implementation does internally.

## Advanced: a generic visitor-driven tree validator

```csharp
public sealed class AllowedMethodsValidator : ExpressionVisitor
{
    private readonly HashSet<MethodInfo> _allowed;
    public List<string> Violations { get; } = new();

    public AllowedMethodsValidator(IEnumerable<MethodInfo> allowed) => _allowed = new(allowed);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (!_allowed.Contains(node.Method))
        {
            Violations.Add($"{node.Method.DeclaringType?.Name}.{node.Method.Name} is not allowed");
        }
        return base.VisitMethodCall(node);
    }
}

public static void EnsureTranslatable<T>(Expression<Func<T, bool>> filter, IEnumerable<MethodInfo> allowedMethods)
{
    var validator = new AllowedMethodsValidator(allowedMethods);
    validator.Visit(filter.Body);

    if (validator.Violations.Count > 0)
    {
        throw new NotSupportedException(string.Join("; ", validator.Violations));
    }
}
```

`EnsureTranslatable<T>` is a generic method usable against any `Expression<Func<T, bool>>` — the
visitor itself doesn't need to be generic (it operates on `Expression` nodes, which are already
type-erased at the tree level), but the entry point wrapping it typically is, since callers work
with a strongly-typed `Expression<Func<T, bool>>` rather than a bare `Expression`.

## Fallback

`ExpressionVisitor` and every node type used above (`BinaryExpression`, `MethodCallExpression`,
`ParameterExpression`, `ConstantExpression`) require the DLR-era API surface from
[csharp4-dlr-expression-node-types.md](../references/csharp4-dlr-expression-node-types.md) — .NET
Framework 4.0 / C# 4.0 or later. On .NET Framework 3.5 (C# 3.0), where `ExpressionVisitor` doesn't
exist yet, walk a tree by hand with a `switch` on `Expression.NodeType`, recursively inspecting
`.Left`/`.Right`/`.Object`/`.Arguments` per node type — considerably more code, but the individual
node types themselves (`BinaryExpression`, `MethodCallExpression`, ...) are all present since C#
3.0's original `System.Linq.Expressions`, per
[csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md).
