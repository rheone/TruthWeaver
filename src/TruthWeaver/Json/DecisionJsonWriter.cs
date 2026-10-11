namespace TruthWeaver.Json;

using System.Text.Encodings.Web;
using System.Text.Json;
using TruthWeaver.Evaluation;

/// <summary>
/// Writes a <see cref="DecisionRecord"/> as JSON with <see cref="Utf8JsonWriter"/>, member by member, so no reflection
/// serializer is involved and the code stays trim and AOT safe. <see cref="DecisionJsonReader"/> reads the same shape back;
/// <c>decision-record.schema.json</c> describes it.
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
            json.WriteNumber("formatVersion", FormatVersion);
            if (record.Rule is { } rule)
            {
                WriteRule(json, rule);
            }

            if (record.Options is { } evaluationOptions)
            {
                WriteOptions(json, evaluationOptions);
            }

            json.WriteString("argumentValues", options.IncludeArgumentValues ? "included" : "omitted");
            json.WriteString("result", record.Decision.Result.ToString());

            json.WriteStartArray("faults");
            foreach (Fault fault in record.Decision.Faults)
            {
                WriteFault(json, fault, options.IncludeArgumentValues);
            }

            json.WriteEndArray();

            if (record.Decision.Trace is { } trace)
            {
                json.WriteStartArray("trace");
                foreach (TraceEntry entry in trace.Entries)
                {
                    json.WriteStartObject();
                    WriteOutcome(json, entry.Text, entry.Result, entry.NotEvaluated);
                    json.WriteEndObject();
                }

                json.WriteEndArray();
            }

            if (record.Decision.TraceTree is { } tree)
            {
                json.WritePropertyName("traceTree");
                WriteTraceNode(json, tree);
            }

            json.WriteEndObject();
        }

        writer.Write(Encoding.UTF8.GetString(stream.ToArray()));
        writer.Write('\n');
    }

    private static void WriteRule(Utf8JsonWriter json, RuleIdentity rule)
    {
        json.WriteStartObject("rule");
        json.WriteString("canonicalTextSha256", rule.CanonicalTextSha256);
        if (rule.Name is not null)
        {
            json.WriteString("name", rule.Name);
        }

        if (rule.Version is not null)
        {
            json.WriteString("version", rule.Version);
        }

        json.WriteEndObject();
    }

    private static void WriteOptions(Utf8JsonWriter json, EvaluationOptions options)
    {
        json.WriteStartObject("options");
        if (options.FaultBudget is { } budget)
        {
            json.WriteNumber("faultBudget", budget);
        }
        else
        {
            json.WriteNull("faultBudget");
        }

        json.WriteString("mode", options.Mode.ToString());

        // The constant ("c") format round-trips every TimeSpan exactly and does not depend on the culture.
        if (options.Timeout is { } timeout)
        {
            json.WriteString("timeout", timeout.ToString("c", CultureInfo.InvariantCulture));
        }
        else
        {
            json.WriteNull("timeout");
        }

        json.WriteBoolean("includeResolvedValues", options.IncludeResolvedValues);
        json.WriteEndObject();
    }

    private static void WriteFault(Utf8JsonWriter json, Fault fault, bool includeArgumentValues)
    {
        json.WriteStartObject();
        json.WriteStartObject("term");
        json.WriteString("predicate", fault.Term.PredicateName);
        json.WriteStartArray("arguments");
        foreach (KeyValuePair<string, LiteralValue> argument in fault.Term.Arguments)
        {
            json.WriteStartObject();
            json.WriteString("name", argument.Key);
            json.WriteString("kind", argument.Value.Kind.ToString());
            if (includeArgumentValues)
            {
                json.WritePropertyName("value");
                WriteLiteral(json, argument.Value);
            }

            json.WriteEndObject();
        }

        json.WriteEndArray();

        // A variable reference names a source and a query, which are rule text, not data, so it is always written.
        json.WriteStartArray("variables");
        foreach (KeyValuePair<string, VariableReference> variable in fault.Term.Variables)
        {
            json.WriteStartObject();
            json.WriteString("name", variable.Key);
            json.WriteString("source", variable.Value.Source);
            json.WriteString("query", variable.Value.Query);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();

        json.WriteStartObject("exception");
        json.WriteString("type", fault.ExceptionType);
        json.WriteString("message", fault.Exception.Message);
        if (fault.Exception is VariableResolutionException variableFailure)
        {
            json.WriteStartObject("variable");
            json.WriteString("source", variableFailure.Reference.Source);
            json.WriteString("query", variableFailure.Reference.Query);
            json.WriteString("failure", variableFailure.Kind.ToString());
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

    private static void WriteTraceNode(Utf8JsonWriter json, TraceNode node)
    {
        json.WriteStartObject();
        WriteOutcome(json, node.Text, node.Result, node.NotEvaluated);
        json.WriteStartArray("children");
        foreach (TraceNode child in node.Children)
        {
            WriteTraceNode(json, child);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteOutcome(Utf8JsonWriter json, string text, TruthValue? result, bool notEvaluated)
    {
        json.WriteString("text", text);
        if (result is { } value)
        {
            json.WriteString("result", value.ToString());
        }
        else
        {
            json.WriteNull("result");
        }

        json.WriteBoolean("notEvaluated", notEvaluated);
    }
}
