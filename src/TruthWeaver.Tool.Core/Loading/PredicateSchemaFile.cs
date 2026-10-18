namespace TruthWeaver.Tool.Core.Loading;

using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Validation;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// The predicate schema file: a YAML file that declares the predicates a rule may use, with the name, the kind and
/// whether each argument is required. The tool has no host registry, so it compiles permissively; the strict schema
/// level of <see cref="RuleValidator"/> compiles the rule again against this file and reports each predicate or argument
/// that the file does not allow.
/// </summary>
/// <remarks>
/// <para>The file is a mapping with one member, <c>predicates</c>, a mapping from predicate name to declaration. A
/// declaration has the optional members <c>description</c> and <c>arguments</c>. <c>arguments</c> maps an argument name
/// to a kind, such as <c>string</c>, or to a mapping with <c>kind</c>, the optional <c>required</c> (default
/// <c>true</c>) and the optional <c>description</c>. A predicate with no arguments can have an empty declaration.</para>
/// <para>A kind is a <see cref="LiteralKind"/> name, with any case: <c>string</c>, <c>int64</c>, <c>decimal</c>,
/// <c>boolean</c>, <c>dateTimeOffset</c>, <c>guid</c>, and each of them with <c>Array</c> at the end. A document that is not
/// well-formed YAML is a <see cref="ToolDiagnosticCodes.MalformedDocument"/> error. An unknown member, an unknown kind,
/// a reserved predicate name and two names that differ only in case are
/// <see cref="ToolDiagnosticCodes.InvalidSchemaFile"/> errors.</para>
/// </remarks>
public sealed class PredicateSchemaFile
{
    private const string DefaultDescription = "Declared in the predicate schema file.";

    private PredicateSchemaFile(IReadOnlyList<PredicateSchema> predicates, PredicateRegistry<StubAssignment> registry)
    {
        this.Predicates = predicates;
        this.Registry = registry;
    }

    /// <summary>Gets the declared predicates, in the order of the file.</summary>
    public IReadOnlyList<PredicateSchema> Predicates { get; }

    /// <summary>Gets a registry of the declared predicates. Each predicate evaluates <c>Unknown</c>; the registry is for compiling only.</summary>
    internal PredicateRegistry<StubAssignment> Registry { get; }

    /// <summary>Reads a predicate schema file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The schema, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<PredicateSchemaFile> Load(string path)
    {
        ToolResult<SourceText> read = SourceReader.ReadFile(path);
        if (!read.TryGetValue(out SourceText? source, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<PredicateSchemaFile>(diagnostics);
        }

        return Parse(source.Text);
    }

    /// <summary>Reads predicate schema text.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <returns>The schema, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml"/> is <see langword="null"/>.</exception>
    public static ToolResult<PredicateSchemaFile> Parse(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        YamlStream stream = [];
        try
        {
            using StringReader reader = new(yaml);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            return ToolResults.Failure<PredicateSchemaFile>(
                ToolDiagnosticCodes.MalformedDocument,
                $"The predicate schema file is not well-formed YAML (line {ex.Start.Line}, column {ex.Start.Column})."
            );
        }

        List<Diagnostic> diagnostics = [];
        List<PredicateSchema> predicates = [];
        YamlNode? root = stream.Documents.Count == 0 ? null : stream.Documents[0].RootNode;
        if (root is not YamlMappingNode document)
        {
            return ToolResults.Failure<PredicateSchemaFile>(
                ToolDiagnosticCodes.InvalidSchemaFile,
                "The predicate schema file must be a YAML mapping with the member 'predicates'.",
                "$"
            );
        }

        foreach (KeyValuePair<YamlNode, YamlNode> member in document.Children)
        {
            if (Name(member.Key) != "predicates")
            {
                diagnostics.Add(Invalid($"The member '{Name(member.Key)}' is not known.", "$"));
            }
            else if (member.Value is YamlMappingNode declarations)
            {
                foreach (KeyValuePair<YamlNode, YamlNode> declaration in declarations.Children)
                {
                    predicates.Add(ReadPredicate(Name(declaration.Key), declaration.Value, diagnostics));
                }
            }
            else if (!IsEmpty(member.Value))
            {
                diagnostics.Add(Invalid("'predicates' must be a mapping from predicate name to declaration.", "$.predicates"));
            }
        }

        PredicateRegistry<StubAssignment>? registry = diagnostics.Count == 0 ? BuildRegistry(predicates, diagnostics) : null;
        return registry is null
            ? ToolResults.Failure<PredicateSchemaFile>(diagnostics)
            : ToolResults.Success(new PredicateSchemaFile(predicates, registry));
    }

    private static PredicateSchema ReadPredicate(string name, YamlNode node, List<Diagnostic> diagnostics)
    {
        string path = "$.predicates['" + name + "']";
        string description = DefaultDescription;
        List<PredicateArgumentSchema> arguments = [];
        if (node is YamlMappingNode declaration)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> member in declaration.Children)
            {
                switch (Name(member.Key))
                {
                    case "description":
                        description = Name(member.Value);
                        break;
                    case "arguments" when member.Value is YamlMappingNode argumentNodes:
                        foreach (KeyValuePair<YamlNode, YamlNode> argument in argumentNodes.Children)
                        {
                            PredicateArgumentSchema? schema = ReadArgument(
                                Name(argument.Key),
                                argument.Value,
                                path + ".arguments",
                                diagnostics
                            );
                            if (schema is not null)
                            {
                                arguments.Add(schema);
                            }
                        }

                        break;
                    case "arguments" when IsEmpty(member.Value):
                        break;
                    case "arguments":
                        diagnostics.Add(Invalid("'arguments' must be a mapping from argument name to kind.", path));
                        break;
                    default:
                        diagnostics.Add(Invalid($"The member '{Name(member.Key)}' is not known.", path));
                        break;
                }
            }
        }
        else if (!IsEmpty(node))
        {
            diagnostics.Add(Invalid($"The declaration of '{name}' must be a mapping.", path));
        }

