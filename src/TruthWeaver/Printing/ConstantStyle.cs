namespace TruthWeaver.Printing;

/// <summary>
/// How a printer writes the three constants <c>True</c>, <c>False</c> and <c>Unknown</c>. The set has no <c>1</c>/<c>0</c>/<c>½</c>
/// form on purpose: the order <c>False &lt; Unknown &lt; True</c> must not read as a number.
/// </summary>
/// <remarks>
/// <see cref="Letters"/> is the recommended style for strict-logic output: <c>U</c> cannot be mistaken for the
/// bottom element of a lattice. <see cref="LogicSymbols"/> uses <c>⊥</c> for <c>False</c>, but in partial logic and
/// domain theory <c>⊥</c> often means "no information", which is the meaning of <c>Unknown</c>. A reader who knows
/// that convention can take the symbol the wrong way. Only <see cref="Word"/> is read back by the rule text parser.
/// </remarks>
public enum ConstantStyle
{
    /// <summary>The default: <c>True</c>, <c>False</c>, <c>Unknown</c>. This is the only style that rule text can read back.</summary>
    Word,

    /// <summary>One letter each: <c>T</c>, <c>F</c>, <c>U</c>. The recommended style for strict-logic output.</summary>
    Letters,

    /// <summary>Logic symbols: <c>⊤</c> for <c>True</c>, <c>⊥</c> for <c>False</c>, and <c>U</c> for <c>Unknown</c>, which has no standard symbol.</summary>
    LogicSymbols,
}
