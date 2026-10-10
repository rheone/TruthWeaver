namespace TruthWeaver.Generators;

using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TruthWeaver.Generators.Model;

/// <summary>
/// Reads a method marked with <c>[Predicate]</c> into a <see cref="PredicateMethod"/>, or into the diagnostics that
/// explain why the generator cannot register it.
/// </summary>
internal static class PredicateMethodReader
{
    private const string AbstractionsNamespace = "TruthWeaver.Abstractions";

    /// <summary>Reads one marked method.</summary>
    /// <param name="context">The attribute match from the syntax provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The method model, the diagnostics, or both.</returns>
    public static MethodResult Read(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        IMethodSymbol method = (IMethodSymbol)context.TargetSymbol;
        AttributeData attribute = context.Attributes[0];
        Location? location = ((MethodDeclarationSyntax)context.TargetNode).Identifier.GetLocation();
        ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        // A method the delegate cannot call gets one diagnostic and no further checks: its parameters have no roles.
        string? shapeProblem = ReadShapeProblem(method);
        if (shapeProblem is not null)
        {
            diagnostics.Add(
                DiagnosticInfo.Create(GeneratorDiagnostics.InvalidMethodShape, location, method.Name, shapeProblem)
            );
            return new MethodResult(null, new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable()));
        }

