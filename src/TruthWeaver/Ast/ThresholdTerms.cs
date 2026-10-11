namespace TruthWeaver.Ast;

/// <summary>
/// A count condition written only with "at least" tests: the number of <c>True</c> operands is at least
/// <see cref="AtLeast"/> and is not at least <see cref="NotAtLeast"/>. A <see langword="null"/> bound means that side
/// is not tested.
/// </summary>
/// <param name="AtLeast">The lower test (<c>count &gt;= AtLeast</c>), or <see langword="null"/> for none.</param>
/// <param name="NotAtLeast">The negated upper test (<c>NOT (count &gt;= NotAtLeast)</c>), or <see langword="null"/> for none.</param>
internal readonly record struct ThresholdTerms(int? AtLeast, int? NotAtLeast);
