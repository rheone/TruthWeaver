namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;

/// <summary>The format of an input and what decided it.</summary>
/// <param name="Format">The format.</param>
/// <param name="Evidence">What decided <paramref name="Format"/>.</param>
public sealed record FormatDetection(RuleFormat Format, FormatEvidence Evidence);
