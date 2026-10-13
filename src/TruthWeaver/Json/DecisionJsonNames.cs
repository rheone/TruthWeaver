namespace TruthWeaver.Json;

/// <summary>
/// The member names and fixed string values of the decision record JSON, shared by <see cref="DecisionJsonWriter"/> and
/// <see cref="DecisionJsonReader"/> so the two cannot drift apart. <c>decision-record.schema.json</c> names the same members.
/// </summary>
internal static class DecisionJsonNames
{
    /// <summary>The root member that holds the format version.</summary>
    public const string FormatVersion = "formatVersion";

    /// <summary>The root member that holds the rule identity.</summary>
    public const string Rule = "rule";

    /// <summary>The rule member that holds the canonical text hash.</summary>
    public const string CanonicalTextSha256 = "canonicalTextSha256";

    /// <summary>The member that holds a name: of the rule, an argument or a variable.</summary>
    public const string Name = "name";

    /// <summary>The rule member that holds the version.</summary>
    public const string Version = "version";

    /// <summary>The root member that holds the evaluation options.</summary>
    public const string Options = "options";

    /// <summary>The options member that holds the fault budget.</summary>
    public const string FaultBudget = "faultBudget";

    /// <summary>The options member that holds the evaluation mode.</summary>
    public const string Mode = "mode";

    /// <summary>The options member that holds the timeout.</summary>
    public const string Timeout = "timeout";

    /// <summary>The options member that says whether the trace shows resolved values.</summary>
    public const string IncludeResolvedValues = "includeResolvedValues";

    /// <summary>The root member that says whether literal argument values are in the record.</summary>
    public const string ArgumentValues = "argumentValues";

    /// <summary>The <see cref="ArgumentValues"/> value for a record that holds the literal argument values.</summary>
    public const string Included = "included";

    /// <summary>The <see cref="ArgumentValues"/> value for a record without the literal argument values.</summary>
    public const string Omitted = "omitted";

    /// <summary>The member that holds a result: of the decision, a trace entry or a trace node.</summary>
    public const string Result = "result";

    /// <summary>The root member that holds the faults.</summary>
    public const string Faults = "faults";

    /// <summary>The fault member that holds the term.</summary>
    public const string Term = "term";

    /// <summary>The term member that holds the predicate name.</summary>
    public const string Predicate = "predicate";

    /// <summary>The term member that holds the literal arguments.</summary>
    public const string Arguments = "arguments";

    /// <summary>The argument member that holds the literal kind.</summary>
    public const string Kind = "kind";

    /// <summary>The argument member that holds the literal value.</summary>
    public const string Value = "value";

    /// <summary>The term member that holds the variable arguments.</summary>
    public const string Variables = "variables";

    /// <summary>The member that holds the data source name of a variable reference.</summary>
    public const string Source = "source";

    /// <summary>The member that holds the query of a variable reference.</summary>
    public const string Query = "query";

    /// <summary>The fault member that holds the exception.</summary>
    public const string Exception = "exception";

    /// <summary>The exception member that holds the full type name.</summary>
    public const string Type = "type";

    /// <summary>The exception member that holds the message.</summary>
    public const string Message = "message";

    /// <summary>The exception member that holds the reference of a variable failure.</summary>
    public const string Variable = "variable";

    /// <summary>The variable failure member that holds the failure kind.</summary>
    public const string Failure = "failure";

    /// <summary>The root member that holds the flat trace.</summary>
    public const string Trace = "trace";

    /// <summary>The root member that holds the trace tree.</summary>
    public const string TraceTree = "traceTree";

    /// <summary>The member that holds the text of a trace entry or a trace node.</summary>
    public const string Text = "text";

    /// <summary>The member that says whether a trace entry or a trace node was skipped.</summary>
    public const string NotEvaluated = "notEvaluated";

    /// <summary>The trace node member that holds the operand nodes.</summary>
    public const string Children = "children";
}
