namespace TruthWeaver.Evaluation;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Rewriting;

public sealed partial class CompiledRule<TContext>
{
    /// <summary>
    /// Prints this rule as DSL text with the chosen grouping delimiters. <see cref="GroupingStyle.Parentheses"/> returns
    /// exactly <see cref="CanonicalText"/>; <see cref="GroupingStyle.DepthCycling"/> varies the delimiter by nesting depth
    /// for readability. Every style re-parses to a tree equal to this rule's, because the DSL treats <c>()</c>, <c>[]</c>
    /// and <c>{}</c> as the same grouping.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with. Defaults to <see cref="GroupingStyle.Parentheses"/>.</param>
    /// <param name="operatorStyle">
    /// How to spell <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c>.
    /// <see cref="OperatorStyle.Word"/> (the default, and the only form <see cref="CanonicalText"/> ever uses) writes
    /// the word form. <see cref="OperatorStyle.Symbolic"/> writes the mathematical symbols the DSL parser itself
    /// reads (<c>∧ ∨ ¬ ⊕ ↔ → ↑ ↓</c>), so the result still re-parses to a tree equal to this one.
    /// <see cref="OperatorStyle.CStyle"/> is rejected: the parser does not read <c>^</c> or <c>==</c>, so there is no
    /// round-trippable C-style rule text. <c>COALESCE</c> and <c>If</c> always keep their word/function-call spelling
    /// here, in every style, because the DSL reads <c>??</c> only infix and has no call syntax for it. Call
    /// <see cref="TryPrintRuleText(GroupingStyle, OperatorStyle, out string?)"/> to avoid the exception.
    /// </param>
    /// <param name="constants">
    /// How to spell <c>True</c>, <c>False</c> and <c>Unknown</c>, or <see langword="null"/> (the default) for the word form.
    /// Only a format that the parser reads back is accepted: <see cref="ConstantStyle.Word"/> or
    /// <see cref="ConstantStyle.Letters"/> (<c>T</c>, <c>F</c>, <c>U</c>), with no override, or with an override that is a
    /// letter-case variant of the constant's own word or letter (for example <c>TRUE</c> or <c>t</c>).
    /// <see cref="ConstantStyle.LogicSymbols"/> is rejected, because the parser does not read <c>⊤</c> or <c>⊥</c> at all.
    /// </param>
    /// <returns>The DSL text.</returns>
    /// <exception cref="ArgumentException"><paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>, or <paramref name="constants"/> is a format that the parser does not read back.</exception>
    public string PrintRuleText(
        GroupingStyle grouping = GroupingStyle.Parentheses,
        OperatorStyle operatorStyle = OperatorStyle.Word,
        ConstantFormat? constants = null
    )
    {
        if (operatorStyle == OperatorStyle.CStyle)
        {
            throw new ArgumentException(
                "OperatorStyle.CStyle has no round-trippable rule text: the DSL parser does not read '^' or '=='. "
                    + "Use Word or Symbolic, or call TryPrintRuleText.",
                nameof(operatorStyle)
            );
        }

        if (constants is not null && !constants.IsRuleTextSafe(out string? reason))
        {
            throw new ArgumentException($"{reason} Call TryPrintRuleText to avoid the exception.", nameof(constants));
        }

        return
            grouping == GroupingStyle.Parentheses
            && operatorStyle == OperatorStyle.Word
            && constants is null
            && this.NoteLookup is null
            ? this.CanonicalText
            : CanonicalPrinter.Print(this.Root, grouping, operatorStyle, constants, this.NoteLookup);
    }

