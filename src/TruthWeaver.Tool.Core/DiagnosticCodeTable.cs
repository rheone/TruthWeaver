namespace TruthWeaver.Tool.Core;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Diagnostics;

/// <summary>
/// The table of every diagnostic code that the tools can report: the compiler's <c>TRE</c> codes
/// (<see cref="DiagnosticCodes"/>) and the tool core's <c>TWT</c> codes (<see cref="ToolDiagnosticCodes"/>). It is the
/// source of the rule ids and rule descriptions in SARIF output, so every code that a result names has a rule.
/// </summary>
/// <remarks>
/// The rows match the code tables of the documentation: the diagnostic code catalog for the <c>TRE</c> codes and the
/// tool core page for the <c>TWT</c> codes. A test checks the rows against both tables and against the constants.
/// </remarks>
public static class DiagnosticCodeTable
{
    // The rows in number order; ByCode is declared after it so that it is initialized after it.
    private static readonly ImmutableArray<DiagnosticCodeInfo> Rows =
    [
        Error(DiagnosticCodes.SyntaxError, nameof(DiagnosticCodes.SyntaxError), "The rule text does not parse."),
        Error(
            DiagnosticCodes.UnknownPredicate,
            nameof(DiagnosticCodes.UnknownPredicate),
            "A term names a predicate that is not registered, or a node or a call names an operation that does not exist."
        ),
        Error(
            DiagnosticCodes.MissingArgument,
            nameof(DiagnosticCodes.MissingArgument),
            "A term omits a required argument of its predicate."
        ),
        Error(
            DiagnosticCodes.ArgumentTypeMismatch,
            nameof(DiagnosticCodes.ArgumentTypeMismatch),
            "An argument value has the wrong kind for the predicate."
        ),
        Error(
            DiagnosticCodes.UnknownArgument,
            nameof(DiagnosticCodes.UnknownArgument),
            "A term names an argument that the predicate does not declare."
        ),
        Error(
            DiagnosticCodes.InfixArityViolation,
            nameof(DiagnosticCodes.InfixArityViolation),
            "An operation has an operand count that it does not accept."
        ),
        Error(
            DiagnosticCodes.AmbiguousOperatorMixing,
            nameof(DiagnosticCodes.AmbiguousOperatorMixing),
            "Operators share a level without parentheses where the grouping is ambiguous."
        ),
        Error(
            DiagnosticCodes.InvalidThresholdValue,
            nameof(DiagnosticCodes.InvalidThresholdValue),
            "A threshold or the bounds of BETWEEN are not whole numbers, or make the result a constant."
        ),
        Error(
            DiagnosticCodes.MaxDepthExceeded,
            nameof(DiagnosticCodes.MaxDepthExceeded),
            "The expression tree is deeper than the depth limit."
        ),
        Error(
            DiagnosticCodes.MaxNodeCountExceeded,
            nameof(DiagnosticCodes.MaxNodeCountExceeded),
            "The expression tree has more nodes than the node limit."
        ),
        Info(
            DiagnosticCodes.AnalysisSkippedTooManyTerms,
            nameof(DiagnosticCodes.AnalysisSkippedTooManyTerms),
            "The analysis was skipped because the rule has more terms than the analysis limit."
        ),
        Warning(
            DiagnosticCodes.StructuralTautology,
            nameof(DiagnosticCodes.StructuralTautology),
            "A sub-expression is True for every assignment of its terms."
        ),
        Warning(
            DiagnosticCodes.StructuralContradiction,
            nameof(DiagnosticCodes.StructuralContradiction),
            "A sub-expression is False for every assignment of its terms."
        ),
        Error(
            DiagnosticCodes.MalformedTree,
            nameof(DiagnosticCodes.MalformedTree),
            "The shape of a JSON or YAML tree is wrong."
        ),
        Error(
            DiagnosticCodes.InvalidEscapeSequence,
            nameof(DiagnosticCodes.InvalidEscapeSequence),
            "A string literal contains a backslash escape that is not supported."
        ),
        Error(
            DiagnosticCodes.RewriteTooLarge,
            nameof(DiagnosticCodes.RewriteTooLarge),
            "An expanding rewrite would produce more nodes than the rewrite limit."
        ),
        Info(
            DiagnosticCodes.RedundantInspection,
            nameof(DiagnosticCodes.RedundantInspection),
            "An inspection has an operand that can never be Unknown, or never be known."
        ),
        Info(
            DiagnosticCodes.RedundantCoalesce,
            nameof(DiagnosticCodes.RedundantCoalesce),
            "A COALESCE operand can never be Unknown, so the operands after it are never reached."
        ),
        Info(
            DiagnosticCodes.ConstantIfCondition,
            nameof(DiagnosticCodes.ConstantIfCondition),
            "The condition of an If is always True or always False."
        ),
        Info(DiagnosticCodes.IdenticalIfBranches, nameof(DiagnosticCodes.IdenticalIfBranches), "An If has two equal branches."),
        Info(
            DiagnosticCodes.VacuousCardinality,
            nameof(DiagnosticCodes.VacuousCardinality),
            "The constant operands of a threshold or BETWEEN fix its value."
        ),
        Info(
            DiagnosticCodes.DuplicateOperands,
            nameof(DiagnosticCodes.DuplicateOperands),
            "An operand repeats where the repeat adds nothing."
        ),
        Info(DiagnosticCodes.DoubleNegation, nameof(DiagnosticCodes.DoubleNegation), "NOT (NOT x), which is x."),
        Error(
            DiagnosticCodes.UndeclaredDataSource,
            nameof(DiagnosticCodes.UndeclaredDataSource),
            "A variable reference names a data source that is not declared."
        ),
        Error(
            DiagnosticCodes.MalformedDataQuery,
            nameof(DiagnosticCodes.MalformedDataQuery),
            "The query of a variable reference is not valid for its data source."
        ),
        Error(
            DiagnosticCodes.InvalidArgumentValue,
            nameof(DiagnosticCodes.InvalidArgumentValue),
            "The literal argument values of a predicate call break a rule that the predicate checks."
        ),
        Warning(
            DiagnosticCodes.DeprecatedPredicate,
            nameof(DiagnosticCodes.DeprecatedPredicate),
            "A rule uses a deprecated predicate."
        ),
        Info(DiagnosticCodes.DeepNesting, nameof(DiagnosticCodes.DeepNesting), "The rule is close to the depth limit."),
        Info(
            DiagnosticCodes.WideChain,
            nameof(DiagnosticCodes.WideChain),
            "An AND or OR has more operands than the wide-chain limit."
        ),
        Info(DiagnosticCodes.NotCanonical, nameof(DiagnosticCodes.NotCanonical), "Canonicalize() would change the rule."),
        Warning(
            DiagnosticCodes.ThresholdKeptAsAtom,
            nameof(DiagnosticCodes.ThresholdKeptAsAtom),
            "A normal form kept a threshold as an atom."
        ),
        Error(
            DiagnosticCodes.DuplicateArgument,
            nameof(DiagnosticCodes.DuplicateArgument),
            "A term names the same argument more than once."
        ),
        Warning(
            DiagnosticCodes.RewriteNotConverged,
            nameof(DiagnosticCodes.RewriteNotConverged),
            "A rewrite stopped at its pass limit before the rule stopped changing."
        ),
        Warning(
            DiagnosticCodes.TruthTableCapRaised,
            nameof(DiagnosticCodes.TruthTableCapRaised),
            "A truth table has more terms than the default cap."
        ),
        Error(
            DiagnosticCodes.TruthTableTooLarge,
            nameof(DiagnosticCodes.TruthTableTooLarge),
            "A truth table was refused because the rule has too many terms."
        ),
        Error(
            DiagnosticCodes.DefinitionCycleDetected,
            nameof(DiagnosticCodes.DefinitionCycleDetected),
            "A let definition references itself, directly or through a chain of other definitions."
        ),
        Error(
            DiagnosticCodes.DefinitionNameCollision,
            nameof(DiagnosticCodes.DefinitionNameCollision),
            "A let definition's name clashes with another definition, a reserved word, or a predicate name."
        ),
        Error(
            DiagnosticCodes.MaxDefinitionDepthExceeded,
            nameof(DiagnosticCodes.MaxDefinitionDepthExceeded),
            "A use would expand through more let definitions than the definition-depth limit."
        ),
        Error(
            DiagnosticCodes.DefinitionArgumentCountMismatch,
            nameof(DiagnosticCodes.DefinitionArgumentCountMismatch),
            "A use of a let definition supplies the wrong number of arguments."
        ),
        Error(
            DiagnosticCodes.UnknownDefinition,
            nameof(DiagnosticCodes.UnknownDefinition),
            "A use names a let definition that was never declared."
        ),
        Info(
            DiagnosticCodes.UnusedDefinition,
            nameof(DiagnosticCodes.UnusedDefinition),
            "A let definition is declared but never used."
        ),
        Error(
            DiagnosticCodes.UnknownCollection,
            nameof(DiagnosticCodes.UnknownCollection),
            "A FORALL/EXISTS head names a collection that is not registered."
        ),
        Error(
            DiagnosticCodes.MaxQuantifierDepthExceeded,
            nameof(DiagnosticCodes.MaxQuantifierDepthExceeded),
            "A rule nests quantifiers deeper than the compiler option MaxQuantifierDepth allows."
        ),
        Error(
            DiagnosticCodes.ShadowedVariable,
            nameof(DiagnosticCodes.ShadowedVariable),
            "A nested quantifier reuses the bound variable name of an enclosing quantifier."
        ),
        Error(ToolDiagnosticCodes.InputUnreadable, nameof(ToolDiagnosticCodes.InputUnreadable), "The input cannot be read."),
        Error(
            ToolDiagnosticCodes.InvalidEncoding,
            nameof(ToolDiagnosticCodes.InvalidEncoding),
            "A byte is not valid in the encoding of the input."
        ),
        Error(
            ToolDiagnosticCodes.MalformedDocument,
            nameof(ToolDiagnosticCodes.MalformedDocument),
            "A JSON or YAML document is not well formed."
        ),
        Error(
            ToolDiagnosticCodes.InvalidPath,
            nameof(ToolDiagnosticCodes.InvalidPath),
            "An embedded-rule path has a syntax error."
        ),
        Error(
            ToolDiagnosticCodes.PathNotFound,
            nameof(ToolDiagnosticCodes.PathNotFound),
            "An embedded-rule path selects nothing."
        ),
        Error(
            ToolDiagnosticCodes.NotARule,
            nameof(ToolDiagnosticCodes.NotARule),
            "The selected value is not a rule, or the input is rule text and has no paths."
        ),
        Error(
            ToolDiagnosticCodes.InvalidConfiguration,
            nameof(ToolDiagnosticCodes.InvalidConfiguration),
            "A configuration value has the wrong type or is out of range."
        ),
        Warning(
            ToolDiagnosticCodes.UnknownConfigurationProperty,
            nameof(ToolDiagnosticCodes.UnknownConfigurationProperty),
            "A configuration property is not known."
        ),
        Error(
            ToolDiagnosticCodes.InvalidStubValue,
            nameof(ToolDiagnosticCodes.InvalidStubValue),
            "A stub-value entry is not usable."
        ),
        Error(
            ToolDiagnosticCodes.OutputFailed,
            nameof(ToolDiagnosticCodes.OutputFailed),
            "An output file cannot be written or read."
        ),
        Error(
            ToolDiagnosticCodes.DuplicateDataSource,
            nameof(ToolDiagnosticCodes.DuplicateDataSource),
            "Two data files have the same source name."
        ),
        Error(
            ToolDiagnosticCodes.InvalidSchemaFile,
            nameof(ToolDiagnosticCodes.InvalidSchemaFile),
            "The predicate schema file is not usable."
        ),
        Error(
            ToolDiagnosticCodes.TreeSchemaViolation,
            nameof(ToolDiagnosticCodes.TreeSchemaViolation),
            "A JSON or YAML rule does not match the JSON schema of the rule tree."
        ),
        Error(
            ToolDiagnosticCodes.ConversionMismatch,
            nameof(ToolDiagnosticCodes.ConversionMismatch),
            "A conversion of the rule to another format does not compile to the same tree."
        ),
        Warning(
            ToolDiagnosticCodes.ArtifactSkipped,
            nameof(ToolDiagnosticCodes.ArtifactSkipped),
            "An export of every artifact left out an artifact that the rule cannot give."
        ),
        Error(
            ToolDiagnosticCodes.RendererNotFound,
            nameof(ToolDiagnosticCodes.RendererNotFound),
            "The external program that renders a diagram as an image is not on the search path."
        ),
        Error(
            ToolDiagnosticCodes.RenderFailed,
            nameof(ToolDiagnosticCodes.RenderFailed),
            "The external program that renders a diagram as an image failed."
        ),
        Error(
            ToolDiagnosticCodes.UnknownArtifact,
            nameof(ToolDiagnosticCodes.UnknownArtifact),
            "An artifact id is not in the artifact catalog."
        ),
        Warning(
            ToolDiagnosticCodes.RuleRewritten,
            nameof(ToolDiagnosticCodes.RuleRewritten),
            "A patch write-back wrote one rule again in full; comments and layout inside the rule are not kept."
        ),
        Warning(
            ToolDiagnosticCodes.RoundTripWriteBack,
            nameof(ToolDiagnosticCodes.RoundTripWriteBack),
            "A round-trip write-back serialized the whole document again; YAML comments and layout can be lost."
        ),
        Error(
            ToolDiagnosticCodes.WriteBackFailed,
            nameof(ToolDiagnosticCodes.WriteBackFailed),
            "An edited rule cannot be written back into its document."
        ),
        Error(
            ToolDiagnosticCodes.InvalidRuleTestFile,
            nameof(ToolDiagnosticCodes.InvalidRuleTestFile),
            "A rule test file is not usable."
        ),
        Error(
            ToolDiagnosticCodes.UnsupportedJsonLogic,
            nameof(ToolDiagnosticCodes.UnsupportedJsonLogic),
            "A JsonLogic document uses an operator or a shape that the importer cannot map."
        ),
        Warning(
            ToolDiagnosticCodes.JsonLogicBehaviorDiffers,
            nameof(ToolDiagnosticCodes.JsonLogicBehaviorDiffers),
            "An imported JsonLogic construct can give a different answer in TruthWeaver."
        ),
    ];

