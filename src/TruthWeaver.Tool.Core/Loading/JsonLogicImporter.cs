namespace TruthWeaver.Tool.Core.Loading;

using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;

/// <summary>
/// Imports a <see href="https://jsonlogic.com">JsonLogic</see> rule as a TruthWeaver rule tree (the JSON format of
/// <c>rule-tree.schema.json</c>). The importer needs only <see cref="System.Text.Json"/>.
/// </summary>
/// <remarks>
/// <para>
/// The importer maps <c>and</c>, <c>or</c>, <c>!</c>, <c>!!</c>, <c>if</c> and the comparisons to Strong Kleene (K3)
/// operators and to catalog predicates. A comparison between a <c>var</c> and a literal becomes a term whose name is the
/// camel-case path of the variable followed by the catalog member, for example <c>{"&gt;": [{"var": "user.age"}, 18]}</c>
/// becomes <c>userAgeIsGreaterThan(value: 18)</c>. The rule's host registers a predicate with that name that selects the
/// value.
/// </para>
/// <para>
/// JsonLogic is two-valued and TruthWeaver is not. Where the result can differ, the import gives a
/// <see cref="ToolDiagnosticCodes.JsonLogicBehaviorDiffers"/> warning: a <c>var</c> read as a truth value (JsonLogic
/// coerces it), a missing <c>var</c> (null in JsonLogic, so <c>Unknown</c> here), a loose comparison (<c>==</c>, <c>!=</c>)
/// and a missing <c>else</c> branch. An operator or a shape that has no mapping fails the import with a
/// <see cref="ToolDiagnosticCodes.UnsupportedJsonLogic"/> error for each occurrence.
/// </para>
/// </remarks>
public static class JsonLogicImporter
{
    /// <summary>Imports a JsonLogic rule.</summary>
    /// <param name="jsonLogic">The JsonLogic JSON text.</param>
    /// <returns>
    /// The rule tree as compact JSON, or no value when the document is malformed or has an unsupported operator or shape.
    /// The diagnostics have a <see cref="Diagnostic.Path"/> that names the JsonLogic node, such as <c>$.and[1]</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="jsonLogic"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> Import(string jsonLogic)
    {
        ArgumentNullException.ThrowIfNull(jsonLogic);

        JsonNode? document;
        try
        {
            document = JsonNode.Parse(jsonLogic);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return ToolResults.Failure<string>(
                ToolDiagnosticCodes.MalformedDocument,
                $"The JsonLogic document is not well-formed JSON: {exception.Message}"
            );
        }

        Conversion conversion = new();
        JsonNode? tree = conversion.Condition(document, "$");
        return conversion.HasErrors || tree is null
            ? ToolResults.Failure<string>(conversion.Diagnostics)
            : ToolResults.Success(tree.ToJsonString(), conversion.Diagnostics);
    }

    // One import: the diagnostics so far and the variables already warned about.
    private sealed class Conversion
    {
        private static readonly Dictionary<string, string> ComparisonMembers = new(StringComparer.Ordinal)
        {
            ["=="] = "IsEqualTo",
            ["==="] = "IsEqualTo",
            ["!="] = "IsNotEqualTo",
            ["!=="] = "IsNotEqualTo",
            ["<"] = "IsLessThan",
            ["<="] = "IsLessThanOrEqualTo",
            [">"] = "IsGreaterThan",
            [">="] = "IsGreaterThanOrEqualTo",
        };

        // The member to use when the literal comes first: 18 < x is x > 18.
        private static readonly Dictionary<string, string> FlippedMembers = new(StringComparer.Ordinal)
        {
            ["IsLessThan"] = "IsGreaterThan",
            ["IsLessThanOrEqualTo"] = "IsGreaterThanOrEqualTo",
            ["IsGreaterThan"] = "IsLessThan",
            ["IsGreaterThanOrEqualTo"] = "IsLessThanOrEqualTo",
        };

        private readonly List<Diagnostic> diagnostics = [];
        private readonly HashSet<string> missingWarned = [with(StringComparer.Ordinal)];

        public bool HasErrors { get; private set; }

