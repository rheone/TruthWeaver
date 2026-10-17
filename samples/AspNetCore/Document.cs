namespace TruthWeaver.Samples.AspNetCore;

/// <summary>A document that a user owns. The resource of the <c>CanEdit</c> check.</summary>
/// <param name="Id">The document identifier.</param>
/// <param name="OwnerId">The user identifier of the owner.</param>
/// <param name="Title">The title.</param>
public sealed record Document(string Id, string OwnerId, string Title);
