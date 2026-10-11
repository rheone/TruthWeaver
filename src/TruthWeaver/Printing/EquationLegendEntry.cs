namespace TruthWeaver.Printing;

using TruthWeaver.Abstractions;

/// <summary>One line of the legend of a simple-variable equation: a letter and the term that it stands for.</summary>
/// <param name="Variable">The letter in the dialect's notation, for example <c>p</c>, <c>p₁</c> (Unicode), <c>p_{1}</c> (LaTeX) or <c>p_1</c> (AsciiMath).</param>
/// <param name="Term">The identity of the term: the predicate name and its arguments.</param>
/// <param name="TermText">The full term call in the dialect's notation, escaped for the dialect, for example <c>hasCrust(crust: "thin")</c>. It shows the argument values even when the equation hides them.</param>
public sealed record EquationLegendEntry(string Variable, TermIdentity Term, string TermText);
