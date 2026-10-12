namespace TruthWeaver.Json;

using System.Text.Encodings.Web;
using System.Text.Json;
using TruthWeaver.Evaluation;

/// <summary>
/// Writes a <see cref="DecisionRecord"/> as JSON with <see cref="Utf8JsonWriter"/>, member by member, so no reflection
/// serializer is involved and the code stays trim and AOT safe. <see cref="DecisionJsonReader"/> reads the same shape back;
/// <c>decision-record.schema.json</c> describes it. Without <see cref="DecisionJsonOptions.IncludeArgumentValues"/> no
/// literal argument value of the rule is written: a fault argument has only its name and kind, and each trace text goes
/// through <see cref="TraceTextRedaction"/>.
/// </summary>
internal static class DecisionJsonWriter
{
    /// <summary>The format version written to <c>formatVersion</c>; the reader accepts only this value.</summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// The nesting limit for writing and reading. Each trace tree level takes two JSON levels (the node and its
    /// <c>children</c> array), so a trace tree about 1000 levels deep still fits.
    /// </summary>
    public const int MaxDepth = 2048;

    /// <summary>Writes <paramref name="record"/> to <paramref name="writer"/> as indented JSON with line feeds and a final line feed.</summary>
    /// <param name="record">The record.</param>
    /// <param name="options">What to include.</param>
    /// <param name="writer">The writer.</param>
    public static void Write(DecisionRecord record, DecisionJsonOptions options, TextWriter writer)
    {
        using MemoryStream stream = new();
        using (
            Utf8JsonWriter json = new(
                stream,
                new JsonWriterOptions
                {
                    Indented = true,
                    NewLine = "\n",
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    MaxDepth = MaxDepth,
                }
            )
        )
        {
            json.WriteStartObject();
            json.WriteNumber(DecisionJsonNames.FormatVersion, FormatVersion);
            if (record.Rule is { } rule)
            {
                WriteRule(json, rule);
            }

            if (record.Options is { } evaluationOptions)
            {
                WriteOptions(json, evaluationOptions);
            }

            json.WriteString(
                DecisionJsonNames.ArgumentValues,
                options.IncludeArgumentValues ? DecisionJsonNames.Included : DecisionJsonNames.Omitted
            );
            json.WriteString(DecisionJsonNames.Result, record.Decision.Result.ToString());

            json.WriteStartArray(DecisionJsonNames.Faults);
            foreach (Fault fault in record.Decision.Faults)
            {
                WriteFault(json, fault, options.IncludeArgumentValues);
            }

            json.WriteEndArray();

            if (record.Decision.Trace is { } trace)
            {
                json.WriteStartArray(DecisionJsonNames.Trace);
                foreach (TraceEntry entry in trace.Entries)
                {
                    json.WriteStartObject();
                    WriteOutcome(json, entry.Text, entry.Result, entry.NotEvaluated, options.IncludeArgumentValues);
                    json.WriteEndObject();
                }

                json.WriteEndArray();
            }

            if (record.Decision.TraceTree is { } tree)
            {
                json.WritePropertyName(DecisionJsonNames.TraceTree);
                WriteTraceNode(json, tree, options.IncludeArgumentValues);
            }

            json.WriteEndObject();
        }

        writer.Write(Encoding.UTF8.GetString(stream.ToArray()));
        writer.Write('\n');
    }

    private static void WriteRule(Utf8JsonWriter json, RuleIdentity rule)
    {
        json.WriteStartObject(DecisionJsonNames.Rule);
        json.WriteString(DecisionJsonNames.CanonicalTextSha256, rule.CanonicalTextSha256);
        if (rule.Name is not null)
        {
            json.WriteString(DecisionJsonNames.Name, rule.Name);
        }

        if (rule.Version is not null)
        {
            json.WriteString(DecisionJsonNames.Version, rule.Version);
        }

        json.WriteEndObject();
    }

    private static void WriteOptions(Utf8JsonWriter json, EvaluationOptions options)
    {
        json.WriteStartObject(DecisionJsonNames.Options);
        if (options.FaultBudget is { } budget)
        {
            json.WriteNumber(DecisionJsonNames.FaultBudget, budget);
        }
        else
        {
            json.WriteNull(DecisionJsonNames.FaultBudget);
        }

        json.WriteString(DecisionJsonNames.Mode, options.Mode.ToString());

        // The constant ("c") format round-trips every TimeSpan exactly and does not depend on the culture.
        if (options.Timeout is { } timeout)
        {
            json.WriteString(DecisionJsonNames.Timeout, timeout.ToString("c", CultureInfo.InvariantCulture));
        }
        else
        {
            json.WriteNull(DecisionJsonNames.Timeout);
        }

        json.WriteBoolean(DecisionJsonNames.IncludeResolvedValues, options.IncludeResolvedValues);
        json.WriteEndObject();
    }

