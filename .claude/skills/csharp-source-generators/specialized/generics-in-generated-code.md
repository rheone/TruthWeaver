# Generics in Generated Code

Generating code for or from a generic type is an ordinary part of authoring a generator — reading a
type's type parameters and constraints off its symbol to decide what's safe to emit, and writing
those same type parameters and constraints back out correctly in generated source. This file is
about that mechanic, not about what generic constraints mean in general. Assumes
[net5-isourcegenerator.md](../references/net5-isourcegenerator.md) or
[net6-iincrementalgenerator.md](../references/net6-iincrementalgenerator.md) for the symbol/syntax
APIs used below.

## Basic: reading a generic type's parameters and constraints off its symbol

`INamedTypeSymbol.TypeParameters` gives each `ITypeParameterSymbol`; each of those exposes its
constraint shape through `ConstraintTypes` (interface/base-class constraints),
`HasReferenceTypeConstraint`, `HasValueTypeConstraint`, `HasConstructorConstraint`, and
`HasNotNullConstraint` — everything needed to reproduce the constraint clause in generated text:

```csharp
static string BuildConstraintClause(ITypeParameterSymbol typeParam)
{
    List<string> constraints = new();

    if (typeParam.HasReferenceTypeConstraint) constraints.Add("class");
    if (typeParam.HasValueTypeConstraint) constraints.Add("struct");
    if (typeParam.HasNotNullConstraint) constraints.Add("notnull");
    constraints.AddRange(typeParam.ConstraintTypes.Select(t => t.ToDisplayString()));
    if (typeParam.HasConstructorConstraint) constraints.Add("new()"); // must be listed last

    return constraints.Count == 0 ? "" : $"where {typeParam.Name} : {string.Join(", ", constraints)}";
}
```

`new()` must be the last entry in a constraint list if present — the same ordering rule that
applies to hand-written C# applies to generated text, since it's parsed by the same compiler.

## Basic: emitting a generic partial type with its constraints intact

```csharp
INamedTypeSymbol type = candidate.Symbol;

string typeParamList = type.TypeParameters.Length == 0
    ? ""
    : $"<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>";

string constraintClauses = string.Join(
    "\n    ",
    type.TypeParameters.Select(BuildConstraintClause).Where(c => c.Length > 0));

string source = $$"""
    namespace {{type.ContainingNamespace}};

    partial class {{type.Name}}{{typeParamList}}
        {{constraintClauses}}
    {
        public override string ToString() => nameof({{type.Name}});
    }
    """;
```

Omitting a constraint the original declaration has is a real bug class here, not just a style
choice — a `partial` type's constraint clauses must match across every partial declaration
(CS0265), so a generator's copy must exactly mirror what the user wrote, not merely be *compatible*
with it.

## Advanced: emitting a generic method inside a non-generic containing type

A generator target is often a *method's own* type parameters rather than its containing type's —
`IMethodSymbol.TypeParameters` and `IMethodSymbol.IsGenericMethod` distinguish that case, and the
generated method needs its own `<T>`/`where` clause independent of whatever the containing type
declares:

```csharp
IMethodSymbol method = candidate.Method;

string methodTypeParams = method.TypeParameters.Length == 0
    ? ""
    : $"<{string.Join(", ", method.TypeParameters.Select(tp => tp.Name))}>";

string methodConstraints = string.Join(
    " ",
    method.TypeParameters.Select(BuildConstraintClause).Where(c => c.Length > 0));

string source = $$"""
    partial class {{method.ContainingType.Name}}
    {
        public partial {{method.ReturnType.ToDisplayString()}} {{method.Name}}{{methodTypeParams}}({{FormatParameters(method)}}) {{methodConstraints}}
        {
            // generated body
        }
    }
    """;
```

This is the shape a generator uses to implement a user-declared `partial` generic method (the
"partial method with a generated body" pattern) — the user writes the signature including its own
type parameters and constraints, the generator reads that signature back off the `IMethodSymbol`
and reproduces it exactly on the implementing declaration.

## Advanced: generating one output per closed generic instantiation found in source

Some generators need to react to *usages* of a generic type with specific type arguments — e.g. a
generator that emits a specialized fast-path converter for every distinct `Repository<T>`
instantiation it finds, rather than once per open generic definition. Collect the closed type
arguments found across candidate sites and de-duplicate before emitting, since the same closed
generic instantiation commonly appears at many call sites:

```csharp
IncrementalValuesProvider<INamedTypeSymbol> closedInstantiations = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: static (n, _) => n is ObjectCreationExpressionSyntax { Type: GenericNameSyntax },
        transform: static (ctx, _) =>
            ctx.SemanticModel.GetTypeInfo(ctx.Node).Type as INamedTypeSymbol)
    .Where(static t => t is { IsGenericType: true, IsUnboundGenericType: false })!;

IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> distinct = closedInstantiations
    .Collect()
    .Select(static (types, _) => types
        .Distinct(SymbolEqualityComparer.Default)
        .ToImmutableArray());
```

`SymbolEqualityComparer.Default` (not the default `Equals`) is required here — two
`INamedTypeSymbol` instances for the same closed generic type from the same compilation compare
equal under it even though plain reference or default equality may not, and it is what the
Roslyn SDK documents as the correct comparer for symbol de-duplication.

## Fallback

None — `ITypeParameterSymbol`/`INamedTypeSymbol.TypeParameters` and `SymbolEqualityComparer` are
part of the base `Microsoft.CodeAnalysis` symbol API available from
[net5-isourcegenerator.md](../references/net5-isourcegenerator.md) onward; nothing here is
tier-gated beyond which generator interface (`ISourceGenerator` vs. `IIncrementalGenerator`) reads
the symbol.
