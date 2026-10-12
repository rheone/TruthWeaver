namespace TruthWeaver.Evaluation;

/// <summary>What <see cref="DecisionRecord.PrintJson(DecisionJsonOptions?)"/> writes beyond the default content.</summary>
/// <param name="IncludeArgumentValues">
/// <see langword="true"/> to write the literal argument values of the rule. The default <see langword="false"/> keeps
/// every literal argument value out of the record, because an audit log may not be allowed to hold them: a faulting term
/// has only the name and kind of each argument, and each trace text and trace tree text has <c>&lt;omitted&gt;</c> in
/// place of each literal argument value. A value that a data source resolved is not affected; the trace holds it only
/// when the evaluation set <see cref="EvaluationOptions.IncludeResolvedValues"/>. A record written without the values
/// reads back with fault terms that have no literal arguments and with the redacted trace texts.
/// </param>
public sealed record DecisionJsonOptions(bool IncludeArgumentValues = false)
{
    /// <summary>Gets the default options: literal argument values are omitted.</summary>
    public static DecisionJsonOptions Default { get; } = new();
}
