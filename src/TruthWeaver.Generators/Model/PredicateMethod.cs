namespace TruthWeaver.Generators.Model;

/// <summary>A valid predicate method, with everything the emitter needs to register it.</summary>
/// <param name="Type">The declaring type.</param>
/// <param name="MethodName">The method name.</param>
/// <param name="PredicateName">The registered predicate name.</param>
/// <param name="Label">The display label.</param>
/// <param name="Description">The predicate description.</param>
/// <param name="ContextType">The fully qualified context type, the <c>TContext</c> of the registry builder.</param>
/// <param name="ContextIsPublic">Whether the context type is public. A public <c>Register</c> method needs a public context type.</param>
/// <param name="Return">The return type of the method.</param>
/// <param name="Parameters">The parameters, in declaration order.</param>
/// <param name="Location">The location of the method name, for a duplicate-name diagnostic.</param>
internal sealed record PredicateMethod(
    ContainingType Type,
    string MethodName,
    string PredicateName,
    string Label,
    string Description,
    string ContextType,
    bool ContextIsPublic,
    ReturnShape Return,
    EquatableArray<PredicateParameter> Parameters,
    LocationInfo? Location
);