        public IReadOnlyList<Diagnostic> Diagnostics => this.diagnostics;

        // Converts a node that stands where a truth value is expected: a rule root, an operand of and/or/!/if.
        public JsonNode? Condition(JsonNode? node, string path)
        {
            switch (node)
            {
                case null:
                    this.Differs(path, "A null condition is falsy in JsonLogic. Here it is the constant Unknown.");
                    return Constant("unknown");
                case JsonValue value when value.TryGetValue(out bool flag):
                    return Constant(flag ? "true" : "false");
                case JsonObject { Count: 1 } operation:
                    KeyValuePair<string, JsonNode?> entry = ((IList<KeyValuePair<string, JsonNode?>>)operation)[0];
                    return this.Operation(entry.Key, entry.Value, $"{path}.{entry.Key}");
                case JsonObject:
                    return this.Unsupported(path, "A JsonLogic operation is an object with exactly one key, the operator.");
                default:
                    return this.Unsupported(
                        path,
                        "A literal that is not true, false or null cannot stand where a truth value is expected."
                    );
            }
        }

        private static JsonObject Constant(string value)
        {
            return value switch
            {
                "true" => new JsonObject { ["const"] = true },
                "false" => new JsonObject { ["const"] = false },
                _ => new JsonObject { ["const"] = "unknown" },
            };
        }

        private static JsonObject Term(string predicate, JsonObject? args = null)
        {
            JsonObject term = new() { ["predicate"] = predicate };
            if (args is not null)
            {
                term["args"] = args;
            }

            return term;
        }

        private static JsonObject Operator(string op, params JsonNode?[] operands)
        {
            return new JsonObject { ["op"] = op, ["operands"] = new JsonArray(operands) };
        }

        // The arguments of an operation as a list: JsonLogic allows a single argument without the array.
        private static List<JsonNode?> Arguments(JsonNode? value)
        {
            return value is JsonArray array ? [.. array] : [value];
        }

        private static bool IsVariable(JsonNode? node)
        {
            return node is JsonObject { Count: 1 } only && only.ContainsKey("var");
        }

        private static bool IsLiteral(JsonNode? node)
        {
            return node switch
            {
                null => true,
                JsonValue => true,
                JsonArray array => array.All(IsLiteral) && array.All(item => item is not JsonArray),
                _ => false,
            };
        }

        // Pascal-case join of the variable path: "user.age" is "userAge".
        private static string? NameOf(string variablePath)
        {
            string[] segments = variablePath.Split('.');
            if (segments.Any(s => s.Length == 0))
            {
                return null;
            }

            StringBuilder name = new();
            for (int i = 0; i < segments.Length; i++)
            {
                for (int j = 0; j < segments[i].Length; j++)
                {
                    char c = segments[i][j];
                    char use = char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_';
                    name.Append(i > 0 && j == 0 ? char.ToUpperInvariant(use) : use);
                }
            }

            if (char.IsAsciiDigit(name[0]))
            {
                name.Insert(0, 'v');
            }

            name[0] = char.ToLowerInvariant(name[0]);
            return name.ToString();
        }

        private JsonNode? Operation(string op, JsonNode? argument, string path)
        {
            List<JsonNode?> arguments = Arguments(argument);
            return op switch
            {
                "var" => this.TruthyVariable(argument, path),
                "!" => this.Negation(arguments, path),
                "!!" => arguments.Count == 1
                    ? this.Condition(arguments[0], $"{path}[0]")
                    : this.Unsupported(path, "'!!' takes one argument."),
                "and" or "or" => this.Junction(op, arguments, path),
                "if" or "?:" => this.Conditional(arguments, 0, path),
                "in" => this.Membership(arguments, path),
                _ => ComparisonMembers.TryGetValue(op, out string? member)
                    ? this.Comparison(op, member, arguments, path)
                    : this.Unsupported(path, $"The JsonLogic operator '{op}' has no TruthWeaver mapping."),
            };
        }

