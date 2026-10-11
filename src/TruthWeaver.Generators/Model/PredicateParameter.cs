namespace TruthWeaver.Generators.Model;

/// <summary>One parameter of a predicate method, as the emitter needs it.</summary>
/// <param name="Role">What the parameter receives.</param>
/// <param name="Name">The parameter name. For an argument, this is also the argument name in rule text.</param>
/// <param name="Kind">The <c>LiteralKind</c> member name of an argument, or empty for the other roles.</param>
/// <param name="Description">The argument description from the <c>param</c> documentation comment, or empty.</param>
/// <param name="Default">The C# expression of the default <c>LiteralValue</c>, or <see langword="null"/> for a required argument.</param>
internal sealed record PredicateParameter(ParameterRole Role, string Name, string Kind, string Description, string? Default);
