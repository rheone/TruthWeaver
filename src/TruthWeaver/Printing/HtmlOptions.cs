namespace TruthWeaver.Printing;

/// <summary>The options for <see cref="Evaluation.CompiledRule{TContext}.PrintHtml(HtmlOptions?)"/>.</summary>
public sealed record HtmlOptions
{
    /// <summary>
    /// Gets the default options: word operators, argument values shown, terms as their labels and the constants as the words
    /// <c>True</c>, <c>False</c> and <c>Unknown</c>.
    /// </summary>
    public static HtmlOptions Default { get; } = new();

    /// <summary>
    /// Gets how operator labels are written. The symbols are HTML entities (<c>&amp;and;</c>, <c>&amp;or;</c>). Defaults to
    /// <see cref="Printing.OperatorStyle.Word"/>.
    /// </summary>
    public OperatorStyle OperatorStyle { get; init; } = OperatorStyle.Word;

    /// <summary>Gets a value indicating whether a term's rule-text argument values appear after its label. Defaults to <see langword="true"/>.</summary>
    public bool ShowArgumentValues { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether each term prints as a letter (<c>p</c>, <c>q</c>, <c>r</c>) instead of its label. The letters
    /// follow the first occurrence of a term, depth first and left to right, and identical terms share a letter. After the
    /// eleventh distinct term (<c>z</c>), the letters repeat with a subscript: <c>p<sub>1</sub></c>. The full term stays in the
    /// <c>title</c> attribute of the term's <c>span</c>. Defaults to <see langword="false"/>.
    /// </summary>
    public bool SimpleVariables { get; init; }

    /// <summary>Gets how the constants <c>True</c>, <c>False</c> and <c>Unknown</c> are written. Defaults to <see cref="ConstantFormat.Default"/>.</summary>
    public ConstantFormat Constants { get; init; } = ConstantFormat.Default;
}
