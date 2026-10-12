namespace TruthWeaver.Testing;

/// <summary>A problem in a rule test file that makes the file unusable, such as a missing key or a value that is not a truth value.</summary>
/// <param name="Message">What is wrong.</param>
/// <param name="Path">The path of the value in the file, such as <c>$.cases[1].expect.result</c>.</param>
/// <param name="Line">The 1-based line of the value, or 0 when the problem has no place in the file.</param>
/// <param name="Column">The 1-based column of the value, or 0 when the problem has no place in the file.</param>
public sealed record RuleTestFileError(string Message, string Path, int Line, int Column)
{
    /// <summary>Returns the place and the message, such as <c>line 4, column 7 ($.cases[0].given): ...</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return this.Line > 0
            ? $"line {this.Line}, column {this.Column} ({this.Path}): {this.Message}"
            : $"{this.Path}: {this.Message}";
    }
}
