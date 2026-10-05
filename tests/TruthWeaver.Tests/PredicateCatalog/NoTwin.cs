namespace TruthWeaver.Tests.PredicateCatalog;

/// <summary>A factory that has no twin by design.</summary>
/// <param name="Factory">The key of the factory.</param>
/// <param name="Reason">Why the factory has no twin.</param>
internal sealed record NoTwin(string Factory, string Reason) : TwinTableEntry
{
    /// <inheritdoc />
    public override IEnumerable<string> Factories => [this.Factory];
}
