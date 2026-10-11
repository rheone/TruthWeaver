namespace TruthWeaver.Samples.PredicateLibrary;

/// <summary>
/// The application context that the predicates in this library read. The application that compiles
/// and evaluates rules supplies one instance to each evaluation.
/// </summary>
/// <param name="IsActive">Whether the account is active, or <see langword="null"/> when this is not known.</param>
/// <param name="OrderCount">How many orders the account placed.</param>
public sealed record Account(bool? IsActive, int OrderCount);
