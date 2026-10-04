# No Expression Tree Representation (C# 1.0 – 2.0 / .NET Framework 1.0 – 2.0)

`Expression<TDelegate>` and the `System.Linq.Expressions` namespace do not exist before C# 3.0
(.NET Framework 3.5, November 2007). Targeting .NET Framework 1.0, 1.1, or 2.0 — or compiling with
`<LangVersion>1</LangVersion>` / `<LangVersion>2</LangVersion>` — there is no built-in way to
represent a chunk of C# logic as an inspectable data structure at all. A method either runs
(a delegate) or it doesn't; nothing sits in between.

## What .NET 2.0 does have

- **Generics** (C# 2.0) — generic delegate types exist (`Predicate<T>`, `Comparison<T>`,
  `Converter<TInput,TResult>`), so a piece of logic can be *parameterized* generically, just not
  *inspected* once it's a delegate.
- **Reflection** (C# 1.0 onward) — a method's *shape* (parameters, return type, attributes) can be
  inspected via `MethodInfo`, but not its *body*; reflection describes what a method looks like
  from the outside, not what it does.
- **`System.CodeDom`** (.NET Framework 1.0 onward) — a namespace for representing source code as an
  object graph (`CodeMemberMethod`, `CodeExpression`, etc.) and compiling it via `CodeDomProvider`.
  This is the closest pre-existing analog to an expression tree — data describing code — but it
  models whole compilation units for code generation, not a single reusable, walkable expression a
  library inspects and reinterprets at runtime the way an `IQueryable` provider does.

## The fallback pattern: build and invoke IL directly, or hand-roll an interpreter

Two idioms filled the gap an expression tree would later close: needing to construct or select
logic *at runtime* based on data, then execute it.

**`System.Reflection.Emit` (`DynamicMethod`/`ILGenerator`)** for runtime-generated code that needs
to run fast:

```csharp
// C# 2.0 / .NET Framework 2.0 — compiles everywhere
var method = new DynamicMethod("Add", typeof(int), new[] { typeof(int), typeof(int) });
ILGenerator il = method.GetILGenerator();
il.Emit(OpCodes.Ldarg_0);
il.Emit(OpCodes.Ldarg_1);
il.Emit(OpCodes.Add);
il.Emit(OpCodes.Ret);

var add = (Func<int, int, int>)method.CreateDelegate(typeof(Func<int, int, int>));
int sum = add(2, 3); // 5
```

This is real, but it operates one level below what an expression tree gives you: you're emitting
IL opcodes directly, with no type-checked, walkable object graph a caller could inspect or
translate to something other than IL (SQL, for instance) afterward.

**A hand-rolled node-and-interpreter pair** for logic that needs to be *inspected or translated*,
not just executed — the shape every later expression-tree-consuming library (LINQ providers, ORMs)
would otherwise have had to invent for itself:

```csharp
public abstract class FilterNode
{
    public abstract bool Matches(Order order);
}

public sealed class TotalGreaterThan : FilterNode
{
    private readonly decimal _threshold;
    public TotalGreaterThan(decimal threshold) => _threshold = threshold;
    public override bool Matches(Order order) => order.Total > _threshold;
}

public sealed class And : FilterNode
{
    private readonly FilterNode _left, _right;
    public And(FilterNode left, FilterNode right) => (_left, _right) = (left, right);
    public override bool Matches(Order order) => _left.Matches(order) && _right.Matches(order);
}
```

A caller composes `FilterNode` instances at runtime and something else — an in-memory `Matches`
call here, or in principle a SQL-translating visitor — interprets them. This is structurally what
`Expression`/`ExpressionVisitor` standardizes in C# 3.0+: instead of every library inventing its
own node hierarchy, the BCL provides one (`Expression`, `MethodCallExpression`,
`BinaryExpression`, ...) plus a visitor base class, and the C# compiler can *emit* it directly
from ordinary lambda syntax.

## Porting forward

When a project later moves to .NET Framework 3.5+ (C# 3.0+), a hand-rolled node hierarchy like
`FilterNode` above can often be replaced wholesale by `Expression<Func<Order, bool>>` — the
compiler builds the tree from ordinary lambda syntax instead of the caller composing node objects
by hand, and any code that walked the custom hierarchy becomes an `ExpressionVisitor` subclass
walking the standard one instead. See
[csharp3-expression-trees-fundamentals.md](csharp3-expression-trees-fundamentals.md).
