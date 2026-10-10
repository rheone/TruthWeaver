namespace TruthWeaver.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TruthWeaver.Generators.Model;

/// <summary>
/// Generates predicate registration code. For each type that declares <c>[Predicate]</c> methods, the generator emits a
/// <c>Register(PredicateRegistryBuilder&lt;TContext&gt;)</c> method that adds each predicate with a schema built from
/// the method signature. The generated code calls the public registry API and uses no reflection, so it is trim and
/// AOT safe.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class PredicateRegistrationGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static output =>
        {
            output.AddEmbeddedAttributeDefinition();
            output.AddSource(PredicateAttributeSource.HintName, PredicateAttributeSource.Text);
        });

        IncrementalValuesProvider<MethodResult> methods = context.SyntaxProvider.ForAttributeWithMetadataName(
            PredicateAttributeSource.MetadataName,
            static (node, _) => node is MethodDeclarationSyntax,
            PredicateMethodReader.Read
        );

        // The methods are grouped in one step: a duplicate name and the Register overloads both need every method of
        // a type. Reading each method stays cached per method, so an edit re-reads only the changed method.
        context.RegisterSourceOutput(methods.Collect(), static (output, results) => Produce(output, results));
    }

    private static void Produce(SourceProductionContext output, ImmutableArray<MethodResult> results)
    {
        foreach (MethodResult result in results)
        {
            foreach (DiagnosticInfo diagnostic in result.Diagnostics)
            {
                output.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        }

        IEnumerable<IGrouping<string, PredicateMethod>> types = results
            .Select(r => r.Method)
            .OfType<PredicateMethod>()
            .GroupBy(m => m.Type.FullyQualifiedName, StringComparer.Ordinal);

        foreach (IGrouping<string, PredicateMethod> type in types)
        {
            PredicateMethod[] typeMethods = [.. Distinct(output, type)];
            if (typeMethods.Length == 0)
            {
                continue;
            }

            string hintName = type.Key.Replace("global::", string.Empty).Replace('<', '_').Replace('>', '_') + ".Register.g.cs";
            output.AddSource(hintName, RegisterEmitter.Emit(typeMethods[0].Type, typeMethods));
        }
    }

    /// <summary>
    /// Keeps the first method for each predicate name and reports TWG002 for each later one. The names are compared
    /// the way <c>PredicateRegistryBuilder</c> compares them, so a duplicate fails the build instead of the registration.
    /// </summary>
    private static IEnumerable<PredicateMethod> Distinct(SourceProductionContext output, IEnumerable<PredicateMethod> methods)
    {
        Dictionary<string, PredicateMethod> byName = [with(StringComparer.Ordinal)];
        foreach (PredicateMethod method in methods)
        {
            string key = method.PredicateName.ToUpperInvariant();
            if (byName.TryGetValue(key, out PredicateMethod? first))
            {
                output.ReportDiagnostic(
                    Diagnostic.Create(
                        GeneratorDiagnostics.DuplicatePredicateName,
                        method.Location?.ToLocation(),
                        method.MethodName,
                        method.PredicateName,
                        first.MethodName
                    )
                );
                continue;
            }

            byName.Add(key, method);
            yield return method;
        }
    }
}
