# C# Extension Members

Helps you write, review, or port extension methods and extension members in C#, from the classic
`this`-parameter form through the newer block-based `extension(...)` syntax that adds properties,
static members, operators, and indexers.

## When to reach for it

- Adding a method, property, or operator to a type you can't or don't want to modify directly
- Choosing between classic extension-method syntax and the newer `extension` block syntax
- Targeting multiple C# language versions from the same library
- Writing a generic extension member
- Resolving ambiguity when classic and new-style extension members coexist on the same type

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting
extension methods or extension members.

## What it covers

| Topic | Reference |
| --- | --- |
| No extension mechanism: the pre-C# 3 fallback pattern | [references/pre-csharp3-no-extensions.md](references/pre-csharp3-no-extensions.md) |
| Classic `this`-parameter extension methods | [references/csharp3-extension-methods.md](references/csharp3-extension-methods.md) |
| Nullable annotations on extension methods | [references/csharp8-nullable-extensions.md](references/csharp8-nullable-extensions.md) |
| Extension blocks: properties, static members, operators | [references/csharp14-extension-members.md](references/csharp14-extension-members.md) |
| Extension indexers | [references/csharp15-extension-indexers.md](references/csharp15-extension-indexers.md) |

## Example prompts

- "Add an `IsNullOrBlank` extension method on `string`."
- "Can I write a static extension property with the new extension block syntax?"
- "Why is the compiler picking the wrong overload between my classic and new-style extension methods?"
