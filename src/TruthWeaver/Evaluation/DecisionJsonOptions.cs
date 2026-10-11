namespace TruthWeaver.Evaluation;

/// <summary>What <see cref="DecisionRecord.PrintJson(DecisionJsonOptions?)"/> writes beyond the default content.</summary>
/// <param name="IncludeArgumentValues">
/// <see langword="true"/> to write the literal argument values of each faulting term. The default
/// <see langword="false"/> writes only the name and kind of each argument, because an audit log may not be allowed to
/// hold them. A record written without the values reads back with fault terms that have no literal arguments.
/// </param>
public sealed record DecisionJsonOptions(bool IncludeArgumentValues = false)
{
    /// <summary>Gets the default options: argument values are omitted.</summary>
    public static DecisionJsonOptions Default { get; } = new();
}
