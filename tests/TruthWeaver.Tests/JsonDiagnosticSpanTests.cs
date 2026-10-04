namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Source spans on diagnostics raised while compiling JSON rule trees (k3-followups 24): each diagnostic that is located
/// by a path also covers the offending node in the original text, in the same character unit the DSL and YAML use.
/// </summary>
public sealed class JsonDiagnosticSpanTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddStringArgPredicate("hasRole", "role", "Y")
            .Build()
    );

    /// <summary>An unknown operator's span covers the operator name string, quotes included.</summary>
    [Fact]
    public void CompileJson_UnknownOperator_SpanCoversTheOperatorValue_Test()
    {
        const string json = """{"op":"annd","operands":[{"const":true},{"const":false}]}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("\"annd\"", Covered(json, diagnostic));
    }

    /// <summary>A malformed tree node (a wrong-typed <c>const</c>) is underlined at the offending value.</summary>
    [Fact]
    public void CompileJson_MalformedConst_SpanCoversTheConstValue_Test()
    {
        const string json = """{"op":"not","operands":[{"const":5}]}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("5", Covered(json, diagnostic));
    }

    /// <summary>A node-level problem (missing <c>operands</c>) covers the whole object.</summary>
    [Fact]
    public void CompileJson_OperatorWithoutOperands_SpanCoversTheWholeNode_Test()
    {
        const string json = """{"op":"and","operands":[{"op":"or"}]}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("{\"op\":\"or\"}", Covered(json, diagnostic));
    }

    /// <summary>An invalid predicate argument raised after parsing (by the validator) carries the argument value's span.</summary>
    [Fact]
    public void CompileJson_InvalidArgumentValue_SpanCoversTheArgumentValue_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":5}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("$.args.role", diagnostic.Path);
        Assert.Equal("5", Covered(json, diagnostic));
    }

    /// <summary>Offsets count UTF-16 characters, not UTF-8 bytes, so multibyte and surrogate-pair text still lines up.</summary>
    [Fact]
    public void CompileJson_MultibyteTextBeforeTheNode_SpanIsInCharacters_Test()
    {
        const string json = """{"op":"and","operands":[{"predicate":"héllo😀"},{"op":"annd","operands":[]}]}""";

        Diagnostic diagnostic = Assert.Single(
            Compiler.CompileJson(json).Diagnostics,
            d => d.Severity == DiagnosticSeverity.Error && d.Path == "$.operands[1].op"
        );

        Assert.Equal("\"annd\"", Covered(json, diagnostic));
    }

    /// <summary>
    /// A byte-order mark character is not valid JSON text, so it is reported as a syntax error whose span is the one
    /// character, not the three bytes it encodes to.
    /// </summary>
    [Fact]
    public void CompileJson_LeadingByteOrderMark_SyntaxErrorSpanIsOneCharacter_Test()
    {
        const string json = "﻿{\"op\":\"annd\",\"operands\":[]}";

        Diagnostic diagnostic = Single(json);

        Assert.Equal(new SourceSpan(0, 1), diagnostic.Span);
    }

    /// <summary>Text spread over several lines is located by absolute character offset, so line breaks do not skew it.</summary>
    [Fact]
    public void CompileJson_MultilineText_SpanIsAnAbsoluteOffset_Test()
    {
        const string json = "{\r\n  \"op\": \"and\",\r\n  \"operands\": [\r\n    { \"const\": 7 }\r\n  ]\r\n}";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("7", Covered(json, diagnostic));
        Assert.Equal(new SourceLocation(4, 16), diagnostic.Span.GetLocation(json));
    }

    /// <summary>Adding a span leaves the path as it was: the path stays the primary locator for JSON.</summary>
    [Fact]
    public void CompileJson_DiagnosticWithASpan_KeepsItsPath_Test()
    {
        Diagnostic diagnostic = Single("""{"op":"and","operands":[{"op":"orr","operands":[]}]}""");

        Assert.Equal("$.operands[0].op", diagnostic.Path);
    }

    /// <summary>A subtree compiled from a <see cref="System.Text.Json.JsonElement"/> has no text to point into, so it has no span.</summary>
    [Fact]
    public void CompileJson_FromAJsonElement_HasNoSpan_Test()
    {
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse("""{"op":"annd","operands":[]}""");

        Diagnostic diagnostic = Assert.Single(Compiler.CompileJson(document.RootElement).Diagnostics);

        Assert.Equal(SourceSpan.None, diagnostic.Span);
    }

    private static Diagnostic Single(string json)
    {
        return Assert.Single(Compiler.CompileJson(json).Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static string Covered(string json, Diagnostic diagnostic)
    {
        return json.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
    }
}