        for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            string? typeProblem = ReadTypeProblem(type, cancellationToken);
            if (typeProblem is not null)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(GeneratorDiagnostics.TypeNotPartial, location, type.Name, method.Name, typeProblem)
                );
                return new MethodResult(null, new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable()));
            }
        }

        ContainingType containingType = ReadContainingType(method.ContainingType);
        ReturnShape? returnShape = ReadReturnShape(method.ReturnType);
        if (returnShape is null)
        {
            Location? returnLocation = ((MethodDeclarationSyntax)context.TargetNode).ReturnType.GetLocation();
            diagnostics.Add(
                DiagnosticInfo.Create(
                    GeneratorDiagnostics.WrongReturnType,
                    returnLocation,
                    method.Name,
                    method.ReturnType.ToDisplayString()
                )
            );
        }

        Dictionary<string, string> descriptions = ReadParameterDescriptions(method, cancellationToken);

        ImmutableArray<PredicateParameter>.Builder parameters = ImmutableArray.CreateBuilder<PredicateParameter>();
        for (int index = 0; index < method.Parameters.Length; index++)
        {
            IParameterSymbol parameter = method.Parameters[index];
            if (index == 0)
            {
                parameters.Add(new PredicateParameter(ParameterRole.Context, parameter.Name, string.Empty, string.Empty, null));
                continue;
            }

            if (IsCancellationToken(parameter.Type))
            {
                parameters.Add(
                    new PredicateParameter(ParameterRole.CancellationToken, parameter.Name, string.Empty, string.Empty, null)
                );
                continue;
            }

            string? kind = ReadLiteralKind(parameter.Type);
            if (kind is null)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        GeneratorDiagnostics.UnsupportedParameterType,
                        parameter.Locations.FirstOrDefault(),
                        parameter.Name,
                        method.Name,
                        parameter.Type.ToDisplayString()
                    )
                );
                continue;
            }

            string? defaultValue = parameter.HasExplicitDefaultValue ? ReadDefault(kind, parameter.ExplicitDefaultValue) : null;
            if (parameter.HasExplicitDefaultValue && defaultValue is null)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        GeneratorDiagnostics.UnsupportedDefaultValue,
                        parameter.Locations.FirstOrDefault(),
                        parameter.Name,
                        method.Name
                    )
                );
                continue;
            }

            descriptions.TryGetValue(parameter.Name, out string? description);
            parameters.Add(
                new PredicateParameter(ParameterRole.Argument, parameter.Name, kind, description ?? string.Empty, defaultValue)
            );
        }

        if (returnShape is null || diagnostics.Count > 0)
        {
            return new MethodResult(null, new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable()));
        }

        ITypeSymbol contextType = method.Parameters[0].Type;
        PredicateMethod model = new(
            containingType,
            method.Name,
            ReadString(attribute, 0),
            ReadString(attribute, 1),
            ReadString(attribute, 2),
            contextType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsEffectivelyPublic(contextType),
            returnShape.Value,
            new EquatableArray<PredicateParameter>(parameters.ToImmutable()),
            LocationInfo.From(location)
        );
        return new MethodResult(model, new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable()));
    }

    /// <summary>Describes why the generated delegate cannot call the method, or returns <see langword="null"/>.</summary>
    private static string? ReadShapeProblem(IMethodSymbol method)
    {
        if (!method.IsStatic)
        {
            return "is not static";
        }

        if (method.IsGenericMethod)
        {
            return "is generic";
        }

        if (method.Parameters.Length == 0)
        {
            return "has no context parameter";
        }

        IParameterSymbol? byReference = method.Parameters.FirstOrDefault(p => p.RefKind != RefKind.None);
        return byReference is null ? null : "passes parameter '" + byReference.Name + "' by reference";
    }

    /// <summary>Describes why the generator cannot add a partial declaration of the type, or returns <see langword="null"/>.</summary>
    private static string? ReadTypeProblem(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        if (type.IsGenericType)
        {
            return "is generic";
        }

        bool isPartial = type.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax declaration
            && declaration.Modifiers.Any(SyntaxKind.PartialKeyword)
        );
        return isPartial ? null : "is not partial";
    }

    private static ContainingType ReadContainingType(INamedTypeSymbol type)
    {
        List<string> declarations = [];
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            declarations.Insert(0, "partial " + DeclarationKeyword(current) + " " + current.Name);
        }

        string ns = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
        return new ContainingType(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ns,
            new EquatableArray<string>([.. declarations])
        );
    }

    private static string DeclarationKeyword(INamedTypeSymbol type)
    {
        if (type.IsRecord)
        {
            return type.IsValueType ? "record struct" : "record";
        }

        return type.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class",
        };
    }

    private static ReturnShape? ReadReturnShape(ITypeSymbol returnType)
    {
        if (IsTruthValue(returnType))
        {
            return ReturnShape.TruthValue;
        }

        if (
            returnType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } generic
            && IsTruthValue(generic.TypeArguments[0])
        )
        {
            string definition = generic.ConstructedFrom.ToDisplayString();
            if (definition == "System.Threading.Tasks.ValueTask<TResult>")
            {
                return ReturnShape.ValueTask;
            }

            if (definition == "System.Threading.Tasks.Task<TResult>")
            {
                return ReturnShape.Task;
            }
        }

        return null;
    }

    private static bool IsTruthValue(ITypeSymbol type)
    {
        return type.Name == "TruthValue" && type.ContainingNamespace?.ToDisplayString() == AbstractionsNamespace;
    }

    private static bool IsCancellationToken(ITypeSymbol type)
    {
        return type.ToDisplayString() == "System.Threading.CancellationToken";
    }

    /// <summary>Maps a parameter type to the name of its <c>LiteralKind</c> member, or <see langword="null"/> when no kind fits.</summary>
    private static string? ReadLiteralKind(ITypeSymbol type)
    {
        string? scalar = ReadScalarKind(type);
        if (scalar is not null)
        {
            return scalar;
        }

        // PredicateArguments returns array arguments as IReadOnlyList<T>, so that is the one supported array shape.
        if (
            type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } list
            && list.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IReadOnlyList<T>"
        )
        {
            string? element = ReadScalarKind(list.TypeArguments[0]);
            return element is null ? null : element + "Array";
        }

        return null;
    }

    private static string? ReadScalarKind(ITypeSymbol type)
    {
        // A nullable reference annotation does not change the kind; the engine never passes null for an argument.
        return type.SpecialType switch
        {
            SpecialType.System_String => "String",
            SpecialType.System_Int64 => "Int64",
            SpecialType.System_Decimal => "Decimal",
            SpecialType.System_Boolean => "Boolean",
            _ => type.ToDisplayString() switch
            {
                "System.DateTimeOffset" => "DateTimeOffset",
                "System.Guid" => "Guid",
                _ => null,
            },
        };
    }

    /// <summary>
    /// Writes the C# expression of a default <c>LiteralValue</c>, or returns <see langword="null"/> when the default
    /// has no literal form (a <see langword="null"/> or <see langword="default"/> value).
    /// </summary>
    private static string? ReadDefault(string kind, object? value)
    {
        const string literalValue = "global::TruthWeaver.Abstractions.LiteralValue.";
        return (kind, value) switch
        {
            ("String", string text) => literalValue + "OfString(" + SymbolDisplay.FormatLiteral(text, true) + ")",
            ("Int64", long number) => literalValue + "OfInt64(" + number.ToString(CultureInfo.InvariantCulture) + "L)",
            ("Decimal", decimal number) => literalValue + "OfDecimal(" + number.ToString(CultureInfo.InvariantCulture) + "m)",
            ("Boolean", bool flag) => literalValue + "OfBoolean(" + (flag ? "true" : "false") + ")",
            _ => null,
        };
    }

    /// <summary>Reads the <c>param</c> documentation comments of the method, keyed by parameter name.</summary>
    private static Dictionary<string, string> ReadParameterDescriptions(
        IMethodSymbol method,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, string> descriptions = new(StringComparer.Ordinal);
        string? xml = method.GetDocumentationCommentXml(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return descriptions;
        }

        XElement root;
        try
        {
            root = XElement.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            // Malformed documentation is the compiler's warning to report. The arguments get empty descriptions.
            return descriptions;
        }

        foreach (XElement param in root.Elements("param"))
        {
            string? name = (string?)param.Attribute("name");
            if (name is not null)
            {
                descriptions[name] = Flatten(param);
            }
        }

        return descriptions;
    }

    /// <summary>Turns a documentation element into plain text: a <c>see</c> element becomes its keyword or type name.</summary>
    private static string Flatten(XElement element)
    {
        StringBuilder text = new();
        foreach (XNode node in element.Nodes())
        {
            if (node is XText plain)
            {
                text.Append(plain.Value);
            }
            else if (node is XElement child)
            {
                string? reference =
                    (string?)child.Attribute("langword")
                    ?? (string?)child.Attribute("cref")
                    ?? (string?)child.Attribute("name");
                text.Append(child.Nodes().Any() ? Flatten(child) : ShortName(reference));
            }
        }

        return string.Join(" ", text.ToString().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ShortName(string? reference)
    {
        if (reference is null)
        {
            return string.Empty;
        }

        // A cref is written as "T:Namespace.Type" in the documentation XML. The text keeps the last segment.
        int separator = Math.Max(reference.LastIndexOf('.'), reference.LastIndexOf(':'));
        return reference.Substring(separator + 1);
    }

    private static string ReadString(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is string value
            ? value
            : string.Empty;
    }

    private static bool IsEffectivelyPublic(ITypeSymbol type)
    {
        for (ISymbol? current = type; current is ITypeSymbol; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        if (type is INamedTypeSymbol { IsGenericType: true } generic)
        {
            return generic.TypeArguments.All(IsEffectivelyPublic);
        }

        return true;
    }
}