    private static readonly FrozenDictionary<string, DiagnosticCodeInfo> ByCode = Rows.ToFrozenDictionary(
        row => row.Code,
        StringComparer.Ordinal
    );

    /// <summary>Gets every row, the <c>TRE</c> codes and then the <c>TWT</c> codes, each in number order.</summary>
    public static IReadOnlyList<DiagnosticCodeInfo> All => Rows;

    /// <summary>Gets the row of a code.</summary>
    /// <param name="code">The code, such as <c>TRE0001</c>.</param>
    /// <param name="info">The row, or <see langword="null"/> when the table has no row for the code.</param>
    /// <returns><see langword="true"/> when the table has a row for the code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    public static bool TryGet(string code, [NotNullWhen(true)] out DiagnosticCodeInfo? info)
    {
        ArgumentNullException.ThrowIfNull(code);

        return ByCode.TryGetValue(code, out info);
    }

    private static DiagnosticCodeInfo Error(string code, string name, string description)
    {
        return new DiagnosticCodeInfo(code, name, DiagnosticSeverity.Error, description);
    }

    private static DiagnosticCodeInfo Warning(string code, string name, string description)
    {
        return new DiagnosticCodeInfo(code, name, DiagnosticSeverity.Warning, description);
    }

    private static DiagnosticCodeInfo Info(string code, string name, string description)
    {
        return new DiagnosticCodeInfo(code, name, DiagnosticSeverity.Info, description);
    }
}
