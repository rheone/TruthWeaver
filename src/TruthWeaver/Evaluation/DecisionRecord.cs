namespace TruthWeaver.Evaluation;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Json;

/// <summary>
/// A <see cref="Abstractions.Decision"/> in a form that can be stored as JSON for an audit log and read back for replay: the
/// decision, the rule that made it and the evaluation options it was made with. The JSON shape is described by
/// <c>decision-record.schema.json</c>, which ships in the package content.
/// </summary>
/// <param name="Decision">The decision: its result, faults, trace and trace tree.</param>
/// <param name="Rule">The rule that made the decision, or <see langword="null"/> when it is not recorded.</param>
/// <param name="Options">The evaluation options the decision was made with, or <see langword="null"/> when they are not recorded.</param>
public sealed record DecisionRecord(Decision Decision, RuleIdentity? Rule = null, EvaluationOptions? Options = null)
{
    /// <summary>
    /// Prints this record as indented JSON with line feeds. Literal argument values of faulting terms are omitted unless
    /// <paramref name="options"/> includes them. The trace is written as the decision holds it: it shows a value from a data
    /// source only when the evaluation set <see cref="EvaluationOptions.IncludeResolvedValues"/>.
    /// </summary>
    /// <param name="options">What to include, or <see langword="null"/> for <see cref="DecisionJsonOptions.Default"/>.</param>
    /// <returns>The JSON text, ending with a line feed.</returns>
    public string PrintJson(DecisionJsonOptions? options = null)
    {
        using StringWriter writer = new();
        DecisionJsonWriter.Write(this, options ?? DecisionJsonOptions.Default, writer);
        return writer.ToString();
    }

    /// <summary>Writes the text of <see cref="PrintJson(DecisionJsonOptions?)"/> to <paramref name="writer"/>. The writer is not flushed or disposed.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="options">What to include, or <see langword="null"/> for <see cref="DecisionJsonOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    public void PrintJson(TextWriter writer, DecisionJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        DecisionJsonWriter.Write(this, options ?? DecisionJsonOptions.Default, writer);
    }

    /// <summary>
    /// Reads a record that <see cref="PrintJson(DecisionJsonOptions?)"/> wrote. A fault reads back with a
    /// <see cref="RecordedFaultException"/> (or a <see cref="VariableResolutionException"/> for a variable failure) that
    /// keeps the type name and message, so the read decision equals the written one when the argument values were included.
    /// Never throws for bad input.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <param name="record">The record when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="error">
    /// When this method returns <see langword="false"/>, what is wrong and where, as a JSON path such as
    /// <c>$.faults[0].term.predicate</c>; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when the text is a valid decision record; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryParseJson(
        [StringSyntax(StringSyntaxAttribute.Json)] string json,
        [NotNullWhen(true)] out DecisionRecord? record,
        [NotNullWhen(false)] out string? error
    )
    {
        ArgumentNullException.ThrowIfNull(json);
        return DecisionJsonReader.TryRead(json, out record, out error);
    }
}