        return new PredicateSchema(name, name, description, arguments);
    }

    private static PredicateArgumentSchema? ReadArgument(
        string name,
        YamlNode node,
        string parentPath,
        List<Diagnostic> diagnostics
    )
    {
        string path = parentPath + "['" + name + "']";
        string? kindText = null;
        bool required = true;
        string description = DefaultDescription;
        if (node is YamlScalarNode scalar)
        {
            kindText = scalar.Value;
        }
        else if (node is YamlMappingNode declaration)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> member in declaration.Children)
            {
                switch (Name(member.Key))
                {
                    case "kind":
                        kindText = Name(member.Value);
                        break;
                    case "required" when bool.TryParse(Name(member.Value), out bool value):
                        required = value;
                        break;
                    case "required":
                        diagnostics.Add(Invalid("'required' must be true or false.", path));
                        break;
                    case "description":
                        description = Name(member.Value);
                        break;
                    default:
                        diagnostics.Add(Invalid($"The member '{Name(member.Key)}' is not known.", path));
                        break;
                }
            }
        }

        // Names only: Enum.TryParse would also take a number such as "3".
        string? kindName = Enum.GetNames<LiteralKind>()
            .FirstOrDefault(n => string.Equals(n, kindText, StringComparison.OrdinalIgnoreCase));
        if (kindName is null)
        {
            diagnostics.Add(
                Invalid(
                    $"The argument '{name}' needs a kind: string, int64, decimal, boolean, dateTimeOffset, guid, or one of them with 'Array' at the end.",
                    path
                )
            );
            return null;
        }

        return new PredicateArgumentSchema(name, description, Enum.Parse<LiteralKind>(kindName), required);
    }

    // The registry builder rejects a reserved name, a name used twice (ignoring case) and two arguments whose names differ
    // only in case. It has no Try form, so its exception is the finding here.
    private static PredicateRegistry<StubAssignment>? BuildRegistry(
        List<PredicateSchema> predicates,
        List<Diagnostic> diagnostics
    )
    {
        PredicateRegistryBuilder<StubAssignment> builder = PredicateRegistry<StubAssignment>.CreateBuilder();
        foreach (PredicateSchema predicate in predicates)
        {
            try
            {
                _ = builder.Add(predicate, static (_, _, _) => ValueTask.FromResult(TruthValue.Unknown));
            }
            catch (ArgumentException ex)
            {
                diagnostics.Add(Invalid(ex.Message, "$.predicates['" + predicate.Name + "']"));
            }
        }

        return diagnostics.Count == 0 ? builder.Build() : null;
    }

    private static string Name(YamlNode node)
    {
        return node is YamlScalarNode { Value: { } value } ? value : string.Empty;
    }

    private static bool IsEmpty(YamlNode node)
    {
        return node is YamlScalarNode { Style: ScalarStyle.Plain, Value: "" or "~" or "null" or null };
    }

    private static Diagnostic Invalid(string message, string path)
    {
        return ToolResults.Error(ToolDiagnosticCodes.InvalidSchemaFile, message, path);
    }
}
