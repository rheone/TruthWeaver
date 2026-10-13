namespace TruthWeaver.Tool.Core;

using System.Text.Json;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;

/// <summary>
/// Finds and reads the <c>truthweaver.json</c> configuration file. The file is one <see cref="ToolConfiguration"/> layer;
/// <see cref="ToolSettings.Resolve"/> puts the command line over it and the defaults under it. The JSON schema of the file
/// is <c>truthweaver.schema.json</c>, next to the assembly.
/// </summary>
/// <remarks>
/// The reader accepts the members of the schema: <c>operatorStyle</c>, <c>constantStyle</c>, <c>dialect</c>,
/// <c>limits</c> (<c>maxDepth</c>, <c>maxNodeCount</c>, <c>maxAnalysisTerms</c>, <c>maxRewriteNodeCount</c>) and
/// <c>schemaFile</c>, plus <c>$schema</c>. Property names match exactly. Enumeration values ignore case; the schema lists them in lower case. A value of the
/// wrong type, an unknown enumeration value or a limit below 1 is a <see cref="ToolDiagnosticCodes.InvalidConfiguration"/>
/// error. An unknown property is a <see cref="ToolDiagnosticCodes.UnknownConfigurationProperty"/> warning, and the reader
/// ignores it.
/// </remarks>
public static class ToolConfigurationFile
{
    /// <summary>The file name the tool looks for.</summary>
    public const string FileName = "truthweaver.json";

    /// <summary>The file name of the JSON schema of the configuration file.</summary>
    public const string SchemaFileName = "truthweaver.schema.json";