    private static void WriteFault(Utf8JsonWriter json, Fault fault, bool includeArgumentValues)
    {
        json.WriteStartObject();
        json.WriteStartObject(DecisionJsonNames.Term);
        json.WriteString(DecisionJsonNames.Predicate, fault.Term.PredicateName);
        json.WriteStartArray(DecisionJsonNames.Arguments);
        foreach (KeyValuePair<string, LiteralValue> argument in fault.Term.Arguments)
        {
            json.WriteStartObject();
            json.WriteString(DecisionJsonNames.Name, argument.Key);
            json.WriteString(DecisionJsonNames.Kind, argument.Value.Kind.ToString());
            if (includeArgumentValues)
            {
                json.WritePropertyName(DecisionJsonNames.Value);
                WriteLiteral(json, argument.Value);
            }

            json.WriteEndObject();
        }

        json.WriteEndArray();

        // A variable reference names a source and a query, which are rule text, not data, so it is always written.
        json.WriteStartArray(DecisionJsonNames.Variables);
        foreach (KeyValuePair<string, VariableReference> variable in fault.Term.Variables)
        {
            json.WriteStartObject();
            json.WriteString(DecisionJsonNames.Name, variable.Key);
            json.WriteString(DecisionJsonNames.Source, variable.Value.Source);
            json.WriteString(DecisionJsonNames.Query, variable.Value.Query);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();

        json.WriteStartObject(DecisionJsonNames.Exception);
        json.WriteString(DecisionJsonNames.Type, fault.ExceptionType);
        json.WriteString(DecisionJsonNames.Message, fault.Exception.Message);
        if (fault.Exception is VariableResolutionException variableFailure)
        {
            json.WriteStartObject(DecisionJsonNames.Variable);
            json.WriteString(DecisionJsonNames.Source, variableFailure.Reference.Source);
            json.WriteString(DecisionJsonNames.Query, variableFailure.Reference.Query);
            json.WriteString(DecisionJsonNames.Failure, variableFailure.Kind.ToString());
            json.WriteEndObject();
        }

        json.WriteEndObject();
        json.WriteEndObject();
    }

    private static void WriteLiteral(Utf8JsonWriter json, LiteralValue value)
    {
        switch (value.Kind)
        {
            case LiteralKind.String:
                json.WriteStringValue(value.AsString());
                break;
            case LiteralKind.Int64:
                json.WriteNumberValue(value.AsInt64());
                break;
            case LiteralKind.Decimal:
                // Written as a JSON number with its scale (1.50 stays 1.50); the reader reads it as a decimal, not a double.
                json.WriteNumberValue(value.AsDecimal());
                break;
            case LiteralKind.Boolean:
                json.WriteBooleanValue(value.AsBoolean());
                break;
            case LiteralKind.DateTimeOffset:
                json.WriteStringValue(value.AsDateTimeOffset().ToString("O", CultureInfo.InvariantCulture));
                break;
            case LiteralKind.Guid:
                json.WriteStringValue(value.AsGuid().ToString("D"));
                break;
            default:
                json.WriteStartArray();
                foreach (LiteralValue element in value.AsArray())
                {
                    WriteLiteral(json, element);
                }

                json.WriteEndArray();
                break;
        }
    }

    private static void WriteTraceNode(Utf8JsonWriter json, TraceNode node, bool includeArgumentValues)
    {
        json.WriteStartObject();
        WriteOutcome(json, node.Text, node.Result, node.NotEvaluated, includeArgumentValues);
        json.WriteStartArray(DecisionJsonNames.Children);
        foreach (TraceNode child in node.Children)
        {
            WriteTraceNode(json, child, includeArgumentValues);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteOutcome(
        Utf8JsonWriter json,
        string text,
        TruthValue? result,
        bool notEvaluated,
        bool includeArgumentValues
    )
    {
        // A term text is rule text with the literal argument values in it, so it is redacted like a fault term.
        json.WriteString(DecisionJsonNames.Text, includeArgumentValues ? text : TraceTextRedaction.Redact(text));
        if (result is { } value)
        {
            json.WriteString(DecisionJsonNames.Result, value.ToString());
        }
        else
        {
            json.WriteNull(DecisionJsonNames.Result);
        }

        json.WriteBoolean(DecisionJsonNames.NotEvaluated, notEvaluated);
    }
}
