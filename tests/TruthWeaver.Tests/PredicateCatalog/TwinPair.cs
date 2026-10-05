namespace TruthWeaver.Tests.PredicateCatalog;

/// <summary>
/// A positive factory and its <c>NotX</c> twin, with one probe for each answer of the positive factory. A probe is the
/// rule-text arguments plus a context whose selected value makes the positive factory answer True, False or Unknown.
/// </summary>
/// <param name="Positive">The key of the positive factory.</param>
/// <param name="Twin">The key of the twin factory.</param>
/// <param name="PositiveFactory">Registers the positive factory.</param>
/// <param name="TwinFactory">Registers the twin factory, configured like the positive one.</param>
/// <param name="Arguments">The rule-text arguments both factories receive.</param>
/// <param name="WhenTrue">A context the positive factory answers True for, or null when no such value exists.</param>
/// <param name="WhenFalse">A context the positive factory answers False for, or null when no such value exists.</param>
/// <param name="WhenUnknown">A context the positive factory answers Unknown for, or null when no such value exists.</param>
/// <param name="Unreachable">Why a probe is null. Required when any probe is null.</param>
internal sealed record TwinPair(
    string Positive,
    string Twin,
    ProbeFactory PositiveFactory,
    ProbeFactory TwinFactory,
    (string Name, object Value)[] Arguments,
    TwinProbeContext? WhenTrue,
    TwinProbeContext? WhenFalse,
    TwinProbeContext? WhenUnknown,
    string? Unreachable = null
) : TwinTableEntry
{
    /// <inheritdoc />
    public override IEnumerable<string> Factories => [this.Positive, this.Twin];
}
