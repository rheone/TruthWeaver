# Visitor Pattern

You add a new operation over a hierarchy of types without modifying any of those types' classes:
each type gains one `Accept` method, and every new operation becomes a new class implementing a
shared visitor interface, entirely external to the hierarchy. It covers the classic double-dispatch
form, a generic visitor that returns a typed result, the extension tension between adding
operations and adding visited types, and `switch`-expression pattern matching as a lighter
alternative for closed hierarchies.

## When to reach for it

- You need to add a new operation over an existing type hierarchy without touching the hierarchy's
  own classes.
- You're reviewing a visitor implementation and want to confirm the dispatch actually resolves to
  the concrete type at each call site.
- You're deciding whether a sealed, closed hierarchy warrants full double dispatch or would be
  simpler as a `switch` expression over the type.
- You're weighing whether a hierarchy is more likely to gain new types or new operations over time,
  since that shapes which approach costs less to extend later.

## Using it

This skill is model-invoked: it fires automatically when your prompt matches its situation, such as
adding an operation over a type hierarchy or reviewing visitor dispatch. You can also invoke it
directly as `/csharp-visitor-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| The classic double-dispatch form (`Accept`/`Visit`) | [references/classic-double-dispatch-visitor.md](references/classic-double-dispatch-visitor.md) |
| A generic visitor returning a typed result | [references/generic-visitor-typed-result.md](references/generic-visitor-typed-result.md) |
| The tension between adding operations and adding visited types | [references/adding-visited-types-vs-adding-visitors.md](references/adding-visited-types-vs-adding-visitors.md) |
| `switch`-expression pattern matching as a lighter alternative | [references/pattern-matching-alternative.md](references/pattern-matching-alternative.md) |
| Testing a visitor's logic and dispatch correctness | [references/testing-visitors.md](references/testing-visitors.md) |
| Adding a new visitor without touching the hierarchy | [references/extending-visitors.md](references/extending-visitors.md) |

## Example prompts

- "I need to add an `AreaVisitor` and an `PerimeterVisitor` over my `Shape` hierarchy without
  editing `Circle` or `Rectangle`. How do I set that up?"
- "Should this closed set of node types use a full visitor, or is a `switch` expression enough?"
- "My visitor's `Visit` overload is dispatching to the base class instead of the concrete type.
  What's wrong with my `Accept` method?"
