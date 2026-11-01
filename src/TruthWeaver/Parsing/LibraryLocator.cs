namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// Where a node of a library rule is written: the name of the library rule and the line and column of the node in the
/// library rule text. Every node and diagnostic that a <c>rule("name")</c> reference expands to keeps the span of the
/// reference in the compiled text, because the library rule text is not that text; the locator is what tells two nodes
/// of one library rule apart in a diagnostic message and a trace entry.
/// </summary>
/// <param name="RuleName">The name of the library rule, as the library holds it.</param>
/// <param name="Location">The 1-based line and column of the node in the library rule text.</param>
internal sealed record LibraryLocator(string RuleName, SourceLocation Location)
{
    /// <summary>Creates the locator of a span of a library rule text.</summary>
    /// <param name="ruleName">The name of the library rule.</param>
    /// <param name="ruleText">The library rule text.</param>
    /// <param name="span">The span in <paramref name="ruleText"/>.</param>
    /// <returns>The locator.</returns>
    public static LibraryLocator Of(string ruleName, string ruleText, SourceSpan span)
    {
        return new LibraryLocator(ruleName, span.GetLocation(ruleText));
    }

    /// <summary>
    /// Adds this locator to a diagnostic message as a closing sentence: <c>In library rule 'adult' at line 1, column 7.</c>
    /// follows the message.
    /// </summary>
    /// <param name="message">The message of the diagnostic.</param>
    /// <returns>The message with the locator.</returns>
    public string AppendTo(string message)
    {
        string separator = message.EndsWith('.') ? " " : ". ";
        return $"{message}{separator}In {this}.";
    }

    /// <summary>Gets the locator as text: <c>library rule 'adult' at line 1, column 7</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return $"library rule '{this.RuleName}' at line {this.Location.Line}, column {this.Location.Column}";
    }
}
