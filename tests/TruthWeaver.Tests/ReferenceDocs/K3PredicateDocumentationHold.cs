namespace TruthWeaver.Tests.ReferenceDocs;

/// <summary>
/// The explicit, committed list of built-in predicates that have no reference document yet. The sync check
/// (<see cref="K3ReferenceSyncChecker"/>) reports a predicate that is neither documented nor listed here, and reports an
/// entry here that has a document or names no predicate. Releasing the hold (reference ticket 13) documents each predicate
/// and removes its entry, which empties this list.
/// </summary>
/// <remarks>
/// An entry is the predicate's document stem: the factory class name without <c>Predicates</c>, a hyphen and the factory
/// method name, all lower-case. The document would be <c>docs/strong-k3/predicates/&lt;stem&gt;.md</c>.
/// </remarks>
internal static class K3PredicateDocumentationHold
{
    internal static readonly IReadOnlyList<string> Stems =
    [
        "collection-contains",
        "collection-containsall",
        "collection-containsany",
        "collection-countequal",
        "collection-countgreaterthan",
        "collection-countgreaterthanorequal",
        "collection-countlessthan",
        "collection-countlessthanorequal",
        "collection-in",
        "collection-isempty",
        "collection-isnotempty",
        "collection-isnotsubsetof",
        "collection-issubsetof",
        "collection-notcontains",
        "collection-notcontainsall",
        "collection-notcontainsany",
        "collection-notcountequal",
        "collection-notcountgreaterthan",
        "collection-notcountgreaterthanorequal",
        "collection-notcountlessthan",
        "collection-notcountlessthanorequal",
        "collection-notin",
        "collection-setequals",
        "datetime-after",
        "datetime-afternow",
        "datetime-before",
        "datetime-beforenow",
        "datetime-between",
        "datetime-notafter",
        "datetime-notafternow",
        "datetime-notbefore",
        "datetime-notbeforenow",
        "datetime-outside",
        "numeric-between",
        "numeric-equal",
        "numeric-greaterthan",
        "numeric-greaterthanorequal",
        "numeric-in",
        "numeric-isdefault",
        "numeric-isnotdefault",
        "numeric-isnotnull",
        "numeric-isnull",
        "numeric-lessthan",
        "numeric-lessthanorequal",
        "numeric-notequal",
        "numeric-notin",
        "numeric-outside",
        "regex-matches",
        "regex-notmatches",
        "scalar-equal",
        "scalar-in",
        "scalar-isdefault",
        "scalar-isnotdefault",
        "scalar-isnotnull",
        "scalar-isnull",
        "scalar-notequal",
        "scalar-notin",
        "selectedvalue-create",
        "string-contains",
        "string-endswith",
        "string-equals",
        "string-equalsconfigurable",
        "string-equalsignorecase",
        "string-isempty",
        "string-isnotempty",
        "string-isnotnullorempty",
        "string-isnotnullorwhitespace",
        "string-isnullorempty",
        "string-isnullorwhitespace",
        "string-notcontains",
        "string-notequal",
        "string-startswith",
    ];
}
