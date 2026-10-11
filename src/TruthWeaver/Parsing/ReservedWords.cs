namespace TruthWeaver.Parsing;

/// <summary>
/// The words the rule text reads as keywords, compared without case. A predicate cannot have one of these names, because
/// the parser would read a call to it as the keyword. This file is the single list: the registry builder reads it through
/// <c>DslParser.IsReservedWord</c>, and the source generator project compiles the same file, so the two cannot
/// disagree.
/// </summary>
internal static class ReservedWords
{
    private static readonly HashSet<string> Words =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "AND",
        "OR",
        "NOT",
        "XOR",
        "EQUIVALENT",
        "IFF",
        "XNOR",
        "IMPLIES",
        "NAND",
        "NOR",
        "TRUE",
        "FALSE",
        "UNKNOWN",
        "PARITY",
        "NXOR",
        "ANY",
        "ALL",
        "NONE",
        "BETWEEN",
        "COALESCE",
        "IF",
        "ISTRUE",
        "ISFALSE",
        "ISUNKNOWN",
        "ISKNOWN",
        "PROJECT",
        "COLLAPSE",
        "EXACTLYONE",
        "ATLEAST",
        "ATMOST",
        "GREATERTHAN",
        "LESSTHAN",
        "EXACTLY",
    ];

    /// <summary>Determines whether an identifier is a reserved keyword.</summary>
    /// <param name="identifier">The identifier text.</param>
    /// <returns><see langword="true"/> if the identifier equals a keyword, ignoring case.</returns>
    public static bool Contains(string identifier)
    {
        return Words.Contains(identifier);
    }
}
