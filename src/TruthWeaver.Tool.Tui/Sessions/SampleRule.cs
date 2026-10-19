namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>One sample rule of the Samples menu: a name, a one-line description and the rule text with notes.</summary>
internal sealed record SampleRule
{
    /// <summary>Gets the name in the menu.</summary>
    public required string Name { get; init; }

    /// <summary>Gets one line that says what the sample shows.</summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the rule text. It compiles in the permissive mode of the workbench, and its notes (comments) explain the logic.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the diagnostic code that the sample reports on purpose, or <see langword="null"/> when the sample compiles
    /// without an error. A sample may show a diagnostic only when its notes explain it.
    /// </summary>
    public string? ExpectedDiagnostic { get; init; }
}
