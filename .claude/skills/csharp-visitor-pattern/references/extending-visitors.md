# Adding a New Visitor Without Breaking Existing Code

Adding an operation to a Visitor-based hierarchy — the direction the pattern optimizes for — never
requires touching the visited types, `Accept`, or any existing visitor. This file covers doing that
addition correctly and the mistakes that quietly reintroduce coupling.

## Adding a new visitor: the steps

1. **Implement the visitor interface for every concrete visited type.** For `IShapeVisitor<TResult>`
   closed over your new result type, that means one `Visit` overload per existing concrete type —
   `Circle`, `Rectangle`, `Triangle` — with no changes needed to `IShape`, `Accept`, or any of those
   three classes.

   ```csharp
   public sealed class SvgPathVisitor : IShapeVisitor<string>
   {
       public string Visit(Circle circle) =>
           $"<circle r=\"{circle.Radius}\" />";
       public string Visit(Rectangle rectangle) =>
           $"<rect width=\"{rectangle.Width}\" height=\"{rectangle.Height}\" />";
       public string Visit(Triangle triangle) =>
           $"<polygon points=\"...\" />"; // computed from Base/Height as needed
   }
   ```

2. **Write dispatch and logic tests for the new visitor** (see
   [testing-visitors.md](testing-visitors.md)) — no existing test file needs to change, because no
   existing type changed.

3. **Wire it into calling code** wherever the operation is triggered — a DI registration if visitors
   are resolved from a container, a direct `new` at the call site, or a factory, exactly as any other
   class would be wired in.

## Keeping visitors independent of each other

Two visitors over the same hierarchy should never depend on each other's internals or share mutable
state through anything other than their own constructor parameters — `AreaVisitor` and
`SvgPathVisitor` are both free to exist, be tested, and be modified in total isolation as long as
neither one reaches into the other. If two operations genuinely need to share a computation
(`PerimeterVisitor` needing the same per-side lengths `AreaVisitor` computes for a polygon), extract
that shared computation into a plain helper method or a value the visited type itself exposes,
rather than having one visitor call another.

## Extending a visitor's constructor without breaking its existing callers

A visitor that grows a new constructor dependency (an `ILogger`, a formatting option) is an ordinary
class-level backward-compatibility concern, not something specific to Visitor — add an overload or a
default parameter if existing call sites must keep compiling unchanged, or update every call site if
a breaking change is acceptable in this codebase's versioning policy. This is unrelated to the
visited hierarchy itself, which never needs to know how any individual visitor is constructed.

## What extension does not mean here

"Extending the visitor pattern" refers to adding a new *operation* (a new visitor class), not a new
*visited type* — adding a new type is the pattern's expensive direction, covered in full in
[adding-visited-types-vs-adding-visitors.md](adding-visited-types-vs-adding-visitors.md), including
the compiler-enforced update every existing visitor needs when that happens. Confusing the two
directions is the most common way this pattern gets misapplied: choosing it because "operations
extend easily," while the actual codebase in question is one where new visited types show up
constantly and existing visitors keep breaking.
