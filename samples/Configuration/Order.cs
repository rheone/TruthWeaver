namespace TruthWeaver.Samples.Configuration;

/// <summary>The evaluation context of the sample: the order that a rule is about.</summary>
/// <param name="IsVip">Whether the customer is a VIP.</param>
/// <param name="Total">The order total.</param>
public sealed record Order(bool IsVip, decimal Total);
