namespace TruthWeaver.Tool.Tui.Sessions;

using System.Text.RegularExpressions;

/// <summary>
/// The sample rules of the workbench: a short list that together shows predicates with arguments, the connectives, the
/// cardinality family, <c>If</c>, the inspections, <c>let</c> definitions, <c>from(...)</c> variables and notes.
/// A sample is rule text, so it replaces the rule of the editor and goes through the undo history.
/// </summary>
internal static class SampleRules
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Gets the parts of the language that at least one sample must show. A test fails for a part that has no sample.</summary>
    public static IReadOnlyList<SampleFeature> Features { get; } =
    [
        Feature("Predicates with arguments", @"\b\w+\(\s*\w+\s*:"),
        Feature("AND", @"\bAND\b"),
        Feature("OR", @"\bOR\b"),
        Feature("NOT", @"\bNOT\b"),
        Feature("XOR", @"\bXOR\b"),
        Feature("EQUIVALENT", @"\bEQUIVALENT\b"),
        Feature("IMPLIES", @"\bIMPLIES\b"),
        Feature("NAND", @"\bNAND\b"),
        Feature("NOR", @"\bNOR\b"),
        Feature("PARITY", @"\bPARITY\("),
        Feature("AtLeast", @"\bAtLeast\("),
        Feature("AtMost", @"\bAtMost\("),
        Feature("Exactly", @"\bExactly\("),
        Feature("ExactlyOne", @"\bExactlyOne\("),
        Feature("ANY", @"\bANY\("),
        Feature("ALL", @"\bALL\("),
        Feature("NONE", @"\bNONE\("),
        Feature("BETWEEN", @"\bBETWEEN\("),
        Feature("If", @"\bIf\("),
        Feature("IsTrue", @"\bIsTrue\("),
        Feature("IsUnknown", @"\bIsUnknown\("),
        Feature("COALESCE", @"\bCOALESCE\("),
        Feature("let definitions", @"(?m)^let\s"),
        Feature("from(...) variables", @"\bfrom\("),
    ];

    /// <summary>Gets the samples in the order of the menu.</summary>
    public static IReadOnlyList<SampleRule> All { get; } =
    [
        new()
        {
            Name = "Predicates with arguments",
            Description = "A predicate takes named arguments: text, numbers and booleans.",
            Text = """
                hasRole(role: "editor") // the argument is text
                AND ageAtLeast(years: 18) // a number
                AND inRegion(region: "EU", strict: true) // several arguments, one of them a boolean
                """,
        },
        new()
        {
            Name = "AND, OR and NOT",
            Description = "The basic connectives; Unknown stays Unknown unless another operand decides.",
            Text = """
                // AND is False when any operand is False, and True only when all are True.
                isMember AND (hasCoupon OR hasGiftCard) // OR is True when any operand is True
                AND NOT isBanned // NOT turns True to False and False to True, and keeps Unknown
                """,
        },
        new()
        {
            Name = "XOR and EQUIVALENT",
            Description = "Exclusive or, and two values that must agree.",
            Text = """
                (isWeekday XOR isWeekend) // exactly one of the two is True; Unknown if either is Unknown
                AND (isOnline EQUIVALENT hasSession) // both True or both False
                """,
        },
        new()
        {
            Name = "IMPLIES",
            Description = "If the first is True, the second must be True too.",
            Text = """
                // False only when isPremium is True and isPaid is False; True whenever isPremium is False.
                (isPremium IMPLIES isPaid)
                """,
        },
        new()
        {
            Name = "NAND and NOR",
            Description = "Negated AND and negated OR.",
            Text = """
                (isOpen NAND isFull) // False only when both are True
                AND (isClosed NOR isHidden) // True only when both are False
                """,
        },
        new()
        {
            Name = "PARITY",
            Description = "True when an odd number of the operands are True.",
            Text = """
                // Unknown whenever one operand is Unknown, because it can flip the count.
                PARITY(smoke, fire, heat)
                """,
        },
        new()
        {
            Name = "AtLeast, AtMost and Exactly",
            Description = "Count the True operands against a number.",
            Text = """
                // The first argument is the count k; the operands follow.
                AtLeast(2, hasPassword, hasPhone, hasToken) // at least two factors
                AND AtMost(1, isGuest, isTrial, isExpired) // at most one of these states
                AND Exactly(1, byEmail, bySms) // exactly one channel
                """,
        },
        new()
        {
            Name = "ExactlyOne",
            Description = "Exactly one operand is True; the short form of Exactly(1, ...).",
            Text = """
                // The same as Exactly(1, ...). The result is Unknown while an Unknown operand could change the count.
                ExactlyOne(isRed, isAmber, isGreen)
                """,
        },
        new()
        {
            Name = "ANY, ALL and NONE",
            Description = "Some, every or no operand is True.",
            Text = """
                ANY(isOwner, isEditor) // at least one is True
                AND ALL(hasEmail, hasPhone) // every one is True
                AND NONE(isBanned, isSuspended) // no one is True
                """,
        },
        new()
        {
            Name = "BETWEEN",
            Description = "The number of True operands lies inside a range.",
            Text = """
                // BETWEEN(min, max, operands...): two to three of the four checks must pass.
                BETWEEN(2, 3, checkA, checkB, checkC, checkD)
                """,
        },
        new()
        {
            Name = "If",
            Description = "Pick one of two results by a condition.",
            Text = """
                // If(condition, then, else). The condition True gives the second operand, False the third.
                If(isMember, hasMemberPrice, hasPublicPrice)
                """,
        },
        new()
        {
            Name = "IsTrue and IsUnknown",
            Description = "Ask about the value itself, so Unknown becomes a plain True or False.",
            Text = """
                // IsTrue is True only when the operand is True. IsUnknown is True only when it is Unknown.
                IsTrue(isVerified) AND IsUnknown(riskCheck) // verified, but the risk check has not answered
                """,
        },
        new()
        {
            Name = "COALESCE",
            Description = "Take the first operand that is not Unknown.",
            Text = """
                // Falls back from the user's choice to the team default, then to False.
                COALESCE(userSetting, teamSetting, False)
                """,
        },
        new()
        {
            Name = "let definitions",
            Description = "Name a sub-expression once and use it with arguments.",
            Text = """
                // A definition has parameters; each use is replaced by its body when the rule compiles.
                let bothApprove(first, second) = first AND second;
                bothApprove(approvedByAlice, approvedByBob) // the use passes two arguments
                AND NOT approvedByCarol // a plain operand keeps its note
                """,
        },
        new()
        {
            Name = "from(...) variables",
            Description = "Read an argument from a named source of the request instead of writing it in the rule.",
            Text = """
                // from("source", "query") takes the value from the data source when the rule runs.
                hasRole(role: from("user", "$.role")) // the role of the user
                AND inRegion(region: from("request", "$.region")) // the region of the request
                """,
        },
        new()
        {
            Name = "Notes",
            Description = "Comments become the notes of nodes and stay with the rule when you convert it.",
            Text = """
                /* A block note can sit in front of a node. */
                (isOpen AND hasStock) // A line note ends at the end of the line.
                OR /* A note works in the middle of a rule too. */ isPreorder
                """,
        },
    ];

    private static SampleFeature Feature(string name, string pattern)
    {
        return new SampleFeature(
            name,
            new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout)
        );
    }
}