    /// <summary>
    /// Writes this rule as DSL text to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/> returns, without a large string
    /// that the caller must hold.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="grouping">The grouping delimiters to print with. Defaults to <see cref="GroupingStyle.Parentheses"/>.</param>
    /// <param name="operatorStyle">How to spell the connectives, as for the string overload. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <param name="constants">How to spell the constants, as for the string overload, or <see langword="null"/> for the word form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>, or <paramref name="constants"/> is a format that the parser does not read back. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintRuleText(
        TextWriter writer,
        GroupingStyle grouping = GroupingStyle.Parentheses,
        OperatorStyle operatorStyle = OperatorStyle.Word,
        ConstantFormat? constants = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintRuleText(grouping, operatorStyle, constants));
    }

    /// <summary>
    /// Renders this rule as DSL text using <paramref name="options"/>. This is the same text as
    /// <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/> for an equivalent grouping, operator
    /// style and constants, with one addition: when <see cref="RuleTextOptions.Width"/> is set, a group that does not
    /// fit the width breaks across lines, indented by <see cref="RuleTextOptions.IndentSize"/> per nesting level. The
    /// breaking rule is documented in <c>docs/rule-text.md</c>. The result still compiles to a tree equal to
    /// <see cref="Root"/>.
    /// </summary>
    /// <param name="options">The printing options.</param>
    /// <returns>The DSL text, one line when <see cref="RuleTextOptions.Width"/> is <see langword="null"/>, otherwise possibly many.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/>'s <see cref="RuleTextOptions.OperatorStyle"/> is <see cref="OperatorStyle.CStyle"/>, or its <see cref="RuleTextOptions.Constants"/> is a format the parser does not read back.</exception>
    public string PrintRuleText(RuleTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.OperatorStyle == OperatorStyle.CStyle)
        {
            throw new ArgumentException(
                "OperatorStyle.CStyle has no round-trippable rule text: the DSL parser does not read '^' or '=='. "
                    + "Use Word or Symbolic, or call TryPrintRuleText.",
                nameof(options)
            );
        }

        if (options.Constants is not null && !options.Constants.IsRuleTextSafe(out string? reason))
        {
            throw new ArgumentException($"{reason} Call TryPrintRuleText to avoid the exception.", nameof(options));
        }

        NoteLookup? notes = options.IncludeNotes ? this.NoteLookup : null;
        return
            options.Width is null
            && options.Grouping == GroupingStyle.Parentheses
            && options.OperatorStyle == OperatorStyle.Word
            && options.Constants is null
            && notes is null
            ? this.CanonicalText
            : CanonicalPrinter.Print(this.Root, options, notes);
    }

    /// <summary>
    /// Writes this rule as DSL text to <paramref name="writer"/> using <paramref name="options"/>. The output is exactly the
    /// text that <see cref="PrintRuleText(RuleTextOptions)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The printing options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> has an operator style or constants that the parser does not read back. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintRuleText(TextWriter writer, RuleTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintRuleText(options));
    }

    /// <summary>
    /// Renders this rule as DSL text, like <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>,
    /// but reports an unsupported <paramref name="operatorStyle"/> through the return value instead of throwing.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with.</param>
    /// <param name="operatorStyle">The operator style. <see cref="OperatorStyle.CStyle"/> is the only value this rejects.</param>
    /// <param name="text">The DSL text, or <see langword="null"/> when <paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>.</param>
    /// <returns><see langword="true"/> unless <paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>.</returns>
    public bool TryPrintRuleText(GroupingStyle grouping, OperatorStyle operatorStyle, [NotNullWhen(true)] out string? text)
    {
        return this.TryPrintRuleText(grouping, operatorStyle, constants: null, out text);
    }

    /// <summary>
    /// Renders this rule as DSL text, like <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>,
    /// but reports an unsupported <paramref name="operatorStyle"/> or <paramref name="constants"/> through the return
    /// value instead of throwing.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with.</param>
    /// <param name="operatorStyle">The operator style. <see cref="OperatorStyle.CStyle"/> is rejected.</param>
    /// <param name="constants">
    /// The constant format, or <see langword="null"/> for the word form. A format that the parser does not read back
    /// is rejected (see <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>).
    /// </param>
    /// <param name="text">The DSL text, or <see langword="null"/> when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> unless <paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/> or <paramref name="constants"/> is not read back.</returns>
    public bool TryPrintRuleText(
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat? constants,
        [NotNullWhen(true)] out string? text
    )
    {
        if (operatorStyle == OperatorStyle.CStyle || constants?.IsRuleTextSafe(out _) == false)
        {
            text = null;
            return false;
        }

        text = this.PrintRuleText(grouping, operatorStyle, constants);
        return true;
    }

    /// <summary>
    /// Renders this rule as DSL text, like <see cref="PrintRuleText(RuleTextOptions)"/>, but reports an unsupported
    /// <see cref="RuleTextOptions.OperatorStyle"/> or <see cref="RuleTextOptions.Constants"/> through the return value
    /// instead of throwing.
    /// </summary>
    /// <param name="options">The printing options. Rejected the same way as <see cref="PrintRuleText(RuleTextOptions)"/>.</param>
    /// <param name="text">The DSL text, or <see langword="null"/> when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> unless <paramref name="options"/> is rejected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public bool TryPrintRuleText(RuleTextOptions options, [NotNullWhen(true)] out string? text)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.OperatorStyle == OperatorStyle.CStyle || options.Constants?.IsRuleTextSafe(out _) == false)
        {
            text = null;
            return false;
        }

        text = this.PrintRuleText(options);
        return true;
    }
}
