namespace TruthWeaver.AspNetCore.Tests;

/// <summary>A resource with an owner, for resource-based checks.</summary>
/// <param name="Id">The document identifier.</param>
/// <param name="OwnerId">The identifier of the owner, or <see langword="null"/> for none.</param>
internal sealed record Document(string Id, string? OwnerId);
