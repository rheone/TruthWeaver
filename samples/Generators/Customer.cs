namespace TruthWeaver.Samples.Generators;

/// <summary>The evaluation context of the sample: the customer that a rule is about.</summary>
/// <param name="OrderCount">How many orders the customer placed.</param>
public sealed record Customer(int OrderCount);