        private JsonNode? TruthyVariable(JsonNode? argument, string path)
        {
            if (this.VariableName(argument, path) is not { } name)
            {
                return null;
            }

            this.Differs(
                path,
                $"JsonLogic reads the variable '{name.Path}' by truthiness (null, 0, '' and [] are false). The term '{name.Predicate}' answers True, False or Unknown."
            );
            return Term(name.Predicate);
        }

        private JsonNode? Negation(List<JsonNode?> arguments, string path)
        {
            if (arguments.Count != 1)
            {
                return this.Unsupported(path, "'!' takes one argument.");
            }

            JsonNode? operand = this.Condition(arguments[0], $"{path}[0]");
            return operand is null ? null : Operator("not", operand);
        }

        private JsonNode? Junction(string op, List<JsonNode?> arguments, string path)
        {
            if (arguments.Count == 0)
            {
                return this.Unsupported(path, $"'{op}' needs at least one argument.");
            }

            List<JsonNode?> operands = [];
            for (int i = 0; i < arguments.Count; i++)
            {
                operands.Add(this.Condition(arguments[i], $"{path}[{i}]"));
            }

            if (operands.Any(o => o is null))
            {
                return null;
            }

            // JsonLogic returns the deciding operand itself; with one operand that is the operand.
            return operands.Count == 1 ? operands[0] : Operator(op, [.. operands]);
        }

        // [condition, then, condition, then, ..., else]: each pair after the first nests in the else position.
        private JsonNode? Conditional(List<JsonNode?> arguments, int start, string path)
        {
            int remaining = arguments.Count - start;
            if (remaining == 0)
            {
                this.Differs(path, "An 'if' with no arguments is null in JsonLogic. Here it is the constant Unknown.");
                return Constant("unknown");
            }

            if (remaining == 1)
            {
                return this.Condition(arguments[start], $"{path}[{start}]");
            }

            JsonNode? condition = this.Condition(arguments[start], $"{path}[{start}]");
            JsonNode? whenTrue = this.Condition(arguments[start + 1], $"{path}[{start + 1}]");
            JsonNode? whenFalse;
            if (remaining == 2)
            {
                this.Differs(
                    path,
                    "An 'if' with no else branch is null in JsonLogic when the condition is false. Here the else branch is the constant Unknown."
                );
                whenFalse = Constant("unknown");
            }
            else
            {
                whenFalse = this.Conditional(arguments, start + 2, path);
            }

            return condition is null || whenTrue is null || whenFalse is null
                ? null
                : Operator("if", condition, whenTrue, whenFalse);
        }

        private JsonNode? Comparison(string op, string member, List<JsonNode?> arguments, string path)
        {
            if (arguments.Count == 3 && op is "<" or "<=")
            {
                return this.Range(op, arguments, path);
            }

            if (arguments.Count != 2)
            {
                return this.Unsupported(path, $"'{op}' takes two arguments, or three for a range with '<' and '<='.");
            }

            bool variableFirst = IsVariable(arguments[0]);
            JsonNode? variable = variableFirst ? arguments[0] : arguments[1];
            JsonNode? literal = variableFirst ? arguments[1] : arguments[0];
            if (!IsVariable(variable) || !IsLiteral(literal) || (IsVariable(arguments[0]) && IsVariable(arguments[1])))
            {
                return this.Unsupported(path, $"A '{op}' comparison needs one variable and one literal value.");
            }

            if (this.VariableName(variable, $"{path}[{(variableFirst ? 0 : 1)}]") is not { } name)
            {
                return null;
            }

            this.MissingValue(name, path);
            if (op is "==" or "!=" && literal is not null)
            {
                this.Differs(
                    path,
                    $"'{op}' is a loose comparison in JsonLogic (it converts types, so 1 equals \"1\"). The term '{name.Predicate}{member}' compares without conversion. Use '{op}=' for the strict comparison."
                );
            }

            if (literal is null)
            {
                return member switch
                {
                    "IsEqualTo" => Term($"{name.Predicate}IsNull"),
                    "IsNotEqualTo" => Term($"{name.Predicate}IsNotNull"),
                    _ => this.Unsupported(path, "An ordering comparison with null has no mapping."),
                };
            }

            string use = variableFirst || !FlippedMembers.TryGetValue(member, out string? flipped) ? member : flipped;
            return Term($"{name.Predicate}{use}", new JsonObject { ["value"] = literal.DeepClone() });
        }

