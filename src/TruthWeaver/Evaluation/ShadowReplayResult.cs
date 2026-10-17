namespace TruthWeaver.Evaluation;

/// <summary>The outcome of a <see cref="ShadowReplay"/>: how many contexts were replayed and which ones changed.</summary>
/// <typeparam name="TContext">The context type of the rules.</typeparam>
/// <param name="ContextCount">The number of contexts replayed.</param>
/// <param name="Changes">The contexts whose decision changed, in the order of the replayed sequence.</param>
public sealed record ShadowReplayResult<TContext>(int ContextCount, IReadOnlyList<ShadowReplayChange<TContext>> Changes)
{
    /// <summary>Gets the number of contexts whose decision did not change.</summary>
    public int UnchangedCount => this.ContextCount - this.Changes.Count;
}
