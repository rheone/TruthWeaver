namespace TruthWeaver.Generators.Model;

/// <summary>The type that declares predicate methods, and the type declarations around it.</summary>
/// <param name="FullyQualifiedName">The fully qualified name, with the <c>global::</c> prefix. It groups the methods of one type.</param>
/// <param name="Namespace">The namespace, or empty for the global namespace.</param>
/// <param name="Declarations">
/// The partial declarations to emit, from the outermost type to the declaring type, for example <c>partial class Outer</c>.
/// </param>
internal sealed record ContainingType(string FullyQualifiedName, string Namespace, EquatableArray<string> Declarations);