    /// <summary>
    /// Finds the configuration file for a directory: <c>truthweaver.json</c> in <paramref name="startDirectory"/> or the
    /// nearest of its parent directories.
    /// </summary>
    /// <param name="startDirectory">The directory to start in, usually the current directory or the directory of the rule file.</param>
    /// <returns>The full path of the file, or <see langword="null"/> when no directory up to the root has one.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="startDirectory"/> is <see langword="null"/>.</exception>
    public static string? Find(string startDirectory)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);

        for (
            DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));
            directory is not null;
            directory = directory.Parent
        )
        {
            string candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Reads a configuration file. A relative <c>schemaFile</c> is made absolute from the directory of the file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The configuration layer, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<ToolConfiguration> Load(string path)
    {
        ToolResult<SourceText> read = SourceReader.ReadFile(path);
        if (!read.TryGetValue(out SourceText? source, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<ToolConfiguration>(diagnostics);
        }

        return Parse(source.Text, Path.GetDirectoryName(Path.GetFullPath(path)));
    }

    /// <summary>Reads configuration text.</summary>
    /// <param name="json">The JSON text.</param>
    /// <param name="baseDirectory">The directory a relative <c>schemaFile</c> is relative to, or <see langword="null"/> to keep it as written.</param>
    /// <returns>The configuration layer, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static ToolResult<ToolConfiguration> Parse(string json, string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return ToolResults.Failure<ToolConfiguration>(
                ToolDiagnosticCodes.MalformedDocument,
                $"The configuration file is not well-formed JSON (line {ex.LineNumber + 1}, byte position {ex.BytePositionInLine + 1})."
            );
        }

        using (document)
        {
            List<Diagnostic> diagnostics = [];
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ToolResults.Failure<ToolConfiguration>(
                    ToolDiagnosticCodes.InvalidConfiguration,
                    "The configuration must be a JSON object.",
                    "$"
                );
            }

            ToolConfiguration configuration = new();
            using JsonElement.ObjectEnumerator properties = document.RootElement.EnumerateObject();
            foreach (JsonProperty property in properties)
            {
                string path = "$." + property.Name;
                configuration = property.Name switch
                {
                    "$schema" => configuration,
                    "operatorStyle" => configuration with
                    {
                        OperatorStyle = ReadEnum<OperatorStyle>(property.Value, path, diagnostics),
                    },
                    "constantStyle" => configuration with
                    {
                        ConstantStyle = ReadEnum<ConstantStyle>(property.Value, path, diagnostics),
                    },
                    "dialect" => configuration with { Dialect = ReadEnum<EquationDialect>(property.Value, path, diagnostics) },
                    "limits" => ReadLimits(configuration, property.Value, path, diagnostics),
                    "schemaFile" => configuration with
                    {
                        SchemaFile = ReadPath(property.Value, path, baseDirectory, diagnostics),
                    },
                    _ => Unknown(configuration, path, diagnostics),
                };
            }

            return diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error)
                ? ToolResults.Failure<ToolConfiguration>(diagnostics)
                : ToolResults.Success(configuration, diagnostics);
        }
    }

    private static ToolConfiguration ReadLimits(
        ToolConfiguration configuration,
        JsonElement value,
        string path,
        List<Diagnostic> diagnostics
    )
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(Invalid(path, "an object of limits", value));
            return configuration;
        }

        using JsonElement.ObjectEnumerator limits = value.EnumerateObject();
        foreach (JsonProperty limit in limits)
        {
            string limitPath = path + "." + limit.Name;
            configuration = limit.Name switch
            {
                "maxDepth" => configuration with { MaxDepth = ReadLimit(limit.Value, limitPath, diagnostics) },
                "maxNodeCount" => configuration with { MaxNodeCount = ReadLimit(limit.Value, limitPath, diagnostics) },
                "maxAnalysisTerms" => configuration with { MaxAnalysisTerms = ReadLimit(limit.Value, limitPath, diagnostics) },
                "maxRewriteNodeCount" => configuration with
                {
                    MaxRewriteNodeCount = ReadLimit(limit.Value, limitPath, diagnostics),
                },
                _ => Unknown(configuration, limitPath, diagnostics),
            };
        }

        return configuration;
    }

    private static int? ReadLimit(JsonElement value, string path, List<Diagnostic> diagnostics)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int limit) && limit >= 1)
        {
            return limit;
        }

        diagnostics.Add(Invalid(path, "a whole number from 1 to 2147483647", value));
        return null;
    }

    private static TEnum? ReadEnum<TEnum>(JsonElement value, string path, List<Diagnostic> diagnostics)
        where TEnum : struct, Enum
    {
        // Names only: Enum.TryParse would also take a number such as "1", which is no style a reader can recognize.
        if (value.ValueKind == JsonValueKind.String)
        {
            string text = value.GetString()!;
            foreach (TEnum member in Enum.GetValues<TEnum>())
            {
                if (string.Equals(member.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    return member;
                }
            }
        }

        string names = string.Join(", ", Enum.GetNames<TEnum>().Select(name => name.ToLowerInvariant()));
        diagnostics.Add(Invalid(path, $"one of {names}", value));
        return null;
    }

    private static string? ReadPath(JsonElement value, string path, string? baseDirectory, List<Diagnostic> diagnostics)
    {
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            diagnostics.Add(Invalid(path, "a file path", value));
            return null;
        }

        string file = value.GetString()!;
        return baseDirectory is null || Path.IsPathFullyQualified(file)
            ? file
            : Path.GetFullPath(Path.Combine(baseDirectory, file));
    }

    private static ToolConfiguration Unknown(ToolConfiguration configuration, string path, List<Diagnostic> diagnostics)
    {
        diagnostics.Add(
            ToolResults.Warning(
                ToolDiagnosticCodes.UnknownConfigurationProperty,
                $"The configuration property '{path}' is not known and is ignored.",
                path
            )
        );
        return configuration;
    }

    private static Diagnostic Invalid(string path, string expected, JsonElement found)
    {
        string foundText = found.ValueKind == JsonValueKind.String ? $"\"{found.GetString()}\"" : found.GetRawText();
        return Diagnostic.Error(
            ToolDiagnosticCodes.InvalidConfiguration,
            $"The configuration value at '{path}' is not valid: expected {expected}.",
            SourceSpan.None,
            expected: expected,
            found: foundText,
            path: path
        );
    }
}
