namespace TruthWeaver.Tool.Core;

/// <summary>
/// The <see cref="Diagnostics.Diagnostic.Code"/> values that the tool core reports for a problem outside the rule itself: a
/// file it cannot read, a document it cannot parse, a path that selects nothing, a configuration value it cannot use. Every
/// code has the <c>TWT</c> prefix ("TruthWeaver tool") and a four-digit number. The compiler's own codes keep their
/// <c>TRE</c> prefix and are passed through unchanged.
/// </summary>
public static class ToolDiagnosticCodes
{
    /// <summary>The input file or stream cannot be read: it does not exist, access is denied, or an I/O error occurred.</summary>
    public const string InputUnreadable = "TWT0001";

    /// <summary>The input bytes are not valid in the encoding that the byte order mark names, or in UTF-8 when there is no mark.</summary>
    public const string InvalidEncoding = "TWT0002";

    /// <summary>A JSON or YAML document is not well formed, so the core cannot read a rule, a configuration, stub values or data from it.</summary>
    public const string MalformedDocument = "TWT0003";

    /// <summary>An embedded-rule path has a syntax error.</summary>
    public const string InvalidPath = "TWT0004";

    /// <summary>An embedded-rule path selects nothing in the document.</summary>
    public const string PathNotFound = "TWT0005";

    /// <summary>
    /// The value that an embedded-rule path selects is not a rule (a rule is a JSON object, a YAML mapping, or a string of
    /// rule text), or the input is rule text, which has no paths.
    /// </summary>
    public const string NotARule = "TWT0006";

    /// <summary>A <c>truthweaver.json</c> value has the wrong type or is out of range.</summary>
    public const string InvalidConfiguration = "TWT0007";

    /// <summary>A <c>truthweaver.json</c> property is not known. The core ignores it and reports this warning.</summary>
    public const string UnknownConfigurationProperty = "TWT0008";

    /// <summary>A stub-value entry is not usable: the key is not exactly one term, the value is not a truth value, or two keys name the same term.</summary>
    public const string InvalidStubValue = "TWT0009";

    /// <summary>An output file cannot be written or compared because of an I/O error.</summary>
    public const string OutputFailed = "TWT0010";

    /// <summary>Two data files have the same data source name.</summary>
    public const string DuplicateDataSource = "TWT0011";

    /// <summary>
    /// The predicate schema file is not usable: it is not well formed, a predicate or an argument is not described
    /// correctly, or an argument kind is not known.
    /// </summary>
    public const string InvalidSchemaFile = "TWT0012";

    /// <summary>A JSON or YAML rule does not match the JSON schema of the rule tree, <c>rule-tree.schema.json</c>.</summary>
    public const string TreeSchemaViolation = "TWT0013";

    /// <summary>
    /// The conversion self-check failed: the rule, converted to rule text, JSON or YAML, does not compile to the same tree
    /// with the same notes.
    /// </summary>
    public const string ConversionMismatch = "TWT0014";

    /// <summary>
    /// An export of every artifact left out one artifact, because the rule cannot give it with these options: a truth
    /// table over the term cap. The manifest lists the artifact as skipped, with the reason.
    /// </summary>
    public const string ArtifactSkipped = "TWT0015";

    /// <summary>
    /// The external program that renders a diagram as an image (Graphviz <c>dot</c> or the Mermaid CLI <c>mmdc</c>) is not on
    /// the search path. The tools have no renderer of their own.
    /// </summary>
    public const string RendererNotFound = "TWT0016";

    /// <summary>
    /// The external program that renders a diagram as an image did not finish, stopped with a non-zero exit code, or wrote no
    /// image file.
    /// </summary>
    public const string RenderFailed = "TWT0017";

    /// <summary>An artifact id is not in the artifact catalog.</summary>
    public const string UnknownArtifact = "TWT0018";

    /// <summary>
    /// A patch write-back wrote one rule again in full, because the edit did not fit the rule's values in place (a node, key
    /// or operand was added or removed, or a changed value is a YAML block scalar, alias, anchor or tag) or the rule is rule
    /// text. The text outside the rule is unchanged; comments and layout inside the rule are not kept.
    /// </summary>
    public const string RuleRewritten = "TWT0019";

    /// <summary>
    /// A round-trip write-back serialized the whole document again. YAML comments and the layout of the document can be
    /// lost.
    /// </summary>
    public const string RoundTripWriteBack = "TWT0020";

    /// <summary>
    /// An edited rule cannot be written back into its document: the written document does not read back as the edited rule,
    /// or the rule is shared through a YAML anchor or alias. The document is not changed.
    /// </summary>
    public const string WriteBackFailed = "TWT0021";

    /// <summary>
    /// A rule test file is not usable: it is not well-formed YAML, a key is missing or not known, a value has the wrong
    /// type, or a rule file or data file cannot be read.
    /// </summary>
    public const string InvalidRuleTestFile = "TWT0022";
}
