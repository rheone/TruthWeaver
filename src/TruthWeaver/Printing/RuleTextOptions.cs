namespace TruthWeaver.Printing;

/// <summary>
/// The options for <see cref="Evaluation.CompiledRule{TContext}.PrintRuleText(RuleTextOptions)"/> and
/// <see cref="Evaluation.CompiledRule{TContext}.TryPrintRuleText(RuleTextOptions, out string?)"/>. One options object
/// replaces the individual <c>grouping</c>/<c>operatorStyle</c>/<c>constants</c> parameters of the other
/// <c>PrintRuleText</c> overloads and adds width-aware line breaking.
/// </summary>
public sealed record RuleTextOptions
{
    /// <summary>
    /// Gets the default options: one-line output (<see cref="Width"/> is <see langword="null"/>), 2-space indents,
    /// <see cref="GroupingStyle.Parentheses"/>, <see cref="Printing.OperatorStyle.Word"/> and the word constants. They give
    /// the output of <see cref="Evaluation.CompiledRule{TContext}.CanonicalText"/>.
    /// </summary>
    public static RuleTextOptions Default { get; } = new();

    /// <summary>
    /// Gets the target line width in characters, or <see langword="null"/> (the default) for today's one-line output.
    /// When set, a group that does not fit within this width breaks across lines: see the breaking rule in
    /// <c>docs/rule-text.md</c>. A single token that is longer than this width on its own (a long term name, for
    /// example) is printed whole on its own line rather than split, so a very small width never loops or throws.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Gets the number of spaces added per nesting level when a group breaks across lines. Ignored when
    /// <see cref="Width"/> is <see langword="null"/>. The default is 2. The value must not be negative.
    /// </summary>
    public int IndentSize { get; init; } = 2;

    /// <summary>Gets the grouping delimiters. Defaults to <see cref="Printing.GroupingStyle.Parentheses"/>.</summary>
    public GroupingStyle Grouping { get; init; } = GroupingStyle.Parentheses;

    /// <summary>
    /// Gets the operator spelling. Defaults to <see cref="Printing.OperatorStyle.Word"/>.
    /// <see cref="Printing.OperatorStyle.CStyle"/> is rejected the same way as the other <c>PrintRuleText</c> overloads,
    /// because the DSL parser does not read it back.
    /// </summary>
    public OperatorStyle OperatorStyle { get; init; } = OperatorStyle.Word;

    /// <summary>
    /// Gets the constant format, or <see langword="null"/> for the word form. A format the parser does not read back is
    /// rejected the same way as the other <c>PrintRuleText</c> overloads (see <see cref="ConstantFormat.IsRuleTextSafe"/>).
    /// </summary>
    public ConstantFormat? Constants { get; init; }
}
