namespace TruthWeaver.Json;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TruthWeaver.Evaluation;

/// <summary>
/// Reads the JSON that <see cref="DecisionJsonWriter"/> writes back into a <see cref="DecisionRecord"/>. It walks a
/// <see cref="JsonDocument"/> member by member (no reflection serializer, so it stays trim and AOT safe) and never throws
/// for bad input: the first problem stops the read and is reported with its JSON path. The shape is strict: an unknown
/// or duplicate member is an error, so a record that a later format version wrote is not misread.
/// </summary>
internal sealed class DecisionJsonReader
{
    private static readonly string VariableExceptionType = typeof(VariableResolutionException).FullName!;

    private string? error;

    private DecisionJsonReader() { }

    /// <summary>Reads a decision record.</summary>
    /// <param name="json">The JSON text.</param>
    /// <param name="record">The record, when the text is valid.</param>
    /// <param name="error">What is wrong and where, when the text is not valid.</param>
    /// <returns><see langword="true"/> when the text is a valid decision record.</returns>
    public static bool TryRead(
        string json,
        [NotNullWhen(true)] out DecisionRecord? record,
        [NotNullWhen(false)] out string? error
    )
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions { MaxDepth = DecisionJsonWriter.MaxDepth, AllowDuplicateProperties = false }
            );
        }
        catch (JsonException ex)
        {
            record = null;
            error = $"$: the text is not valid JSON: {ex.Message}";
            return false;
        }

        using (document)
        {
            DecisionJsonReader reader = new();
            record = reader.ReadRecord(document.RootElement);
            error = record is null ? reader.error ?? "$: the record is not valid." : null;
            return record is not null;
        }
    }

    private static bool TryParseName<TEnum>(string text, out TEnum value)
        where TEnum : struct, Enum
    {
        // Only the exact member names are accepted; Enum.TryParse would also take digits and comma lists.
        TEnum[] matches =
        [
            .. Enum.GetValues<TEnum>().Where(candidate => string.Equals(candidate.ToString(), text, StringComparison.Ordinal)),
        ];
        value = matches.Length == 1 ? matches[0] : default;
        return matches.Length == 1;
    }

    private static IEnumerable<JsonElement> ElementsOf(JsonElement array)
    {
#pragma warning disable IDISP004 // JsonElement's enumerator is a disposable struct, but foreach already disposes it.
        foreach (JsonElement item in array.EnumerateArray())
        {
            yield return item;
        }
#pragma warning restore IDISP004
    }

    private static IEnumerable<string> NamesOf(JsonElement obj)
    {
#pragma warning disable IDISP004 // JsonElement's enumerator is a disposable struct, but foreach already disposes it.
        foreach (JsonProperty property in obj.EnumerateObject())
        {
            yield return property.Name;
        }
#pragma warning restore IDISP004
    }

    private static bool IsLowerHex(string text, int length)
    {
        return text.Length == length && text.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
    }

    private DecisionRecord? ReadRecord(JsonElement root)
    {
        if (
            !this.CheckObject(
                root,
                "$",
                [
                    DecisionJsonNames.FormatVersion,
                    DecisionJsonNames.Rule,
                    DecisionJsonNames.Options,
                    DecisionJsonNames.ArgumentValues,
                    DecisionJsonNames.Result,
                    DecisionJsonNames.Faults,
                    DecisionJsonNames.Trace,
                    DecisionJsonNames.TraceTree,
                ]
            )
        )
        {
            return null;
        }

        if (!this.TryGetRequired(root, "$", DecisionJsonNames.FormatVersion, out JsonElement version))
        {
            return null;
        }

        if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int formatVersion))
        {
            return this.Fail<DecisionRecord>("$.formatVersion", "expected an integer.");
        }

        if (formatVersion != DecisionJsonWriter.FormatVersion)
        {
            return this.Fail<DecisionRecord>(
                "$.formatVersion",
                $"format version {formatVersion} is not supported; the supported version is {DecisionJsonWriter.FormatVersion}."
            );
        }

        RuleIdentity? rule = null;
        if (root.TryGetProperty(DecisionJsonNames.Rule, out JsonElement ruleElement))
        {
            rule = this.ReadRule(ruleElement);
            if (rule is null)
            {
                return null;
            }
        }

        EvaluationOptions? options = null;
        if (root.TryGetProperty(DecisionJsonNames.Options, out JsonElement optionsElement))
        {
            options = this.ReadOptions(optionsElement);
            if (options is null)
            {
                return null;
            }
        }

        if (!this.TryGetRequiredString(root, "$", DecisionJsonNames.ArgumentValues, out string? argumentValues))
        {
            return null;
        }

        if (argumentValues is not (DecisionJsonNames.Included or DecisionJsonNames.Omitted))
        {
            return this.Fail<DecisionRecord>("$.argumentValues", "expected \"included\" or \"omitted\".");
        }

        if (
            !this.TryGetRequiredString(root, "$", DecisionJsonNames.Result, out string? resultText)
            || !this.TryTruthValue(resultText, "$.result", out TruthValue result)
        )
        {
            return null;
        }

        if (!this.TryGetRequired(root, "$", DecisionJsonNames.Faults, out JsonElement faultsElement))
        {
            return null;
        }

        List<Fault>? faults = this.ReadFaults(faultsElement, argumentValues == DecisionJsonNames.Included);
        if (faults is null)
        {
            return null;
        }

        Trace? trace = null;
        if (root.TryGetProperty(DecisionJsonNames.Trace, out JsonElement traceElement))
        {
            trace = this.ReadTrace(traceElement);
            if (trace is null)
            {
                return null;
            }
        }

        TraceNode? tree = null;
        if (root.TryGetProperty(DecisionJsonNames.TraceTree, out JsonElement treeElement))
        {
            tree = this.ReadTraceNode(treeElement, "$.traceTree");
            if (tree is null)
            {
                return null;
            }
        }

        return new DecisionRecord(new Decision(result, new EquatableArray<Fault>(faults), trace, tree), rule, options);
    }

    private RuleIdentity? ReadRule(JsonElement element)
    {
        const string Path = "$.rule";
        if (
            !this.CheckObject(
                element,
                Path,
                [DecisionJsonNames.CanonicalTextSha256, DecisionJsonNames.Name, DecisionJsonNames.Version]
            )
            || !this.TryGetRequiredString(element, Path, DecisionJsonNames.CanonicalTextSha256, out string? hash)
            || !this.TryGetOptionalString(element, Path, DecisionJsonNames.Name, out string? name)
            || !this.TryGetOptionalString(element, Path, DecisionJsonNames.Version, out string? ruleVersion)
        )
        {
            return null;
        }

        return IsLowerHex(hash, 64)
            ? new RuleIdentity(hash, name, ruleVersion)
            : this.Fail<RuleIdentity>($"{Path}.canonicalTextSha256", "expected 64 lower-case hexadecimal digits.");
    }

    private EvaluationOptions? ReadOptions(JsonElement element)
    {
        const string Path = "$.options";
        if (
            !this.CheckObject(
                element,
                Path,
                [
                    DecisionJsonNames.FaultBudget,
                    DecisionJsonNames.Mode,
                    DecisionJsonNames.Timeout,
                    DecisionJsonNames.IncludeResolvedValues,
                    DecisionJsonNames.IncludeTrace,
                ]
            )
        )
        {
            return null;
        }

        if (!this.TryGetRequired(element, Path, DecisionJsonNames.FaultBudget, out JsonElement budgetElement))
        {
            return null;
        }

        int? faultBudget = null;
        if (budgetElement.ValueKind != JsonValueKind.Null)
        {
            if (budgetElement.ValueKind != JsonValueKind.Number || !budgetElement.TryGetInt32(out int budget) || budget < 1)
            {
                return this.Fail<EvaluationOptions>($"{Path}.faultBudget", "expected an integer of at least 1, or null.");
            }

            faultBudget = budget;
        }

        if (!this.TryGetRequiredString(element, Path, DecisionJsonNames.Mode, out string? modeText))
        {
            return null;
        }

        if (!TryParseName(modeText, out EvaluationMode mode))
        {
            return this.Fail<EvaluationOptions>($"{Path}.mode", "expected \"ShortCircuit\" or \"Exhaustive\".");
        }

        if (!this.TryGetRequired(element, Path, DecisionJsonNames.Timeout, out JsonElement timeoutElement))
        {
            return null;
        }

        TimeSpan? timeout = null;
        if (timeoutElement.ValueKind != JsonValueKind.Null)
        {
            if (
                timeoutElement.ValueKind != JsonValueKind.String
                || !TimeSpan.TryParseExact(timeoutElement.GetString(), "c", CultureInfo.InvariantCulture, out TimeSpan parsed)
            )
            {
                return this.Fail<EvaluationOptions>(
                    $"{Path}.timeout",
                    "expected a time span in the constant format (\"00:00:05\"), or null."
                );
            }

            timeout = parsed;
        }

        if (!this.TryGetRequired(element, Path, DecisionJsonNames.IncludeResolvedValues, out JsonElement includeElement))
        {
            return null;
        }

        if (includeElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return this.Fail<EvaluationOptions>($"{Path}.includeResolvedValues", "expected true or false.");
        }

        // Optional, so a record written before the member existed still reads: it did not request a trace option.
        bool includeTrace = false;
        if (element.TryGetProperty(DecisionJsonNames.IncludeTrace, out JsonElement traceElement))
        {
            if (traceElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return this.Fail<EvaluationOptions>($"{Path}.includeTrace", "expected true or false.");
            }

            includeTrace = traceElement.GetBoolean();
        }

        return new EvaluationOptions(faultBudget, mode, timeout, includeElement.GetBoolean(), IncludeTrace: includeTrace);
    }

    private List<Fault>? ReadFaults(JsonElement element, bool valuesIncluded)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return this.Fail<List<Fault>>("$.faults", "expected an array.");
        }

        List<Fault> faults = [];
        int index = 0;
        foreach (JsonElement faultElement in ElementsOf(element))
        {
            string path = $"$.faults[{index}]";
            if (
                !this.CheckObject(faultElement, path, [DecisionJsonNames.Term, DecisionJsonNames.Exception])
                || !this.TryGetRequired(faultElement, path, DecisionJsonNames.Term, out JsonElement termElement)
                || !this.TryGetRequired(faultElement, path, DecisionJsonNames.Exception, out JsonElement exceptionElement)
            )
            {
                return null;
            }

            TermIdentity? term = this.ReadTerm(termElement, $"{path}.term", valuesIncluded);
            Exception? exception = term is null ? null : this.ReadException(exceptionElement, $"{path}.exception");
            if (term is null || exception is null)
            {
                return null;
            }

            faults.Add(new Fault(term, exception));
            index++;
        }

        return faults;
    }

    private TermIdentity? ReadTerm(JsonElement element, string path, bool valuesIncluded)
    {
        if (
            !this.CheckObject(
                element,
                path,
                [DecisionJsonNames.Predicate, DecisionJsonNames.Arguments, DecisionJsonNames.Variables]
            )
            || !this.TryGetRequiredString(element, path, DecisionJsonNames.Predicate, out string? predicate)
            || !this.TryGetRequired(element, path, DecisionJsonNames.Arguments, out JsonElement argumentsElement)
            || !this.TryGetRequired(element, path, DecisionJsonNames.Variables, out JsonElement variablesElement)
        )
        {
            return null;
        }

        if (argumentsElement.ValueKind != JsonValueKind.Array)
        {
            return this.Fail<TermIdentity>($"{path}.arguments", "expected an array.");
        }

        List<KeyValuePair<string, LiteralValue>> arguments = [];
        int index = 0;
        foreach (JsonElement argument in ElementsOf(argumentsElement))
        {
            string argumentPath = $"{path}.arguments[{index}]";
            if (
                !this.CheckObject(
                    argument,
                    argumentPath,
                    [DecisionJsonNames.Name, DecisionJsonNames.Kind, DecisionJsonNames.Value]
                )
                || !this.TryGetRequiredString(argument, argumentPath, DecisionJsonNames.Name, out string? name)
                || !this.TryGetRequiredString(argument, argumentPath, DecisionJsonNames.Kind, out string? kindText)
            )
            {
                return null;
            }

            if (!TryParseName(kindText, out LiteralKind kind))
            {
                return this.Fail<TermIdentity>($"{argumentPath}.kind", $"'{kindText}' is not a literal kind.");
            }

            // The record says whether values were written; a value that contradicts it means the record was edited.
            bool hasValue = argument.TryGetProperty(DecisionJsonNames.Value, out JsonElement valueElement);
            if (hasValue != valuesIncluded)
            {
                string mismatch = valuesIncluded
                    ? "the value is missing, but argumentValues is \"included\"."
                    : "a value is present, but argumentValues is \"omitted\".";
                return this.Fail<TermIdentity>(argumentPath, mismatch);
            }

            if (hasValue)
            {
                if (!this.TryReadLiteral(valueElement, kind, $"{argumentPath}.value", out LiteralValue value))
                {
                    return null;
                }

                arguments.Add(new KeyValuePair<string, LiteralValue>(name, value));
            }

            index++;
        }

        if (variablesElement.ValueKind != JsonValueKind.Array)
        {
            return this.Fail<TermIdentity>($"{path}.variables", "expected an array.");
        }

        List<KeyValuePair<string, VariableReference>> variables = [];
        index = 0;
        foreach (JsonElement variable in ElementsOf(variablesElement))
        {
            string variablePath = $"{path}.variables[{index}]";
            if (
                !this.CheckObject(
                    variable,
                    variablePath,
                    [DecisionJsonNames.Name, DecisionJsonNames.Source, DecisionJsonNames.Query]
                )
                || !this.TryGetRequiredString(variable, variablePath, DecisionJsonNames.Name, out string? name)
                || !this.TryGetRequiredString(variable, variablePath, DecisionJsonNames.Source, out string? source)
                || !this.TryGetRequiredString(variable, variablePath, DecisionJsonNames.Query, out string? query)
            )
            {
                return null;
            }

            variables.Add(new KeyValuePair<string, VariableReference>(name, new VariableReference(source, query)));
            index++;
        }

        return new TermIdentity(predicate, arguments, variables);
    }

    private Exception? ReadException(JsonElement element, string path)
    {
        if (
            !this.CheckObject(element, path, [DecisionJsonNames.Type, DecisionJsonNames.Message, DecisionJsonNames.Variable])
            || !this.TryGetRequiredString(element, path, DecisionJsonNames.Type, out string? type)
            || !this.TryGetRequiredString(element, path, DecisionJsonNames.Message, out string? message)
        )
        {
            return null;
        }

        bool isVariableFailure = string.Equals(type, VariableExceptionType, StringComparison.Ordinal);
        bool hasVariable = element.TryGetProperty(DecisionJsonNames.Variable, out JsonElement variable);
        if (isVariableFailure != hasVariable)
        {
            string mismatch = hasVariable
                ? $"'variable' is allowed only when the type is {VariableExceptionType}."
                : $"a {VariableExceptionType} needs 'variable'.";
            return this.Fail<Exception>(path, mismatch);
        }

        if (!hasVariable)
        {
            return new RecordedFaultException(message) { ExceptionType = type };
        }

        // A variable failure is rebuilt as the real exception type, so its reference and kind stay inspectable.
        string variablePath = $"{path}.variable";
        if (
            !this.CheckObject(
                variable,
                variablePath,
                [DecisionJsonNames.Source, DecisionJsonNames.Query, DecisionJsonNames.Failure]
            )
            || !this.TryGetRequiredString(variable, variablePath, DecisionJsonNames.Source, out string? source)
            || !this.TryGetRequiredString(variable, variablePath, DecisionJsonNames.Query, out string? query)
            || !this.TryGetRequiredString(variable, variablePath, DecisionJsonNames.Failure, out string? failureText)
        )
        {
            return null;
        }

        return TryParseName(failureText, out VariableFailureKind failure)
            ? new VariableResolutionException(new VariableReference(source, query), failure, message)
            : this.Fail<Exception>($"{variablePath}.failure", $"'{failureText}' is not a variable failure kind.");
    }

    private bool TryReadLiteral(JsonElement element, LiteralKind kind, string path, out LiteralValue value)
    {
        value = default;
        switch (kind)
        {
            case LiteralKind.String when element.ValueKind == JsonValueKind.String:
                value = LiteralValue.OfString(element.GetString()!);
                return true;
            case LiteralKind.Int64 when element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out long int64):
                value = LiteralValue.OfInt64(int64);
                return true;
            case LiteralKind.Decimal
                when element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out decimal number):
                value = LiteralValue.OfDecimal(number);
                return true;
            case LiteralKind.Boolean when element.ValueKind is JsonValueKind.True or JsonValueKind.False:
                value = LiteralValue.OfBoolean(element.GetBoolean());
                return true;
            case LiteralKind.DateTimeOffset
                when element.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParseExact(
                        element.GetString(),
                        "O",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTimeOffset instant
                    ):
                value = LiteralValue.OfDateTimeOffset(instant);
                return true;
            case LiteralKind.Guid
                when element.ValueKind == JsonValueKind.String && Guid.TryParseExact(element.GetString(), "D", out Guid guid):
                value = LiteralValue.OfGuid(guid);
                return true;
            case LiteralKind.String
            or LiteralKind.Int64
            or LiteralKind.Decimal
            or LiteralKind.Boolean
            or LiteralKind.DateTimeOffset
            or LiteralKind.Guid:
                return this.Fail(path, $"expected a {kind} value.");
            default:
                return this.TryReadArray(element, kind, path, out value);
        }
    }

    private bool TryReadArray(JsonElement element, LiteralKind kind, string path, out LiteralValue value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Array)
        {
            return this.Fail(path, $"expected a {kind} value (an array).");
        }

        LiteralKind elementKind = LiteralValue.ToElementKind(kind);
        List<LiteralValue> items = [];
        int index = 0;
        foreach (JsonElement item in ElementsOf(element))
        {
            if (!this.TryReadLiteral(item, elementKind, $"{path}[{index}]", out LiteralValue itemValue))
            {
                return false;
            }

            items.Add(itemValue);
            index++;
        }

        value = LiteralValue.OfArray(elementKind, items);
        return true;
    }

    private Trace? ReadTrace(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return this.Fail<Trace>("$.trace", "expected an array.");
        }

        List<TraceEntry> entries = [];
        int index = 0;
        foreach (JsonElement entry in ElementsOf(element))
        {
            string path = $"$.trace[{index}]";
            if (
                !this.CheckObject(
                    entry,
                    path,
                    [DecisionJsonNames.Text, DecisionJsonNames.Result, DecisionJsonNames.NotEvaluated]
                ) || !this.TryReadOutcome(entry, path, out string? text, out TruthValue? result, out bool notEvaluated)
            )
            {
                return null;
            }

            entries.Add(new TraceEntry(text, result, notEvaluated));
            index++;
        }

        return new Trace(new EquatableArray<TraceEntry>(entries));
    }

    private TraceNode? ReadTraceNode(JsonElement element, string path)
    {
        if (
            !this.CheckObject(
                element,
                path,
                [DecisionJsonNames.Text, DecisionJsonNames.Result, DecisionJsonNames.NotEvaluated, DecisionJsonNames.Children]
            )
            || !this.TryReadOutcome(element, path, out string? text, out TruthValue? result, out bool notEvaluated)
            || !this.TryGetRequired(element, path, DecisionJsonNames.Children, out JsonElement childrenElement)
        )
        {
            return null;
        }

        if (childrenElement.ValueKind != JsonValueKind.Array)
        {
            return this.Fail<TraceNode>($"{path}.children", "expected an array.");
        }

        if (notEvaluated && childrenElement.GetArrayLength() > 0)
        {
            return this.Fail<TraceNode>($"{path}.children", "a node that was not evaluated has no children.");
        }

        List<TraceNode> children = [];
        int index = 0;
        foreach (JsonElement childElement in ElementsOf(childrenElement))
        {
            TraceNode? child = this.ReadTraceNode(childElement, $"{path}.children[{index}]");
            if (child is null)
            {
                return null;
            }

            children.Add(child);
            index++;
        }

        return new TraceNode(text, result, notEvaluated, new EquatableArray<TraceNode>(children));
    }

    private bool TryReadOutcome(
        JsonElement element,
        string path,
        [NotNullWhen(true)] out string? text,
        out TruthValue? result,
        out bool notEvaluated
    )
    {
        result = null;
        notEvaluated = false;
        if (
            !this.TryGetRequiredString(element, path, DecisionJsonNames.Text, out text)
            || !this.TryGetRequired(element, path, DecisionJsonNames.Result, out JsonElement resultElement)
            || !this.TryGetRequired(element, path, DecisionJsonNames.NotEvaluated, out JsonElement notEvaluatedElement)
        )
        {
            return false;
        }

        if (notEvaluatedElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return this.Fail($"{path}.notEvaluated", "expected true or false.");
        }

        notEvaluated = notEvaluatedElement.GetBoolean();

        // A skipped node has no result and an evaluated node always has one (TraceEntry and TraceNode say the same).
        if (resultElement.ValueKind == JsonValueKind.Null)
        {
            return notEvaluated || this.Fail($"{path}.result", "an evaluated node needs a result.");
        }

        if (notEvaluated)
        {
            return this.Fail($"{path}.result", "a node that was not evaluated has a null result.");
        }

        if (resultElement.ValueKind != JsonValueKind.String)
        {
            return this.Fail($"{path}.result", "expected \"True\", \"False\", \"Unknown\" or null.");
        }

        if (!this.TryTruthValue(resultElement.GetString()!, $"{path}.result", out TruthValue value))
        {
            return false;
        }

        result = value;
        return true;
    }

    private bool TryTruthValue(string text, string path, out TruthValue value)
    {
        return TryParseName(text, out value) || this.Fail(path, "expected \"True\", \"False\" or \"Unknown\".");
    }

    private bool CheckObject(JsonElement element, string path, string[] allowed)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return this.Fail(path, "expected an object.");
        }

        string? unknown = NamesOf(element).FirstOrDefault(name => !allowed.Contains(name, StringComparer.Ordinal));
        return unknown is null || this.Fail($"{path}.{unknown}", "unknown member.");
    }

    private bool TryGetRequired(JsonElement element, string path, string name, out JsonElement value)
    {
        return element.TryGetProperty(name, out value) || this.Fail($"{path}.{name}", "the member is required.");
    }

    private bool TryGetRequiredString(JsonElement element, string path, string name, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (!this.TryGetRequired(element, path, name, out JsonElement member))
        {
            return false;
        }

        if (member.ValueKind != JsonValueKind.String)
        {
            return this.Fail($"{path}.{name}", "expected a string.");
        }

        value = member.GetString()!;
        return true;
    }

    private bool TryGetOptionalString(JsonElement element, string path, string name, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(name, out JsonElement member))
        {
            return true;
        }

        if (member.ValueKind != JsonValueKind.String)
        {
            return this.Fail($"{path}.{name}", "expected a string.");
        }

        value = member.GetString();
        return true;
    }

    private bool Fail(string path, string message)
    {
        this.error = $"{path}: {message}";
        return false;
    }

    private T? Fail<T>(string path, string message)
        where T : class
    {
        this.error = $"{path}: {message}";
        return null;
    }
}
