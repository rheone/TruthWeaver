namespace TruthWeaver.Tool.Core;

/// <summary>An artifact that an export of every artifact left out, as the <see cref="ExportManifest"/> lists it.</summary>
/// <param name="Id">The artifact id.</param>
/// <param name="Reason">Why the rule cannot give the artifact, such as a truth table over the term cap.</param>
public sealed record SkippedArtifact(string Id, string Reason);
