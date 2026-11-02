namespace TruthWeaver.Samples.Policies;

/// <summary>The evaluation context of the sample: who asks, and what is known about them.</summary>
/// <param name="IsStaff">Whether the requester is a staff member.</param>
/// <param name="IsPremium">Whether the requester has a premium plan.</param>
/// <param name="IsBlocked">Whether the requester is blocked.</param>
public sealed record Request(bool IsStaff, bool IsPremium, bool IsBlocked);
