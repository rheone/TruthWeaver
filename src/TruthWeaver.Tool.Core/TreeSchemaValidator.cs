namespace TruthWeaver.Tool.Core;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;

/// <summary>
/// Checks a JSON or YAML rule against the JSON schema of the rule tree, <c>rule-tree.schema.json</c>. The schema is the
/// one that the <c>TruthWeaver</c> package ships; the build embeds the same file in this assembly.
/// </summary>
/// <remarks>
/// The check reads the keywords that the schema uses: <c>$ref</c> to <c>#/$defs</c>, <c>oneOf</c>, <c>type</c>,
/// <c>properties</c>, <c>required</c>, <c>additionalProperties</c>, <c>items</c>, <c>minItems</c>, <c>maxItems</c> and
/// <c>pattern</c>. A keyword that it does not know is a programming error, and a test keeps the schema inside this set,
/// so the tool core needs no JSON schema library. When no form of a <c>oneOf</c> matches, the check reports the form
/// with the fewest findings, which is the form that the author most likely meant.
/// </remarks>
internal static class TreeSchemaValidator
{
    private const string ResourceName = "TruthWeaver.Tool.Core.rule-tree.schema.json";

    private static readonly Lazy<JsonObject> Schema = new(LoadSchema);

    private static readonly ConcurrentDictionary<string, Regex> Patterns = new(StringComparer.Ordinal);

    /// <summary>Gets the keywords that the check reads, for the test that keeps the schema inside this set.</summary>
    public static IReadOnlySet<string> SupportedKeywords { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "$ref",
            "oneOf",
            "type",
            "properties",
            "required",
            "additionalProperties",
            "items",
            "minItems",
            "maxItems",
            "pattern",
            "$schema",
            "$id",
            "$defs",
            "title",
            "description",
        };

