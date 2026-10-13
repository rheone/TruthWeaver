namespace TruthWeaver.Tool.Core;

/// <summary>The result of <see cref="OutputWriter.Check"/>: how a file compares with the output, for a <c>--check</c> option.</summary>
/// <param name="Path">The file path that was compared.</param>
/// <param name="Status">How the file compares.</param>
/// <param name="FirstDifferentLine">
/// The 1-based number of the first line that differs, when <paramref name="Status"/> is <see cref="OutputCheckStatus.Differs"/>;
/// otherwise <see langword="null"/>. A difference in the line endings or the encoding counts on the line where it is.
/// </param>
public sealed record OutputCheck(string Path, OutputCheckStatus Status, int? FirstDifferentLine)
{
    /// <summary>Gets a value indicating whether the file holds exactly the output.</summary>
    public bool IsIdentical => this.Status == OutputCheckStatus.Identical;
}
