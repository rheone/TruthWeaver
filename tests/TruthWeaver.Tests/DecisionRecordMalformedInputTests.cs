namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// The error branches of the decision record reader: one bad payload per row, each refused with the exact JSON path and
/// reason of the first problem. <see cref="DecisionRecordTests"/> covers the happy path and a first set of bad records.
/// </summary>
public sealed class DecisionRecordMalformedInputTests
{
    private const string Ok = "\"formatVersion\":1,\"argumentValues\":\"omitted\",\"result\":\"True\"";
    private const string Term = "{\"predicate\":\"p\",\"arguments\":[],\"variables\":[]}";
    private const string Exception = "{\"type\":\"X\",\"message\":\"m\"}";

    private static readonly string VariableType = typeof(VariableResolutionException).FullName!;

    /// <summary>Gets the rows: the bad record and the exact error the reader reports.</summary>
    public static TheoryData<string, string> BadRecords
    {
        get
        {
            TheoryData<string, string> rows = [];

            // The root.
            rows.Add("[]", "$: expected an object.");
            rows.Add("{\"result\":\"True\"}", "$.formatVersion: the member is required.");
            rows.Add(
                "{\"formatVersion\":\"1\",\"argumentValues\":\"omitted\",\"result\":\"True\",\"faults\":[]}",
                "$.formatVersion: expected an integer."
            );
            rows.Add(
                "{\"formatVersion\":1,\"argumentValues\":\"omitted\",\"result\":5,\"faults\":[]}",
                "$.result: expected a string."
            );
            rows.Add(
                "{\"formatVersion\":1,\"argumentValues\":\"maybe\",\"result\":\"True\",\"faults\":[]}",
                "$.argumentValues: expected \"included\" or \"omitted\"."
            );
            rows.Add("{\"formatVersion\":1,\"result\":\"True\",\"faults\":[]}", "$.argumentValues: the member is required.");
            rows.Add("{" + Ok + "}", "$.faults: the member is required.");
            rows.Add("{" + Ok + ",\"faults\":{}}", "$.faults: expected an array.");

            // The rule identity.
            rows.Add("{" + Ok + ",\"faults\":[],\"rule\":5}", "$.rule: expected an object.");
            rows.Add("{" + Ok + ",\"faults\":[],\"rule\":{}}", "$.rule.canonicalTextSha256: the member is required.");
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"rule\":{\"canonicalTextSha256\":\"" + new string('a', 64) + "\",\"name\":5}}",
                "$.rule.name: expected a string."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"rule\":{\"canonicalTextSha256\":\"" + new string('a', 64) + "\",\"version\":5}}",
                "$.rule.version: expected a string."
            );

            // The options.
            rows.Add("{" + Ok + ",\"faults\":[],\"options\":5}", "$.options: expected an object.");
            rows.Add("{" + Ok + ",\"faults\":[],\"options\":{\"extra\":1}}", "$.options.extra: unknown member.");
            rows.Add("{" + Ok + ",\"faults\":[],\"options\":{}}", "$.options.faultBudget: the member is required.");
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"options\":{\"faultBudget\":\"x\"}}",
                "$.options.faultBudget: expected an integer of at least 1, or null."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"options\":{\"faultBudget\":null}}",
                "$.options.mode: the member is required."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"options\":{\"faultBudget\":null,\"mode\":\"Fast\"}}",
                "$.options.mode: expected \"ShortCircuit\" or \"Exhaustive\"."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"options\":{\"faultBudget\":null,\"mode\":\"Exhaustive\"}}",
                "$.options.timeout: the member is required."
            );
            rows.Add(
                "{"
                    + Ok
                    + ",\"faults\":[],\"options\":{\"faultBudget\":null,\"mode\":\"Exhaustive\",\"timeout\":\"5 seconds\"}}",
                "$.options.timeout: expected a time span in the constant format (\"00:00:05\"), or null."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"options\":{\"faultBudget\":null,\"mode\":\"Exhaustive\",\"timeout\":5}}",
                "$.options.timeout: expected a time span in the constant format (\"00:00:05\"), or null."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"options\":{\"faultBudget\":null,\"mode\":\"Exhaustive\",\"timeout\":null}}",
                "$.options.includeResolvedValues: the member is required."
            );
            rows.Add(
                "{"
                    + Ok
                    + ",\"faults\":[],\"options\":{\"faultBudget\":null,\"mode\":\"Exhaustive\",\"timeout\":null,\"includeResolvedValues\":\"yes\"}}",
                "$.options.includeResolvedValues: expected true or false."
            );

