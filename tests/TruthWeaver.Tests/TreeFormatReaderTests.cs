namespace TruthWeaver.Tests;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// The shared tree-format reader (<see cref="TreeFormatReader"/>) exercised through the
/// <see cref="ITreeNodeCursor"/> seam with a trivial in-memory cursor, so the dispatch, op-name mapping and validation
/// rules are tested without any JSON or YAML document model.
/// </summary>
public sealed class TreeFormatReaderTests
{
    /// <summary>The entry key that stands in for a key that is not a string.</summary>
    private const string NonStringKey = "<non-string key>";

    private static readonly TreeFormatVocabulary Words = new(
        MappingNoun: "map",
        SequenceNoun: "list",
        StringNoun: "text",
        KeyNoun: "key",
        ConstMessage: "'const' must be true, false or unknown.",
        ConstExpected: "true, false or unknown"
    );

    /// <summary>
    /// Verifies that a node with a <c>const</c> key becomes a constant node, and that the node records the root path.
    /// </summary>
    [Fact]
    public void Read_ConstNode_ProducesConstantNodeAtRootPath_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Map(("const", FakeNode.Constant(TruthValue.Unknown)));

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Empty(diagnostics);
        ConstantNode constant = Assert.IsType<ConstantNode>(node);
        Assert.Equal(TruthValue.Unknown, constant.Value);
        Assert.Equal("$", constant.Path);
    }

    /// <summary>
    /// Verifies that a <c>predicate</c> node carries its name and its arguments, in document order.
    /// </summary>
    [Fact]
    public void Read_PredicateNodeWithArgs_ProducesTermWithArgumentsInOrder_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Map(
            ("predicate", FakeNode.Text("isAdult")),
            ("args", FakeNode.Map(("minAge", FakeNode.Number(18)), ("region", FakeNode.Text("EU"))))
        );

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Empty(diagnostics);
        TermNode term = Assert.IsType<TermNode>(node);
        Assert.Equal("isAdult", term.PredicateName);
        Assert.Equal(["minAge", "region"], term.Arguments.Select(a => a.Name));
        Assert.Equal("$.args.minAge", term.Arguments[0].Path);
    }

    /// <summary>
    /// Verifies that an <c>op</c> node maps its tree-format name to the matching operator node, with each operand
    /// located by its index path.
    /// </summary>
    [Fact]
    public void Read_OperatorNode_ProducesOperatorWithIndexedOperandPaths_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Op("and", FakeNode.Predicate("a"), FakeNode.Predicate("b"));

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Empty(diagnostics);
        AndNode and = Assert.IsType<AndNode>(node);
        Assert.Equal(["$.operands[0]", "$.operands[1]"], and.Operands.Select(o => o.Path));
    }

    /// <summary>
    /// Verifies that <c>not</c> with the wrong operand count is reported at the operands path, using the operand-count wording.
    /// </summary>
    [Fact]
    public void Read_NotWithTwoOperands_ReportsArityDiagnosticAtOperandsPath_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Op("not", FakeNode.Predicate("a"), FakeNode.Predicate("b"));

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Null(node);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Equal("$.operands", diagnostic.Path);
        Assert.Equal("1 operand", diagnostic.Expected);
        Assert.Equal("2 operands", diagnostic.Found);
    }

    /// <summary>
    /// Verifies that a threshold operator reads its integer <c>k</c>, and that a missing <c>k</c> is reported on the
    /// node itself using the format's word for a key.
    /// </summary>
    [Fact]
    public void Read_ThresholdWithoutK_ReportsMissingKeyUsingTheFormatsWords_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Op("atLeast", FakeNode.Predicate("a"));

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Null(node);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("no 'k' key", diagnostic.Found);
    }

    /// <summary>
    /// Verifies that <c>between</c> reads its integer bounds.
    /// </summary>
    [Fact]
    public void Read_BetweenWithBounds_ProducesBetweenNode_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Map(
            ("op", FakeNode.Text("between")),
            ("operands", FakeNode.Seq(FakeNode.Predicate("a"))),
            ("min", FakeNode.Number(1)),
            ("max", FakeNode.Number(2))
        );

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Empty(diagnostics);
        BetweenNode between = Assert.IsType<BetweenNode>(node);
        Assert.Equal((1, 2), (between.Min, between.Max));
    }

    /// <summary>
    /// Verifies that the retired <c>nxor</c> spelling is rejected with the dedicated diagnostic and a <c>parity</c> suggestion.
    /// </summary>
    [Fact]
    public void Read_RetiredNxorOperator_IsRejectedWithParitySuggestion_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Op("NXOR", FakeNode.Predicate("a"));

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Null(node);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NXOR", diagnostic.Found);
        Assert.Equal("parity", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// Verifies that an unknown operator name is reported at the <c>op</c> property, before its operands are examined.
    /// </summary>
    [Fact]
    public void Read_UnknownOperator_ReportsAtOpPathWithoutReadingOperands_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Map(("op", FakeNode.Text("andd")));

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Null(node);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("$.op", diagnostic.Path);
        Assert.Equal("'andd'", diagnostic.Found);
    }

    /// <summary>
    /// Verifies that a node that is not a mapping is reported with the format's own noun and kind name.
    /// </summary>
    [Fact]
    public void Read_NonMappingNode_ReportsExpectedMappingUsingTheFormatsWords_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Text("oops");

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Null(node);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Expected a map node but found scalar.", diagnostic.Message);
        Assert.Equal("a map", diagnostic.Expected);
    }

    /// <summary>
    /// Verifies that an argument whose key is not a string (possible in YAML) is reported at the key, in the format's words.
    /// </summary>
    [Fact]
    public void Read_PredicateArgWithNonStringKey_ReportsArgumentNameAtTheKey_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Map(
            ("predicate", FakeNode.Text("isAdult")),
            ("args", FakeNode.Map(("minAge", FakeNode.Number(18)), (NonStringKey, FakeNode.Text("EU"))))
        );

        // Act
        (RuleNode? node, IReadOnlyList<Diagnostic> diagnostics) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Null(node);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("An argument name must be a text.", diagnostic.Message);
        Assert.Equal("a text", diagnostic.Expected);
        Assert.Equal("a scalar", diagnostic.Found);
        Assert.Equal("$.args", diagnostic.Path);
    }

    /// <summary>
    /// Verifies that the node span the cursor reports is recorded on the parsed node, which is how a position-aware
    /// format keeps its spans.
    /// </summary>
    [Fact]
    public void Read_CursorWithSpan_RecordsSpanOnTheParsedNode_Test()
    {
        // Arrange
        FakeNode root = FakeNode.Op("or", FakeNode.Predicate("a")) with
        {
            Span = new SourceSpan(3, 9),
        };

        // Act
        (RuleNode? node, _) = TreeFormatReader.Read(root, Words);

        // Assert
        Assert.Equal(new SourceSpan(3, 9), node?.Span);
    }

    /// <summary>A minimal in-memory node: just enough structure to drive the reader without a document model.</summary>
    private sealed record FakeNode(
        TreeNodeShape Shape,
        string? StringValue = null,
        int? Integer = null,
        TruthValue? Truth = null,
        IReadOnlyList<KeyValuePair<string, FakeNode>>? Entries = null,
        IReadOnlyList<FakeNode>? Items = null
    ) : ITreeNodeCursor
    {
        public SourceSpan Span { get; init; } = SourceSpan.None;

        public string KindName => this.Shape.ToString().ToLowerInvariant();

        public IEnumerable<ITreeNodeCursor> Elements => this.Items ?? [];

        public IEnumerable<TreeMember> Members =>
            (this.Entries ?? []).Select(e =>
                e.Key == NonStringKey ? new TreeMember(null, e.Value, Text("key")) : new TreeMember(e.Key, e.Value)
            );

        public string UnsupportedLiteralMessage => "Unsupported literal.";

        public static FakeNode Map(params (string Key, FakeNode Value)[] entries)
        {
            return new(TreeNodeShape.Mapping, Entries: [.. entries.Select(e => KeyValuePair.Create(e.Key, e.Value))]);
        }

        public static FakeNode Seq(params FakeNode[] items)
        {
            return new(TreeNodeShape.Sequence, Items: items);
        }

        public static FakeNode Text(string text)
        {
            return new(TreeNodeShape.Scalar, StringValue: text);
        }

        public static FakeNode Number(int value)
        {
            return new(TreeNodeShape.Scalar, Integer: value);
        }

        public static FakeNode Constant(TruthValue value)
        {
            return new(TreeNodeShape.Scalar, Truth: value);
        }

        public static FakeNode Predicate(string name)
        {
            return Map(("predicate", Text(name)));
        }

        public static FakeNode Op(string op, params FakeNode[] operands)
        {
            return Map(("op", Text(op)), ("operands", Seq(operands)));
        }

        public string Describe()
        {
            return this.Shape == TreeNodeShape.Scalar ? "a scalar" : $"a {this.KindName}";
        }

        public string DescribeValue()
        {
            return this.StringValue is { } text ? $"'{text}'" : this.Describe();
        }

        public bool TryGetChild(string key, [NotNullWhen(true)] out ITreeNodeCursor? child)
        {
            FakeNode? found = this.Entries?.FirstOrDefault(e => e.Key == key).Value;
            child = found;
            return found is not null;
        }

        public bool TryGetInt32(out int value)
        {
            value = this.Integer.GetValueOrDefault();
            return this.Integer is not null;
        }

        public bool TryGetTruthValue(out TruthValue value)
        {
            value = this.Truth.GetValueOrDefault();
            return this.Truth is not null;
        }

        public RawLiteral? ReadScalarLiteral()
        {
            if (this.StringValue is { } text)
            {
                return RawLiteral.OfString(text, this.Span);
            }

            return this.Integer is { } number ? RawLiteral.OfNumber(number.ToString(), this.Span) : null;
        }
    }
}
