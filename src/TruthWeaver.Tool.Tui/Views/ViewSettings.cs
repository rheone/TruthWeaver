namespace TruthWeaver.Tool.Tui.Views;

using TruthWeaver.Printing;

/// <summary>
/// The toggles of the view pane. A value of <see langword="null"/> or the default leaves the matching printer option at
/// the library default, so an untouched view prints exactly what the library printer prints with default options.
/// </summary>
internal sealed class ViewSettings
{
    /// <summary>Gets or sets the form to show.</summary>
    public ViewKind Kind { get; set; } = ViewKind.Tree;

    /// <summary>Gets or sets the equation dialect. It applies to <see cref="ViewKind.Equation"/>.</summary>
    public EquationDialect Dialect { get; set; } = EquationDialect.Unicode;

    /// <summary>Gets or sets the LaTeX envelope. It applies to the LaTeX dialect only.</summary>
    public LatexWrapMode LatexWrap { get; set; } = LatexWrapMode.None;

    /// <summary>Gets or sets the operator style, or <see langword="null"/> for the printer default.</summary>
    public OperatorStyle? OperatorStyle { get; set; }

    /// <summary>Gets or sets the constant style, or <see langword="null"/> for the printer default.</summary>
    public ConstantStyle? ConstantStyle { get; set; }

    /// <summary>Gets or sets the text that replaces the constant <c>True</c>, or <see langword="null"/> for none.</summary>
    public string? TrueText { get; set; }

    /// <summary>Gets or sets the text that replaces the constant <c>False</c>, or <see langword="null"/> for none.</summary>
    public string? FalseText { get; set; }

    /// <summary>Gets or sets the text that replaces the constant <c>Unknown</c>, or <see langword="null"/> for none.</summary>
    public string? UnknownText { get; set; }

    /// <summary>Gets or sets a value indicating whether terms print as simple variables. It applies to equations and HTML.</summary>
    public bool SimpleVariables { get; set; }

    /// <summary>Gets or sets a value indicating whether the current decision shows in the tree, Mermaid, DOT and HTML views.</summary>
    public bool ShowDecision { get; set; } = true;

    /// <summary>Gets a value indicating whether the constant format differs from the printer default.</summary>
    public bool HasConstantFormat =>
        this.ConstantStyle is not null
        || this.TrueText is not null
        || this.FalseText is not null
        || this.UnknownText is not null;
}
