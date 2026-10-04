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
        "collection-setequals",
        "regex-matches",
        "selectedvalue-create",
        "string-contains",
        "string-endswith",
        "string-equals",
        "string-equalsconfigurable",
        "string-equalsignorecase",
        "string-isnullorempty",
        "string-startswith",
    ];
}