    /// <summary>Gets the embedded schema text.</summary>
    /// <returns>The text of <c>rule-tree.schema.json</c>.</returns>
    public static string SchemaText()
    {
        using Stream stream =
            typeof(TreeSchemaValidator).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Checks a rule document against the schema.</summary>
    /// <param name="text">The rule text: a JSON or YAML document.</param>
    /// <param name="format">The format of <paramref name="text"/>: <see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</param>
    /// <returns>
    /// One <see cref="ToolDiagnosticCodes.TreeSchemaViolation"/> error for each finding, or one
    /// <see cref="ToolDiagnosticCodes.MalformedDocument"/> error when the text is not a well-formed document. Empty when the
    /// document matches the schema.
    /// </returns>
    public static IReadOnlyList<Diagnostic> Validate(string text, RuleFormat format)
    {
        TreeValue? root = format == RuleFormat.Json ? TreeValue.ReadJson(text) : TreeValue.ReadYaml(text);
        if (root is null)
        {
            return
            [
                ToolResults.Error(
                    ToolDiagnosticCodes.MalformedDocument,
                    $"The rule is not a well-formed {(format == RuleFormat.Json ? "JSON" : "YAML")} document, so the tree schema check cannot read it."
                ),
            ];
        }

        List<Finding> findings = Check(Schema.Value, root, "$");

        // A finding can come through more than one branch; report each one once, in document order.
        return
        [
            .. findings
                .Distinct()
                .OrderBy(f => f.Span.Start)
                .ThenBy(f => f.Path, StringComparer.Ordinal)
                .Select(f => Diagnostic.Error(ToolDiagnosticCodes.TreeSchemaViolation, f.Message, f.Span, path: f.Path)),
        ];
    }

    private static JsonObject LoadSchema()
    {
        return JsonNode.Parse(SchemaText())?.AsObject()
            ?? throw new InvalidOperationException("The embedded rule tree schema is empty.");
    }

    private static List<Finding> Check(JsonNode schema, TreeValue value, string path)
    {
        if (schema is JsonValue boolean)
        {
            return boolean.GetValue<bool>() ? [] : [new Finding(path, value.Span, "This value is not allowed here.")];
        }

        JsonObject keywords = schema.AsObject();
        List<Finding> findings = [];
        string? unsupported = keywords.Select(k => k.Key).FirstOrDefault(k => !SupportedKeywords.Contains(k));
        if (unsupported is not null)
        {
            throw new InvalidOperationException(
                $"The rule tree schema uses the keyword '{unsupported}', which the tree schema check does not read."
            );
        }

        if (keywords["type"] is JsonValue type && !HasType(value, type.GetValue<string>()))
        {
            // A value of the wrong kind has no members or items to check further.
            return
            [
                new Finding(
                    path,
                    value.Span,
                    $"The value must be {Article(type.GetValue<string>())}. It is {Article(KindName(value))}."
                ),
            ];
        }

        if (keywords["$ref"] is JsonValue reference)
        {
            findings.AddRange(Check(Resolve(reference.GetValue<string>()), value, path));
        }

        if (keywords["oneOf"] is JsonArray forms)
        {
            findings.AddRange(CheckOneOf(forms, value, path));
        }

        if (keywords["pattern"] is JsonValue pattern && value.Kind == TreeValueKind.String)
        {
            Regex regex = Patterns.GetOrAdd(
                pattern.GetValue<string>(),
                p => new Regex(p, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
            );
            if (!regex.IsMatch(value.Text ?? string.Empty))
            {
                findings.Add(new Finding(path, value.Span, $"The value '{value.Text}' is not allowed here."));
            }
        }

        if (value.Kind == TreeValueKind.Object)
        {
            findings.AddRange(CheckObject(keywords, value, path));
        }

        if (value.Kind == TreeValueKind.Array)
        {
            findings.AddRange(CheckArray(keywords, value, path));
        }

        return findings;
    }

    private static List<Finding> CheckOneOf(JsonArray forms, TreeValue value, string path)
    {
        List<List<Finding>> failures = [];
        int matches = 0;
        foreach (JsonNode? form in forms)
        {
            List<Finding> findings = Check(form!, value, path);
            if (findings.Count == 0)
            {
                matches++;
            }
            else
            {
                failures.Add(findings);
            }
        }

        if (matches == 1)
        {
            return [];
        }

        if (matches > 1)
        {
            return [new Finding(path, value.Span, "The value matches more than one kind of node.")];
        }

        // The form with the fewest findings is the closest match; on a tie, the form whose findings go deepest.
        return failures.OrderBy(f => f.Count).ThenByDescending(f => f.Max(x => x.Path.Length)).First();
    }

    private static IEnumerable<Finding> CheckObject(JsonObject keywords, TreeValue value, string path)
    {
        JsonObject? properties = keywords["properties"] as JsonObject;
        if (keywords["required"] is JsonArray required)
        {
            IEnumerable<string> missing = required
                .Select(r => r!.GetValue<string>())
                .Where(name => !value.Members.Any(m => m.Key == name));
            foreach (string name in missing)
            {
                yield return new Finding(path, value.Span, $"This node must have the member '{name}'.");
            }
        }

        foreach (KeyValuePair<string, TreeValue> member in value.Members)
        {
            string memberPath = path + PathSegment(member.Key);
            if (properties is not null && properties.TryGetPropertyValue(member.Key, out JsonNode? property))
            {
                foreach (Finding finding in Check(property!, member.Value, memberPath))
                {
                    yield return finding;
                }
            }
            else if (keywords["additionalProperties"] is { } additional)
            {
                if (additional is JsonValue allowed && !allowed.GetValue<bool>())
                {
                    yield return new Finding(
                        memberPath,
                        member.Value.Span,
                        $"The member '{member.Key}' is not allowed in this node."
                    );
                }
                else
                {
                    foreach (Finding finding in Check(additional, member.Value, memberPath))
                    {
                        yield return finding;
                    }
                }
            }
        }
    }

    private static IEnumerable<Finding> CheckArray(JsonObject keywords, TreeValue value, string path)
    {
        if (keywords["minItems"] is JsonValue min && value.Items.Count < min.GetValue<int>())
        {
            yield return new Finding(
                path,
                value.Span,
                $"The array must have at least {min.GetValue<int>()} items. It has {value.Items.Count}."
            );
        }

        if (keywords["maxItems"] is JsonValue max && value.Items.Count > max.GetValue<int>())
        {
            yield return new Finding(
                path,
                value.Span,
                $"The array must have at most {max.GetValue<int>()} items. It has {value.Items.Count}."
            );
        }

        if (keywords["items"] is { } items)
        {
            for (int i = 0; i < value.Items.Count; i++)
            {
                foreach (Finding finding in Check(items, value.Items[i], $"{path}[{i}]"))
                {
                    yield return finding;
                }
            }
        }
    }

    private static JsonNode Resolve(string reference)
    {
        const string Prefix = "#/$defs/";
        if (!reference.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The rule tree schema reference '{reference}' is not to '{Prefix}'.");
        }

        return Schema.Value["$defs"]?[reference[Prefix.Length..]]
            ?? throw new InvalidOperationException($"The rule tree schema reference '{reference}' names nothing.");
    }

    private static bool HasType(TreeValue value, string type)
    {
        return type switch
        {
            "object" => value.Kind == TreeValueKind.Object,
            "array" => value.Kind == TreeValueKind.Array,
            "string" => value.Kind == TreeValueKind.String,
            "boolean" => value.Kind == TreeValueKind.Boolean,
            "number" => value.Kind == TreeValueKind.Number,
            "integer" => value.Kind == TreeValueKind.Number && value.IsInteger,
            "null" => value.Kind == TreeValueKind.Null,
            _ => throw new InvalidOperationException($"The rule tree schema uses the type '{type}', which is not known."),
        };
    }

    private static string KindName(TreeValue value)
    {
        return value.Kind switch
        {
            TreeValueKind.Number => value.IsInteger ? "integer" : "number",
            _ => value.Kind.ToString().ToLowerInvariant(),
        };
    }

    private static string Article(string kind)
    {
        return kind switch
        {
            "object" or "array" or "integer" => "an " + kind,
            "null" => "null",
            _ => "a " + kind,
        };
    }

    // The same path form as the compiler's JSON and YAML diagnostics: .name for an identifier, ['key'] for any other key.
    private static string PathSegment(string key)
    {
        bool identifier =
            key.Length > 0
            && (char.IsAsciiLetter(key[0]) || key[0] == '_')
            && key.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
        return identifier ? "." + key : "['" + key.Replace("\\", "\\\\").Replace("'", "\\'") + "']";
    }

    private sealed record Finding(string Path, SourceSpan Span, string Message);
}
