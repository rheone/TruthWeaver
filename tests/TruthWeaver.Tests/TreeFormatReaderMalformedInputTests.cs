namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// The error branches of the tree reader for the parts of the tree shape that
/// <see cref="TreeFormatReaderDiagnosticTests"/> does not reach: the <c>definitions</c> section, <c>use</c> nodes and
/// <c>forall</c> / <c>exists</c> quantifiers. Each row is one bad payload and the first error it must produce.
/// </summary>
public sealed class TreeFormatReaderMalformedInputTests
{
    private const string MalformedTree = "TRE0014";

    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build()
    );

    /// <summary>Gets the rows: the payload, the code, the JSON path and a fragment of the message.</summary>
    public static TheoryData<string, string, string, string> BadJson =>
        new()
        {
            {
                """{"definitions":5,"const":true}""",
                MalformedTree,
                "$.definitions",
                "'definitions' must be a JSON object, keyed by definition name."
            },
            {
                """{"definitions":{"d":5},"const":true}""",
                MalformedTree,
                "$.definitions.d",
                "A definition entry must be a JSON object with 'parameters' and 'body'."
            },
            {
                """{"definitions":{"d":{"body":{"const":true},"x":1}},"const":true}""",
                MalformedTree,
                "$.definitions.d.x",
                "Unknown key 'x' in a definition"
            },
            {
                """{"definitions":{"d":{"parameters":5,"body":{"const":true}}},"const":true}""",
                MalformedTree,
                "$.definitions.d.parameters",
                "'parameters' must be a array of JSON strings."
            },
            {
                """{"definitions":{"d":{"parameters":[1],"body":{"const":true}}},"const":true}""",
                MalformedTree,
                "$.definitions.d.parameters[0]",
                "Each parameter name must be a JSON string."
            },
            {
                """{"definitions":{"d":{"parameters":[]}},"const":true}""",
                MalformedTree,
                "$.definitions.d",
                "A definition requires a 'body' node."
            },
            {
                """{"definitions":{"d":{"body":5}},"const":true}""",
                MalformedTree,
                "$.definitions.d.body",
                "Expected a JSON object node"
            },
            {
                """{"op":"use","name":5}""",
                MalformedTree,
                "$.name",
                "A 'use' node requires a 'name' property naming the definition."
            },
            { """{"op":"use"}""", MalformedTree, "$", "A 'use' node requires a 'name' property naming the definition." },
            {
                """{"op":"use","name":"d","operands":5}""",
                MalformedTree,
                "$.operands",
                "'operands' of a 'use' node must be a array."
            },
            { """{"op":"use","name":"d","operands":[5]}""", MalformedTree, "$.operands[0]", "Expected a JSON object node" },
            { """{"op":"use","name":"d","extra":1}""", MalformedTree, "$.extra", "Unknown key 'extra' in a 'use' node" },
            {
                """{"op":"forall","in":"c","operands":[{"const":true}]}""",
                MalformedTree,
                "$",
                "A 'forall' quantifier requires a 'var' property naming the bound variable."
            },
            {
                """{"op":"exists","var":5,"in":"c","operands":[{"const":true}]}""",
                MalformedTree,
                "$.var",
                "A 'exists' quantifier requires a 'var' property naming the bound variable."
            },
            {
                """{"op":"forall","var":"x","operands":[{"const":true}]}""",
                MalformedTree,
                "$",
                "A 'forall' quantifier requires an 'in' property naming the collection."
            },
            {
                """{"op":"forall","var":"x","in":5,"operands":[{"const":true}]}""",
                MalformedTree,
                "$.in",
                "A 'forall' quantifier requires an 'in' property naming the collection."
            },
            {
                """{"op":"forall","var":"x","in":"c"}""",
                MalformedTree,
                "$",
                "A 'forall' quantifier requires an 'operands' array with exactly one item (the body)."
            },
            {
                """{"op":"forall","var":"x","in":"c","operands":5}""",
                MalformedTree,
                "$.operands",
                "A 'forall' quantifier requires an 'operands' array with exactly one item (the body)."
            },
            {
                """{"op":"forall","var":"x","in":"c","operands":[]}""",
                MalformedTree,
                "$.operands",
                "A 'forall' quantifier requires an 'operands' array with exactly one item (the body)."
            },
            {
                """{"op":"exists","var":"x","in":"c","operands":[{"const":true},{"const":false}]}""",
                MalformedTree,
                "$.operands",
                "A 'exists' quantifier requires an 'operands' array with exactly one item (the body)."
            },
            {
                """{"op":"forall","var":"x","in":"c","operands":[5]}""",
                MalformedTree,
                "$.operands[0]",
                "Expected a JSON object node"
            },
            {
                """{"op":"forall","var":"x","in":"c","operands":[{"const":true}],"extra":1}""",
                MalformedTree,
                "$.extra",
                "Unknown key 'extra' in a 'forall' quantifier"
            },
        };

    /// <summary>Gets the rows: the YAML payload, the code, the path and a fragment of the message.</summary>
    public static TheoryData<string, string, string, string> BadYaml =>
        new()
        {
            { "const: true\n? [1]\n: x\n", MalformedTree, "$", "A key of a constant node must be a YAML string." },
            {
                "definitions:\n  ? [1]\n  : {body: {const: true}}\nconst: true\n",
                MalformedTree,
                "$.definitions",
                "A definition name must be a YAML string."
            },
            {
                "definitions:\n  d:\n    parameters: x\n    body: {const: true}\nconst: true\n",
                MalformedTree,
                "$.definitions.d.parameters",
                "'parameters' must be a sequence of YAML strings."
            },
            {
                "op: use\nname: d\noperands: x\n",
                MalformedTree,
                "$.operands",
                "'operands' of a 'use' node must be a sequence."
            },
            {
                "op: exists\nvar: x\nin: c\noperands: []\n",
                MalformedTree,
                "$.operands",
                "A 'exists' quantifier requires an 'operands' sequence with exactly one item (the body)."
            },
        };

    /// <summary>A malformed JSON tree is refused with the specific code, path and message.</summary>
    [Theory]
    [MemberData(nameof(BadJson))]
    public void CompileJson_MalformedTree_ReportsTheSpecificDiagnostic_Test(
        string json,
        string code,
        string path,
        string messageFragment
    )
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        AssertFirstError(result, code, path, messageFragment);
    }

    /// <summary>A malformed YAML tree is refused with the specific code, path and message in the YAML wording.</summary>
    [Theory]
    [MemberData(nameof(BadYaml))]
    public void CompileYaml_MalformedTree_ReportsTheSpecificDiagnostic_Test(
        string yaml,
        string code,
        string path,
        string messageFragment
    )
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(yaml);

        AssertFirstError(result, code, path, messageFragment);
    }

    private static void AssertFirstError(
        CompilationResult<RuleTestContext> result,
        string code,
        string path,
        string messageFragment
    )
    {
        Assert.False(result.Succeeded);
        Diagnostic first = result.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(code, first.Code);
        Assert.Equal(path, first.Path);
        Assert.Contains(messageFragment, first.Message, StringComparison.Ordinal);
    }
}
