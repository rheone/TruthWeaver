namespace TruthWeaver.Samples.Consumer;

/// <summary>
/// The application context. Every predicate in the sample reads from it, and every evaluation
/// receives one instance.
/// </summary>
/// <param name="LovesPineapple">Whether the customer likes pineapple on pizza.</param>
/// <param name="OrderCount">How many orders the customer placed before.</param>
public sealed record Customer(bool LovesPineapple, int OrderCount);
