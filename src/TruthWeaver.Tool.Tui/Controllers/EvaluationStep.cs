namespace TruthWeaver.Tool.Tui.Controllers;

/// <summary>One step of an evaluation: a node and what it returned.</summary>
/// <param name="Path">The path of the node in the tree.</param>
/// <param name="Label">The label of the node, as the tree shows it.</param>
/// <param name="Result">The value as a word (<c>True</c>, <c>False</c> or <c>Unknown</c>), or <c>skipped</c> for a node that short-circuiting never reached.</param>
/// <param name="Fault">The message of the fault at the node, or <see langword="null"/>.</param>
internal sealed record EvaluationStep(string Path, string Label, string Result, string? Fault)
{
    /// <summary>Gets the text of the step for the status line, such as <c>Step 2 of 5: b = skipped</c>.</summary>
    /// <param name="index">The index of the step.</param>
    /// <param name="count">The number of steps.</param>
    /// <returns>The text.</returns>
    public string Describe(int index, int count)
    {
        string text = $"Step {index + 1} of {count}: {this.Label} = {this.Result}";
        return this.Fault is null ? text : $"{text} (fault: {this.Fault})";
    }
}
