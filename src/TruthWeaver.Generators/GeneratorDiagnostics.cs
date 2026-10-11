namespace TruthWeaver.Generators;

using Microsoft.CodeAnalysis;

/// <summary>
/// The diagnostics of the predicate registration generator. Each one is an error: a method the generator cannot
/// register fails the build, so the problem never reaches run time.
/// </summary>
internal static class GeneratorDiagnostics
{
    /// <summary>The category of every generator diagnostic.</summary>
    public const string Category = "TruthWeaver.Generators";

    /// <summary>TWG001: a parameter type that has no literal kind.</summary>
    public static readonly DiagnosticDescriptor UnsupportedParameterType = new(
        "TWG001",
        "Unsupported predicate parameter type",
        "Parameter '{0}' of predicate method '{1}' has type '{2}', which is not a predicate argument type. Use string, long, decimal, bool, DateTimeOffset, Guid, or IReadOnlyList<T> of one of them.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>TWG002: two predicate methods of one type with the same name, ignoring case.</summary>
    public static readonly DiagnosticDescriptor DuplicatePredicateName = new(
        "TWG002",
        "Duplicate predicate name",
        "Predicate method '{0}' uses the name '{1}', which method '{2}' of the same type already uses. Predicate names are not case-sensitive.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>TWG003: a predicate method that does not return a truth value.</summary>
    public static readonly DiagnosticDescriptor WrongReturnType = new(
        "TWG003",
        "Predicate method does not return a truth value",
        "Predicate method '{0}' returns '{1}'. A predicate method returns TruthValue, ValueTask<TruthValue> or Task<TruthValue>.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>TWG004: a predicate method that the generated delegate cannot call.</summary>
    public static readonly DiagnosticDescriptor InvalidMethodShape = new(
        "TWG004",
        "Predicate method has an unsupported shape",
        "Predicate method '{0}' {1}. A predicate method is static and not generic, its first parameter is the context, and it has no ref, out or in parameters.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>TWG005: a declaring type that the generator cannot add <c>Register</c> to.</summary>
    public static readonly DiagnosticDescriptor TypeNotPartial = new(
        "TWG005",
        "Type of a predicate method must be partial and not generic",
        "Type '{0}' around predicate method '{1}' {2}. The generator adds the Register method to the type, so the type and each type around it must be partial and not generic.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>TWG006: a default parameter value that has no literal form.</summary>
    public static readonly DiagnosticDescriptor UnsupportedDefaultValue = new(
        "TWG006",
        "Unsupported default value for a predicate argument",
        "Parameter '{0}' of predicate method '{1}' has a default value that a predicate schema cannot hold. Use a non-null string, long, decimal or bool constant, or remove the default to make the argument required.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>TWG007: a predicate name that the rule text reads as a keyword.</summary>
    public static readonly DiagnosticDescriptor ReservedPredicateName = new(
        "TWG007",
        "Reserved predicate name",
        "Predicate method '{0}' uses the name '{1}', which the rule text reads as a keyword. Choose another name.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
