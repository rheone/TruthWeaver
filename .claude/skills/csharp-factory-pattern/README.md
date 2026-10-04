# C# Factory Pattern

Factory Method centralizes how a single type (or one of several interchangeable variants) gets
constructed, behind an interface's `Create` method or a static factory method. Abstract Factory
extends that idea to a whole family of related types that must be created consistently together.
This skill covers both patterns, a generic factory interface, resolving a factory through
dependency injection, and when a factory is worth the extra indirection.

## When to reach for it

- Object creation needs to vary at runtime based on configuration, input, or context.
- A family of related objects must be created together and stay mutually consistent.
- A long-lived consumer needs fresh instances of a short-lived dependency on demand, rather than one
  instance injected once at startup.
- You're reviewing whether a factory is overkill for a given type versus calling `new` directly or
  relying on constructor injection alone.

## Using it

This skill is model-invoked: it activates automatically when the conversation touches varying
object creation at runtime, families of related objects, or resolving a factory through DI. You can
also invoke it directly by asking for it or typing `/csharp-factory-pattern`.

## What it covers

| Topic | Reference |
| --- | --- |
| An interface `Create` method or static factory method; why not just call `new` everywhere | [references/factory-method.md](references/factory-method.md) |
| Creating families of related objects that must stay mutually consistent | [references/abstract-factory.md](references/abstract-factory.md) |
| A generic `IFactory<T>`/`IFactory<TArg, T>` interface versus a plain `Func<TArg, T>` | [references/generic-factory-interface.md](references/generic-factory-interface.md) |
| Registering a factory delegate or interface, and resolving a fresh transient from a singleton | [references/factory-registration-via-di.md](references/factory-registration-via-di.md) |
| The decision list for when a factory earns its cost | [references/factory-vs-new-vs-di.md](references/factory-vs-new-vs-di.md) |
| Testing a factory's produced type, family consistency, and a consumer with a fake factory | [references/testing-factories.md](references/testing-factories.md) |
| Adding a new Factory Method case or a new Abstract Factory family/product | [references/extending-factories.md](references/extending-factories.md) |

## Example prompts

- "This singleton service needs a fresh instance of a short-lived dependency every time it's
  called. Help me set up a factory for that."
- "I need to create a matching set of UI controls for either a light or dark theme, and they have
  to stay consistent with each other."
- "Is a factory overkill here, or should I just inject the concrete type directly?"
