namespace TruthWeaver.Samples.Yaml;

/// <summary>The evaluation context of the sample: the person the rule is about.</summary>
/// <param name="IsActive">Whether the membership is active.</param>
/// <param name="IsSuspended">Whether the member is suspended.</param>
public sealed record Member(bool IsActive, bool IsSuspended);