        // lower <= x <= upper is IsBetween; lower < x < upper is two strict comparisons.
        private JsonNode? Range(string op, List<JsonNode?> arguments, string path)
        {
            if (
                !IsLiteral(arguments[0])
                || !IsVariable(arguments[1])
                || !IsLiteral(arguments[2])
                || arguments[0] is null
                || arguments[2] is null
            )
            {
                return this.Unsupported(path, $"A '{op}' range needs a literal, a variable and a literal, in that order.");
            }

            if (this.VariableName(arguments[1], $"{path}[1]") is not { } name)
            {
                return null;
            }

            this.MissingValue(name, path);
            if (op == "<=")
            {
                return Term(
                    $"{name.Predicate}IsBetween",
                    new JsonObject { ["lower"] = arguments[0]!.DeepClone(), ["upper"] = arguments[2]!.DeepClone() }
                );
            }

            return Operator(
                "and",
                Term($"{name.Predicate}IsGreaterThan", new JsonObject { ["value"] = arguments[0]!.DeepClone() }),
                Term($"{name.Predicate}IsLessThan", new JsonObject { ["value"] = arguments[2]!.DeepClone() })
            );
        }

        private JsonNode? Membership(List<JsonNode?> arguments, string path)
        {
            if (
                arguments.Count == 2
                && IsVariable(arguments[0])
                && arguments[1] is JsonArray candidates
                && IsLiteral(candidates)
            )
            {
                if (this.VariableName(arguments[0], $"{path}[0]") is not { } name)
                {
                    return null;
                }

                this.MissingValue(name, path);
                return Term($"{name.Predicate}IsIn", new JsonObject { ["values"] = candidates.DeepClone() });
            }

            if (arguments.Count == 2 && IsVariable(arguments[1]) && arguments[0] is JsonValue { } element)
            {
                if (this.VariableName(arguments[1], $"{path}[1]") is not { } name)
                {
                    return null;
                }

                this.MissingValue(name, path);
                return Term($"{name.Predicate}Contains", new JsonObject { ["value"] = element.DeepClone() });
            }

            return this.Unsupported(
                path,
                "'in' is supported as a variable in an array of literals, or a literal in a variable collection."
            );
        }

        private (string Path, string Predicate)? VariableName(JsonNode? variable, string path)
        {
            // The node is { "var": <argument> } from a comparison, or the argument itself from a bare var.
            JsonNode? argument = variable is JsonObject { Count: 1 } only && only.ContainsKey("var") ? only["var"] : variable;
            List<JsonNode?> parts = Arguments(argument);
            if (parts.Count is 0 or > 2 || parts[0] is not JsonValue first || !first.TryGetValue(out string? variablePath))
            {
                this.Unsupported(path, "A 'var' needs a variable path as a string, with an optional default.");
                return null;
            }

            if (parts.Count == 2)
            {
                this.Differs(
                    path,
                    $"The default of the variable '{variablePath}' is not used. A missing value is Unknown here."
                );
            }

            if (NameOf(variablePath) is not { } predicate)
            {
                this.Unsupported(path, $"The variable path '{variablePath}' is empty or has an empty segment.");
                return null;
            }

            return (variablePath, predicate);
        }

        private void MissingValue((string Path, string Predicate) name, string path)
        {
            if (this.missingWarned.Add(name.Path))
            {
                this.Differs(
                    path,
                    $"A missing '{name.Path}' is null in JsonLogic, so a comparison is False. The term '{name.Predicate}...' answers Unknown for a missing value."
                );
            }
        }

        private JsonNode? Unsupported(string path, string message)
        {
            this.HasErrors = true;
            this.diagnostics.Add(ToolResults.Error(ToolDiagnosticCodes.UnsupportedJsonLogic, message, path));
            return null;
        }

        private void Differs(string path, string message)
        {
            this.diagnostics.Add(ToolResults.Warning(ToolDiagnosticCodes.JsonLogicBehaviorDiffers, message, path));
        }
    }
}
