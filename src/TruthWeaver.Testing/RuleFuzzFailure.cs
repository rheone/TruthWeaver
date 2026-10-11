namespace TruthWeaver.Testing;

using System.Globalization;

/// <summary>One failed check of one generated rule. Run the fuzzer again with <see cref="Seed"/> to get the same failure.</summary>
/// <param name="Seed">The seed of the run.</param>
/// <param name="RuleIndex">The zero-based position of the rule in the run.</param>
/// <param name="RuleText">The generated rule text.</param>
/// <param name="Check">The check that failed.</param>
/// <param name="Detail">What the check found, for example the assignment where two values differ.</param>
public sealed record RuleFuzzFailure(int Seed, int RuleIndex, string RuleText, RuleFuzzCheck Check, string Detail)
{
    /// <summary>Returns the check, the seed, the rule index, the rule text and the detail on one line.</summary>
    /// <returns>The failure text.</returns>
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{this.Check}: seed {this.Seed}, rule {this.RuleIndex}: {this.RuleText} -- {this.Detail}"
        );
    }
}
