namespace TruthWeaver.Tests;

/// <summary>Collection definition that keeps tests which swap an operator's label in the live table away from every other test.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OperatorLabelMutationCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "Operator label mutation";

    private OperatorLabelMutationCollection() { }
}
