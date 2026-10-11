namespace TruthWeaver.Tests;

/// <summary>Collection definition that keeps tests which swap the host time zone away from every other test.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HostTimeZoneCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "Host time zone";

    private HostTimeZoneCollection() { }
}
