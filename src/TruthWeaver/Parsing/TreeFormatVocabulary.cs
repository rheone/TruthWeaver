namespace TruthWeaver.Parsing;

/// <summary>
/// The words a tree format uses for its own constructs, so one reader can phrase diagnostics the way each format's authors
/// expect ("a JSON object" versus "a YAML mapping") without the reader knowing which format it is reading.
/// </summary>
/// <param name="MappingNoun">The format's keyed collection, without an article: <c>JSON object</c>, <c>YAML mapping</c>.</param>
/// <param name="SequenceNoun">The format's ordered collection, without an article: <c>array</c>, <c>sequence</c>.</param>
/// <param name="StringNoun">The format's string value, without an article: <c>JSON string</c>, <c>YAML string</c>.</param>
/// <param name="KeyNoun">What a mapping entry is called in "no 'k' ..." messages: <c>property</c>, <c>key</c>.</param>
/// <param name="ConstMessage">The full message for an invalid <c>const</c> value.</param>
/// <param name="ConstExpected">The <c>Expected</c> text for an invalid <c>const</c> value.</param>
/// <param name="LiteralExpected">The <c>Expected</c> text for a predicate argument that cannot be a literal.</param>
internal sealed record TreeFormatVocabulary(
    string MappingNoun,
    string SequenceNoun,
    string StringNoun,
    string KeyNoun,
    string ConstMessage,
    string ConstExpected,
    string LiteralExpected = "a string, number, boolean or array"
);
