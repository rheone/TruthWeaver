namespace TruthWeaver.Printing;

/// <summary>
/// An equation in simple-variable mode together with its legend. The equation uses a letter for each term. The legend
/// is available as data (<see cref="Legend"/>) and as text (<see cref="LegendText"/>).
/// </summary>
/// <param name="Equation">The equation text with a letter for each term, wrapped for the dialect.</param>
/// <param name="Legend">
/// One entry per distinct term. The order is the letter order: the first occurrence of each term in a depth-first,
/// left-to-right walk of the rule. The list is empty for a rule with no term.
/// </param>
/// <param name="LegendText">
/// The legend as text: one <c>letter = term</c> line per entry, joined with <c>\n</c>. Each line is its own equation
/// in the dialect, so it has the dialect's envelope. The text is empty when the legend is empty.
/// </param>
public sealed record EquationWithLegend(string Equation, IReadOnlyList<EquationLegendEntry> Legend, string LegendText);