            // A fault, its term and its arguments and variables.
            rows.Add(Faults("5"), "$.faults[0]: expected an object.");
            rows.Add(Faults("{\"exception\":" + Exception + "}"), "$.faults[0].term: the member is required.");
            rows.Add(Faults("{\"term\":" + Term + "}"), "$.faults[0].exception: the member is required.");
            rows.Add(Faults("{\"term\":5,\"exception\":" + Exception + "}"), "$.faults[0].term: expected an object.");
            rows.Add(
                Faults("{\"term\":{\"arguments\":[],\"variables\":[]},\"exception\":" + Exception + "}"),
                "$.faults[0].term.predicate: the member is required."
            );
            rows.Add(
                Faults("{\"term\":{\"predicate\":\"p\",\"arguments\":{},\"variables\":[]},\"exception\":" + Exception + "}"),
                "$.faults[0].term.arguments: expected an array."
            );
            rows.Add(
                Faults("{\"term\":{\"predicate\":\"p\",\"arguments\":[],\"variables\":{}},\"exception\":" + Exception + "}"),
                "$.faults[0].term.variables: expected an array."
            );
            rows.Add(Faults(WithArgument("5")), "$.faults[0].term.arguments[0]: expected an object.");
            rows.Add(
                Faults(WithArgument("{\"kind\":\"Int64\"}")),
                "$.faults[0].term.arguments[0].name: the member is required."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Float\"}")),
                "$.faults[0].term.arguments[0].kind: 'Float' is not a literal kind."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Int64\"}"), "included"),
                "$.faults[0].term.arguments[0]: the value is missing, but argumentValues is \"included\"."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"String\",\"value\":5}"), "included"),
                "$.faults[0].term.arguments[0].value: expected a String value."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Boolean\",\"value\":\"x\"}"), "included"),
                "$.faults[0].term.arguments[0].value: expected a Boolean value."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"DateTimeOffset\",\"value\":\"yesterday\"}"), "included"),
                "$.faults[0].term.arguments[0].value: expected a DateTimeOffset value."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Guid\",\"value\":\"nope\"}"), "included"),
                "$.faults[0].term.arguments[0].value: expected a Guid value."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Decimal\",\"value\":\"1.5\"}"), "included"),
                "$.faults[0].term.arguments[0].value: expected a Decimal value."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Int64Array\",\"value\":5}"), "included"),
                "$.faults[0].term.arguments[0].value: expected a Int64Array value (an array)."
            );
            rows.Add(
                Faults(WithArgument("{\"name\":\"a\",\"kind\":\"Int64Array\",\"value\":[1,\"x\"]}"), "included"),
                "$.faults[0].term.arguments[0].value[1]: expected a Int64 value."
            );
            rows.Add(Faults(WithVariable("5")), "$.faults[0].term.variables[0]: expected an object.");
            rows.Add(
                Faults(WithVariable("{\"name\":\"v\",\"source\":\"s\"}")),
                "$.faults[0].term.variables[0].query: the member is required."
            );

            // A fault exception.
            rows.Add(
                Faults("{\"term\":" + Term + ",\"exception\":{\"message\":\"m\"}}"),
                "$.faults[0].exception.type: the member is required."
            );
            rows.Add(
                Faults("{\"term\":" + Term + ",\"exception\":{\"type\":\"X\"}}"),
                "$.faults[0].exception.message: the member is required."
            );
            rows.Add(
                Faults("{\"term\":" + Term + ",\"exception\":{\"type\":\"X\",\"message\":\"m\",\"variable\":{}}}"),
                "$.faults[0].exception: 'variable' is allowed only when the type is " + VariableType + "."
            );
            rows.Add(
                Faults("{\"term\":" + Term + ",\"exception\":{\"type\":\"" + VariableType + "\",\"message\":\"m\"}}"),
                "$.faults[0].exception: a " + VariableType + " needs 'variable'."
            );
            rows.Add(
                Faults(VariableFault("{\"source\":\"s\",\"query\":\"q\"}")),
                "$.faults[0].exception.variable.failure: the member is required."
            );
            rows.Add(
                Faults(VariableFault("{\"source\":\"s\",\"query\":\"q\",\"failure\":\"Exploded\"}")),
                "$.faults[0].exception.variable.failure: 'Exploded' is not a variable failure kind."
            );
            rows.Add(Faults(VariableFault("5")), "$.faults[0].exception.variable: expected an object.");

            // The flat trace.
            rows.Add("{" + Ok + ",\"faults\":[],\"trace\":{}}", "$.trace: expected an array.");
            rows.Add("{" + Ok + ",\"faults\":[],\"trace\":[5]}", "$.trace[0]: expected an object.");
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"trace\":[{\"text\":\"a\",\"result\":\"True\"}]}",
                "$.trace[0].notEvaluated: the member is required."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"trace\":[{\"text\":\"a\",\"result\":\"True\",\"notEvaluated\":\"no\"}]}",
                "$.trace[0].notEvaluated: expected true or false."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"trace\":[{\"text\":\"a\",\"result\":null,\"notEvaluated\":false}]}",
                "$.trace[0].result: an evaluated node needs a result."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"trace\":[{\"text\":\"a\",\"result\":\"True\",\"notEvaluated\":true}]}",
                "$.trace[0].result: a node that was not evaluated has a null result."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"trace\":[{\"text\":\"a\",\"result\":5,\"notEvaluated\":false}]}",
                "$.trace[0].result: expected \"True\", \"False\", \"Unknown\" or null."
            );
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"trace\":[{\"text\":\"a\",\"result\":\"Maybe\",\"notEvaluated\":false}]}",
                "$.trace[0].result: expected \"True\", \"False\" or \"Unknown\"."
            );

            // The trace tree.
            rows.Add("{" + Ok + ",\"faults\":[],\"traceTree\":5}", "$.traceTree: expected an object.");
            rows.Add(
                "{" + Ok + ",\"faults\":[],\"traceTree\":{\"text\":\"a\",\"result\":\"True\",\"notEvaluated\":false}}",
                "$.traceTree.children: the member is required."
            );
            rows.Add(
                "{"
                    + Ok
                    + ",\"faults\":[],\"traceTree\":{\"text\":\"a\",\"result\":\"True\",\"notEvaluated\":false,\"children\":{}}}",
                "$.traceTree.children: expected an array."
            );
            rows.Add(
                "{"
                    + Ok
                    + ",\"faults\":[],\"traceTree\":{\"text\":\"a\",\"result\":null,\"notEvaluated\":true,\"children\":[{\"text\":\"b\",\"result\":null,\"notEvaluated\":true,\"children\":[]}]}}",
                "$.traceTree.children: a node that was not evaluated has no children."
            );
            rows.Add(
                "{"
                    + Ok
                    + ",\"faults\":[],\"traceTree\":{\"text\":\"a\",\"result\":\"True\",\"notEvaluated\":false,\"children\":[{\"text\":\"b\",\"result\":\"Maybe\",\"notEvaluated\":false,\"children\":[]}]}}",
                "$.traceTree.children[0].result: expected \"True\", \"False\" or \"Unknown\"."
            );

            return rows;
        }
    }

    /// <summary>A malformed decision record is refused with the exact JSON path and reason of its first problem.</summary>
    [Theory]
    [MemberData(nameof(BadRecords))]
    public void TryParseJson_MalformedRecord_ReportsTheExactPathAndReason_Test(string json, string expectedError)
    {
        bool read = DecisionRecord.TryParseJson(json, out DecisionRecord? record, out string? error);

        Assert.False(read);
        Assert.Null(record);
        Assert.Equal(expectedError, error);
    }

    /// <summary>Text that is not JSON, and JSON with a repeated member, are refused as invalid JSON at the root.</summary>
    [Theory]
    [InlineData("{")]
    [InlineData("{\"formatVersion\":1,\"formatVersion\":1}")]
    public void TryParseJson_TextThatIsNotValidJson_ReportsTheRootAsInvalidJson_Test(string json)
    {
        bool read = DecisionRecord.TryParseJson(json, out DecisionRecord? record, out string? error);

        Assert.False(read);
        Assert.Null(record);
        Assert.StartsWith("$: the text is not valid JSON:", error, StringComparison.Ordinal);
    }

    /// <summary>
    /// Text that holds a lone UTF-16 surrogate is refused as invalid JSON at the root, with <see langword="false"/>
    /// and an error, never an exception.
    /// </summary>
    [Fact]
    public void TryParseJson_LoneSurrogate_ReportsTheRootAsInvalidJson_Test()
    {
        bool read = DecisionRecord.TryParseJson(
            "{\"formatVersion\":1,\"argumentValues\":\"omitted\",\"result\":\"True\",\"faults\":[],\"x\":\"\uD800\"}",
            out DecisionRecord? record,
            out string? error
        );

        Assert.False(read);
        Assert.Null(record);
        Assert.StartsWith("$: the text is not valid JSON:", error, StringComparison.Ordinal);
        Assert.Contains("UTF-16", error, StringComparison.Ordinal);
    }

    private static string Faults(string fault, string argumentValues = "omitted")
    {
        return "{\"formatVersion\":1,\"argumentValues\":\""
            + argumentValues
            + "\",\"result\":\"Unknown\",\"faults\":["
            + fault
            + "]}";
    }

    // A fault whose term carries the given argument member.
    private static string WithArgument(string argument)
    {
        return "{\"term\":{\"predicate\":\"p\",\"arguments\":["
            + argument
            + "],\"variables\":[]},\"exception\":"
            + Exception
            + "}";
    }

    // A fault whose term carries the given variable member.
    private static string WithVariable(string variable)
    {
        return "{\"term\":{\"predicate\":\"p\",\"arguments\":[],\"variables\":["
            + variable
            + "]},\"exception\":"
            + Exception
            + "}";
    }

    // A fault whose exception is a variable failure carrying the given 'variable' member.
    private static string VariableFault(string variable)
    {
        return "{\"term\":"
            + Term
            + ",\"exception\":{\"type\":\""
            + VariableType
            + "\",\"message\":\"m\",\"variable\":"
            + variable
            + "}}";
    }
}
