# Diagnostics from a Generator

A generator is not limited to emitting source — it can report ordinary compiler `Diagnostic`s that
show up in the IDE's error list and fail the build, exactly like an analyzer's diagnostics, pointed
at the *user's* source location that caused the problem rather than at the generator's own code.
Assumes [net5-isourcegenerator.md](../references/net5-isourcegenerator.md) and
[net6-iincrementalgenerator.md](../references/net6-iincrementalgenerator.md) — both report
diagnostics through the same `DiagnosticDescriptor`/`Diagnostic.Create` shape, only the context
object reporting them differs.

## Basic: a `DiagnosticDescriptor` and reporting it

```csharp
private static readonly DiagnosticDescriptor MissingPartialModifier = new(
    id: "GEN001",
    title: "Type must be declared partial",
    messageFormat: "'{0}' is marked [Greetable] but is not declared 'partial'; the generator cannot add members to it",
    category: "MyApp.Generators",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);
```

```csharp
// IIncrementalGenerator: report from inside RegisterSourceOutput via SourceProductionContext.
context.RegisterSourceOutput(candidates, static (spc, candidate) =>
{
    if (!candidate.IsPartial)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            MissingPartialModifier,
            candidate.Location,
            candidate.Name));
        return;
    }

    spc.AddSource($"{candidate.Name}.g.cs", GenerateSource(candidate));
});
```

```csharp
// ISourceGenerator: report from Execute via GeneratorExecutionContext.
context.ReportDiagnostic(Diagnostic.Create(MissingPartialModifier, classDeclaration.GetLocation(), classDeclaration.Identifier.Text));
```

`{0}` in `messageFormat` is filled from the `params object?[]` passed after `location` in
`Diagnostic.Create` — here, `candidate.Name`.

## Basic: mapping the diagnostic back to the user's actual source location

The `Location` argument is what makes the diagnostic actionable — without it (or with
`Location.None`), the diagnostic still fails the build but gives the developer no squiggle to click
on. Pull the location from the syntax node the candidate was derived from, captured during the
pipeline's `transform` stage (see
[specialized/incremental-pipeline-and-equatable-models.md](incremental-pipeline-and-equatable-models.md)
for why only the location — not the whole `SyntaxNode` — should survive into the cached model):

```csharp
public record GreetableCandidate(string Name, bool IsPartial, Location Location);

context.SyntaxProvider
    .ForAttributeWithMetadataName("MyApp.Generators.GreetableAttribute",
        predicate: static (n, _) => n is ClassDeclarationSyntax,
        transform: static (ctx, _) =>
        {
            var classDecl = (ClassDeclarationSyntax)ctx.TargetNode;
            bool isPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
            return new GreetableCandidate(ctx.TargetSymbol.Name, isPartial, classDecl.Identifier.GetLocation());
        });
```

`Location` (unlike `SyntaxNode`/`ISymbol`) is a lightweight, self-contained value safe to keep in a
cached pipeline model — it doesn't pin the whole compilation the way a live symbol reference does.

## Advanced: severity and suppressing a diagnostic conditionally

`DiagnosticSeverity.Error` fails the build; `Warning` and `Info` surface without failing it.
A generator that supports an intentional escape hatch (a `SuppressGeneration = true` property on
its marker attribute, say) reads that value out of the resolved `AttributeData` during `transform`
and skips reporting entirely rather than always emitting a lower-severity diagnostic:

```csharp
if (candidate.SuppressGeneration)
{
    return; // no diagnostic, no generated source -- an intentional, silent opt-out
}

if (!candidate.IsPartial)
{
    spc.ReportDiagnostic(Diagnostic.Create(MissingPartialModifier, candidate.Location, candidate.Name));
}
```

## Advanced: a diagnostic for a generic type's unsatisfiable constraint interaction

Diagnostics are also the right tool for constraint-shape problems the generated code itself can't
work around — e.g. a generator that needs to synthesize a `new()` call and finds the target type
parameter has no parameterless-constructor constraint:

```csharp
private static readonly DiagnosticDescriptor MissingNewConstraint = new(
    "GEN002",
    "Type parameter requires a parameterless constructor",
    "Type parameter '{0}' on '{1}' must have a 'new()' constraint for the generator to construct instances of it",
    "MyApp.Generators",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

foreach (ITypeParameterSymbol typeParam in namedType.TypeParameters)
{
    if (!typeParam.HasConstructorConstraint)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            MissingNewConstraint,
            typeParam.Locations.FirstOrDefault() ?? Location.None,
            typeParam.Name,
            namedType.Name));
    }
}
```

## Fallback

None — `DiagnosticDescriptor` and `Diagnostic.Create` are part of the base `Microsoft.CodeAnalysis`
package referenced by every tier in this skill; only the reporting context type
(`GeneratorExecutionContext` vs. `SourceProductionContext`) differs between the
[ISourceGenerator](../references/net5-isourcegenerator.md) and
[IIncrementalGenerator](../references/net6-iincrementalgenerator.md) tiers, both shown above.
