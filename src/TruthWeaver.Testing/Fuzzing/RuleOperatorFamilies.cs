namespace TruthWeaver.Testing.Fuzzing;

/// <summary>The groups of rule operators that <see cref="K3RuleGenerator"/> can draw from.</summary>
[Flags]
public enum RuleOperatorFamilies
{
    /// <summary>No operator: the rule is a single term or constant.</summary>
    None = 0,

    /// <summary><c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>IMPLIES</c>, <c>NAND</c>, <c>NOR</c> and <c>PARITY</c>.</summary>
    Connectives = 1,

    /// <summary><c>ANY</c>, <c>ALL</c>, <c>NONE</c>, <c>BETWEEN</c>, <c>ExactlyOne</c> and the threshold operators such as <c>AtLeast</c>.</summary>
    Cardinality = 2,

    /// <summary><c>If</c>.</summary>
    Conditional = 4,

    /// <summary>The inspections <c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c> and <c>IsKnown</c>.</summary>
    Inspection = 8,

    /// <summary><c>COALESCE</c>, with a list of operands and with a replacement value.</summary>
    Coalescing = 16,

    /// <summary>Every family.</summary>
    All = Connectives | Cardinality | Conditional | Inspection | Coalescing,
}
