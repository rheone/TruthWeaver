# .NET 7 SDK / Roslyn 4.3 — `ForAttributeWithMetadataName` (November 2022)

A second, purpose-built entry point on `context.SyntaxProvider` for the single most common
generator shape: "find every declaration marked with attribute X." `ForAttributeWithMetadataName`
replaces a hand-written `CreateSyntaxProvider` predicate/transform pair for that case and is
documented as roughly two orders of magnitude cheaper on the predicate side, because Roslyn indexes
which syntax trees even *textually mention* the attribute's simple name before your predicate runs
at all — files that don't mention the attribute anywhere never reach your delegates, instead of
every node in every file being tested against a hand-written predicate.

This tier also ships `WithTrackingName`, which names a pipeline stage so a unit test can assert on
*which* stages re-ran (and why) after an edit — see
[specialized/testing-a-source-generator.md](../specialized/testing-a-source-generator.md).

## Syntax

```csharp
IncrementalValuesProvider<INamedTypeSymbol> candidates = context.SyntaxProvider
    .ForAttributeWithMetadataName(
        fullyQualifiedMetadataName: "MyApp.Generators.GreetableAttribute",
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
    .WithTrackingName("GreetableTypes");
```

## Basic use case: attribute-driven generation with no manual symbol lookup

Unlike `CreateSyntaxProvider`, the `transform` delegate here receives a
`GeneratorAttributeSyntaxContext` whose `TargetSymbol`, `TargetNode`, and `Attributes` are already
resolved — no manual `GetDeclaredSymbol` call, and `Attributes` gives every matching
`AttributeData` instance directly, including constructor-argument values:

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyApp.Generators.GreetableAttribute",
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, _) =>
        {
            AttributeData attribute = ctx.Attributes[0];
            string greeting = attribute.ConstructorArguments is [{ Value: string s }]
                ? s
                : "Hello";
            return new GreetableType(ctx.TargetSymbol.Name, greeting);
        });
```

`GreetableType` here is a small `record` — deliberately, since it needs value equality for the
driver's caching to work (see
[specialized/incremental-pipeline-and-equatable-models.md](../specialized/incremental-pipeline-and-equatable-models.md)).

## Advanced use case: matching an attribute on a generic type's members

`ForAttributeWithMetadataName` matches on the declaration the attribute is directly applied to —
including a member of a generic type, where `TargetSymbol` resolves to the member symbol and the
containing type's type parameters are reached through `ContainingType.TypeParameters`:

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyApp.Generators.CacheableAttribute",
        predicate: static (node, _) => node is MethodDeclarationSyntax,
        transform: static (ctx, _) =>
        {
            var method = (IMethodSymbol)ctx.TargetSymbol;
            INamedTypeSymbol containingType = method.ContainingType;

            string typeParams = containingType.TypeParameters.Length == 0
                ? ""
                : $"<{string.Join(", ", containingType.TypeParameters.Select(tp => tp.Name))}>";

            return new CacheableMethod(containingType.Name, typeParams, method.Name);
        });
```

## Requirements and restrictions

- `fullyQualifiedMetadataName` must match the attribute's metadata name exactly, including the
  `Attribute` suffix and namespace — it does not accept a short name or a wildcard.
- The attribute type itself must be visible to the compilation being analyzed; a generator that
  both defines its own marker attribute and consumes it typically emits that attribute via
  `RegisterPostInitializationOutput` in the same `Initialize` call, before registering the
  `ForAttributeWithMetadataName` provider that looks for it (see
  [specialized/generator-project-setup-and-packaging.md](../specialized/generator-project-setup-and-packaging.md)
  for the multi-project duplication problem this creates, and the .NET 10 tier below for the fix).

## Fallback

Below the .NET 7 SDK,
[the `IIncrementalGenerator` tier](net6-iincrementalgenerator.md)'s `CreateSyntaxProvider` still
works for attribute-driven matching — write the predicate as
`node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0` (matching on any attribute
present, since a syntax-only predicate can't check the attribute's resolved name) and do the actual
name check inside the semantic `transform` delegate via
`ctx.SemanticModel.GetDeclaredSymbol(...).GetAttributes()`. It costs the predicate-side performance
`ForAttributeWithMetadataName` recovers, but produces the same generated output.
