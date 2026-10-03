namespace TruthWeaver.Tests.ReferenceDocs;

using System.Globalization;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// One entry of the approved inventory (docs/strong-k3/PROPOSAL.md section 4) together with the independent oracle that
/// computes it. The oracle is <see cref="K3Oracle"/>, not the engine, so a documented table can disagree with the engine
/// only because one of them is wrong, never because both share a bug.
/// </summary>
/// <param name="Name">The canonical name, as written in a marker and (lower-cased) as the document's file name.</param>
/// <param name="Directory">The category directory the document must live in.</param>
/// <param name="Kind"><c>Primitive</c> or <c>Derived</c>, as recorded in the inventory.</param>
/// <param name="Parameters">The operation's parameter names, in call order (<c>k</c>, <c>min</c>, <c>max</c>, ...).</param>
/// <param name="MinOperands">The smallest operand count the reference documents.</param>
/// <param name="MaxOperands">The largest operand count, or <see langword="null"/> when the operation is variadic.</param>
/// <param name="ParameterCombinations">Every valid parameter assignment for an operand count (one empty assignment when none).</param>
/// <param name="Evaluate">The oracle: parameter values (in <paramref name="Parameters"/> order) and operands to a result name.</param>
internal sealed record K3Operation(
    string Name,
    string Directory,
    string Kind,
    string[] Parameters,
    int MinOperands,
    int? MaxOperands,
    Func<int, IEnumerable<string[]>> ParameterCombinations,
    Func<IReadOnlyList<string>, IReadOnlyList<TruthValue>, string> Evaluate
)
{
    private static readonly IEnumerable<string[]> NoParameters =
    [
        [],
    ];

    /// <summary>Gets the approved inventory, keyed by upper-cased name.</summary>
    internal static IReadOnlyDictionary<string, K3Operation> Inventory { get; } = Build();

    /// <summary>Parses a truth cell or literal: <c>T</c>, <c>F</c>, <c>U</c>, or the full word, in any case.</summary>
    /// <param name="text">The text to parse, with surrounding backticks and whitespace already tolerated.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns><see langword="true"/> when the text names a truth value.</returns>
    internal static bool TryParseTruth(string text, out TruthValue value)
    {
        switch (text.Trim('`', ' ').ToUpperInvariant())
        {
            case "T" or "TRUE":
                value = TruthValue.True;
                return true;
            case "F" or "FALSE":
                value = TruthValue.False;
                return true;
            case "U" or "UNKNOWN":
                value = TruthValue.Unknown;
                return true;
            default:
                value = default;
                return false;
        }
    }

    /// <summary>Whether <paramref name="count"/> operands are valid for this operation.</summary>
    internal bool AcceptsOperandCount(int count)
    {
        return count >= this.MinOperands && (this.MaxOperands is null || count <= this.MaxOperands);
    }

    private static Dictionary<string, K3Operation> Build()
    {
        static string S(TruthValue value) => value.ToString();

        static IEnumerable<string[]> Range(int low, int high) =>
            Enumerable.Range(low, Math.Max(0, high - low + 1)).Select(i => new[] { i.ToString(CultureInfo.InvariantCulture) });

        static TruthValue Truth(string text) => TryParseTruth(text, out TruthValue v) ? v : throw new FormatException(text);

        static int Int(string text) => int.Parse(text, CultureInfo.InvariantCulture);

        // The threshold family accepts one operand in the compiler (inventory open question 10), so the reference does too.
        // BETWEEN's valid bounds are 0 <= min <= max <= n, excluding the whole 0..n range, which the engine rejects as vacuous.
        K3Operation[] operations =
        [
            new("NOT", "gates", "Primitive", [], 1, 1, _ => NoParameters, (_, x) => S(K3Oracle.Not(x[0]))),
            new("AND", "gates", "Primitive", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.And(x))),
            new("OR", "gates", "Primitive", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.Or(x))),
            new("IMPLIES", "derived", "Derived", [], 2, 2, _ => NoParameters, (_, x) => S(K3Oracle.Implies(x[0], x[1]))),
            new("EQUIVALENT", "derived", "Derived", [], 2, 2, _ => NoParameters, (_, x) => S(K3Oracle.Equivalent(x[0], x[1]))),
            new("XOR", "derived", "Derived", [], 2, 2, _ => NoParameters, (_, x) => S(K3Oracle.Xor(x[0], x[1]))),
            new("NAND", "derived", "Derived", [], 2, 2, _ => NoParameters, (_, x) => S(K3Oracle.Nand(x[0], x[1]))),
            new("NOR", "derived", "Derived", [], 2, 2, _ => NoParameters, (_, x) => S(K3Oracle.Nor(x[0], x[1]))),
            new("PARITY", "derived", "Derived", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.Parity(x))),
            new(
                "AtLeast",
                "cardinality",
                "Primitive",
                ["k"],
                1,
                null,
                n => Range(1, n),
                (p, x) => S(K3Oracle.Cardinality(c => c >= Int(p[0]), x))
            ),
            new(
                "AtMost",
                "cardinality",
                "Primitive",
                ["k"],
                1,
                null,
                n => Range(0, n - 1),
                (p, x) => S(K3Oracle.Cardinality(c => c <= Int(p[0]), x))
            ),
            new(
                "Exactly",
                "cardinality",
                "Primitive",
                ["k"],
                1,
                null,
                n => Range(0, n),
                (p, x) => S(K3Oracle.Cardinality(c => c == Int(p[0]), x))
            ),
            new("ExactlyOne", "cardinality", "Derived", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.ExactlyOne(x))),
            new(
                "GreaterThan",
                "cardinality",
                "Derived",
                ["k"],
                1,
                null,
                n => Range(0, n - 1),
                (p, x) => S(K3Oracle.Cardinality(c => c > Int(p[0]), x))
            ),
            new(
                "LessThan",
                "cardinality",
                "Derived",
                ["k"],
                1,
                null,
                n => Range(1, n),
                (p, x) => S(K3Oracle.Cardinality(c => c < Int(p[0]), x))
            ),
            new("ANY", "cardinality", "Derived", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.Any(x))),
            new("ALL", "cardinality", "Derived", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.All(x))),
            new("NONE", "cardinality", "Derived", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.None(x))),
            new(
                "BETWEEN",
                "cardinality",
                "Derived",
                ["min", "max"],
                2,
                null,
                n =>
                    from min in Enumerable.Range(0, n + 1)
                    from max in Enumerable.Range(min, n - min + 1)
                    where !(min == 0 && max == n)
                    select new[] { min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture) },
                (p, x) => S(K3Oracle.Between(Int(p[0]), Int(p[1]), x))
            ),
            new("COALESCE", "functions", "Primitive", [], 2, null, _ => NoParameters, (_, x) => S(K3Oracle.Coalesce(x))),
            new("If", "functions", "Derived", [], 3, 3, _ => NoParameters, (_, x) => S(K3Oracle.If(x[0], x[1], x[2]))),
            new("IsTrue", "functions", "Derived", [], 1, 1, _ => NoParameters, (_, x) => S(K3Oracle.IsTrue(x[0]))),
            new("IsFalse", "functions", "Derived", [], 1, 1, _ => NoParameters, (_, x) => S(K3Oracle.IsFalse(x[0]))),
            new("IsUnknown", "functions", "Derived", [], 1, 1, _ => NoParameters, (_, x) => S(K3Oracle.IsUnknown(x[0]))),
            new("IsKnown", "functions", "Derived", [], 1, 1, _ => NoParameters, (_, x) => S(K3Oracle.IsKnown(x[0]))),
            new(
                "Project",
                "result-transformations",
                "Derived",
                ["unknownAs"],
                1,
                1,
                _ =>
                    [
                        ["False"],
                        ["True"],
                    ],
                (p, x) => S(K3Oracle.Project(x[0], Truth(p[0])))
            ),
            new(
                "Collapse",
                "result-transformations",
                "Primitive",
                ["policy"],
                1,
                1,
                _ => Enum.GetNames<CollapsePolicy>().Select(name => new[] { name }),
                (p, x) => K3Oracle.Collapse(x[0], Enum.Parse<CollapsePolicy>(p[0])).ToString()
            ),
        ];

        return operations.ToDictionary(operation => operation.Name.ToUpperInvariant(), StringComparer.Ordinal);
    }
}
