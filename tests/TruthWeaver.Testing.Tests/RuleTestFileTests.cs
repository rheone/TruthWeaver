namespace TruthWeaver.Testing.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using global::Json.Schema;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using YamlDotNet.RepresentationModel;

public sealed class RuleTestFileTests
{
    private static readonly string SamplesDirectory = Path.Combine(AppContext.BaseDirectory, "RuleTests");

    private static readonly JsonSchema Schema = JsonSchema.FromFile(Path.Combine(SamplesDirectory, "rule-test.schema.json"));

    /// <summary>A case passes when the result is the expected value, for each of the three values.</summary>
    [Theory]
    [InlineData("True", "True")]
    [InlineData("False", "False")]
    [InlineData("Unknown", "Unknown")]
    public async Task RunAsync_ExpectedValueMatches_CasePasses_Test(string given, string expected)
    {
        RuleTestFile file = Parse(OneCase(given, expected));

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(report.Passed, report.ToString());
    }

    /// <summary>A case fails when the result is another value, for each of the three expected values, and says why.</summary>
    [Theory]
    [InlineData("False", "True")]
    [InlineData("Unknown", "False")]
    [InlineData("True", "Unknown")]
    public async Task RunAsync_ExpectedValueDiffers_CaseFails_Test(string given, string expected)
    {
        RuleTestFile file = Parse(OneCase(given, expected));

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        RuleTestResult failure = Assert.Single(report.Failures);
        Assert.Equal($"Expected the result {expected}, but the result is {given}.", Assert.Single(failure.Failures));
    }

    /// <summary>The sample with a rule file runs every case, and each one passes.</summary>
    [Fact]
    public async Task RunAsync_RuleFileSample_EveryCasePasses_Test()
    {
        RuleTestReport report = await RuleTestFile.RunAsync(
            Path.Combine(SamplesDirectory, "access.test.yaml"),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("access", report.Name);
        Assert.Equal(3, report.Results.Count);
        report.ShouldPass();
    }

    /// <summary>Inline variables and a data file resolve a from(...) reference; with no data the reference is a fault.</summary>
    [Fact]
    public async Task RunAsync_VariablesSample_EveryCasePasses_Test()
    {
        RuleTestReport report = await RuleTestFile.RunAsync(
            Path.Combine(SamplesDirectory, "region.test.yaml"),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("region.test", report.Name);
        report.ShouldPass();
    }

    /// <summary>An inline YAML rule tree compiles as a YAML rule.</summary>
    [Fact]
    public async Task RunAsync_InlineRuleTree_CasePasses_Test()
    {
        RuleTestFile file = Parse(
            """
            rule:
              op: NOT
              operands:
                - predicate: isActive
            cases:
              - name: inactive
                given: { isActive: False }
                expect: { result: True }
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RuleFormat.Yaml, file.RuleFormat);
        report.ShouldPass();
    }

    /// <summary>A wrong fault count fails the case with both counts.</summary>
    [Fact]
    public async Task RunAsync_FaultCountDiffers_CaseFails_Test()
    {
        RuleTestFile file = Parse(
            """
            rule: 'inRegion(region: from("user", "$.region"))'
            cases:
              - name: no data
                expect: { result: Unknown, faults: 0 }
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            "Expected 0 fault(s), but the evaluation recorded 1.",
            Assert.Single(Assert.Single(report.Failures).Failures)
        );
    }

    /// <summary>The trace assertion compares the plain text trace, ignoring line endings and trailing spaces.</summary>
    [Fact]
    public async Task RunAsync_TraceMatches_CasePasses_Test()
    {
        CompiledRule<StubValues> rule = StubRuleCompiler.Compile("isActive AND isAdmin", RuleFormat.Text).GetRuleOrThrow();
        StubValues stubs = StubValues.Create(new Dictionary<string, TruthValue> { ["isActive"] = TruthValue.False });
        string trace = rule.PrintPlainText(
            await rule.EvaluateAsync(stubs, cancellationToken: TestContext.Current.CancellationToken)
        );
        string indented = string.Join("\n", trace.Split('\n').Select(line => "        " + line + "  "));
        RuleTestFile file = Parse(
            $"""
            rule: isActive AND isAdmin
            cases:
              - name: short circuit
                given:
                  isActive: False
                expect:
                  result: False
                  trace: |
            {indented}
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        report.ShouldPass();
    }

    /// <summary>A trace that differs fails the case and shows the expected and the actual trace.</summary>
    [Fact]
    public async Task RunAsync_TraceDiffers_CaseFailsWithBothTraces_Test()
    {
        RuleTestFile file = Parse(
            """
            rule: isActive AND isAdmin
            cases:
              - name: wrong trace
                given: { isActive: False }
                expect: { result: False, trace: "OR [false]" }
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        string failure = Assert.Single(Assert.Single(report.Failures).Failures);
        Assert.StartsWith(
            "The trace is not the expected trace.\nExpected:\nOR [false]\nActual:\n",
            failure,
            StringComparison.Ordinal
        );
    }

    /// <summary>A given term whose predicate the rule does not use fails the case, and a spelling in another case is named.</summary>
    [Fact]
    public async Task RunAsync_GivenTermNotInRule_CaseFails_Test()
    {
        RuleTestFile file = Parse(
            """
            rule: isActive OR isAdmin
            cases:
              - name: typo
                given: { isactive: True, isEditor: True }
                expect: { result: Unknown }
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                "The given term 'isactive' spells the predicate 'isactive', but the rule spells it 'isActive'.",
                "The given term 'isEditor' uses the predicate 'isEditor', which the rule does not use.",
            ],
            Assert.Single(report.Failures).Failures
        );
    }

    /// <summary>
    /// A given term with the predicate of the rule but other literal arguments fails the case, and a term whose arguments
    /// come from data is accepted, because its looked-up term is known only when the data answers.
    /// </summary>
    [Fact]
    public async Task RunAsync_GivenTermWithOtherArguments_CaseFails_Test()
    {
        RuleTestFile file = Parse(
            """
            rule: |
              hasRole(role: "editor") OR inRegion(region: from("user", "$.region"))
            cases:
              - name: typo
                given:
                  'hasRole(role: "edtor")': True
                  'inRegion(region: "EU")': False
                expect: { result: Unknown, faults: 1 }
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                "The given term 'hasRole(role: \"edtor\")' is not a term of the rule. The rule uses 'hasRole' as hasRole(role: \"editor\").",
            ],
            Assert.Single(report.Failures).Failures
        );
    }

    /// <summary>Empty inline rule text makes the file unusable, as the schema states.</summary>
    [Fact]
    public void TryParse_EmptyRule_ReturnsAnError_Test()
    {
        bool parsed = RuleTestFile.TryParse(
            "rule: ''\ncases: [{ name: a, expect: { result: True } }]",
            SamplesDirectory,
            out _,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.False(parsed);
        Assert.Equal("$.rule", Assert.Single(errors).Path);
    }

    /// <summary>A case name used a second time is reported even when that case has other problems.</summary>
    [Fact]
    public void TryParse_DuplicateNameOnCaseWithProblems_ReportsTheName_Test()
    {
        bool parsed = RuleTestFile.TryParse(
            "rule: a\ncases:\n  - { name: x, expect: { result: True } }\n  - { name: x, expect: { result: Maybe } }\n",
            SamplesDirectory,
            out _,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.False(parsed);
        Assert.Equal(["$.cases[1].name", "$.cases[1].expect.result"], errors.Select(e => e.Path));
    }

    /// <summary>A rule that does not compile fails every case, and the report holds the compile errors.</summary>
    [Fact]
    public async Task RunAsync_RuleDoesNotCompile_EveryCaseFails_Test()
    {
        RuleTestFile file = Parse(
            """
            rule: isActive AND
            cases:
              - name: first
                expect: { result: True }
              - name: second
                expect: { result: False }
            """
        );

        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, report.Failures.Count);
        Assert.All(report.Failures, r => Assert.False(r.RuleCompiles));
        Assert.NotEmpty(report.RuleDiagnostics);
    }

    /// <summary>Every problem of an unusable file is reported with its path and place, not only the first one.</summary>
    [Fact]
    public void TryParse_InvalidFile_ReportsEveryProblem_Test()
    {
        bool parsed = RuleTestFile.TryParse(
            """
            rule: isActive
            extra: 1
            cases:
              - name: one
                given: { isActive: maybe, "a AND b": True }
                expect: { result: Yes, faults: -1 }
              - name: one
                expect: { result: True }
            """,
            SamplesDirectory,
            out RuleTestFile? file,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.False(parsed);
        Assert.Null(file);
        Assert.Equal(
            [
                "$",
                "$.cases[0].given['isActive']",
                "$.cases[0].given['a AND b']",
                "$.cases[0].expect.result",
                "$.cases[0].expect.faults",
                "$.cases[1].name",
            ],
            errors.Select(e => e.Path)
        );
        Assert.Equal(2, errors[0].Line);
    }

    /// <summary>A file with both or neither of rule and ruleFile is not usable.</summary>
    [Theory]
    [InlineData("cases: [{ name: a, expect: { result: True } }]")]
    [InlineData("rule: a\nruleFile: access.rule\ncases: [{ name: a, expect: { result: True } }]")]
    public void TryParse_NotExactlyOneRule_ReturnsAnError_Test(string yaml)
    {
        bool parsed = RuleTestFile.TryParse(yaml, SamplesDirectory, out _, out IReadOnlyList<RuleTestFileError> errors);

        Assert.False(parsed);
        Assert.Contains(errors, e => e.Message.Contains("exactly one of 'rule'", StringComparison.Ordinal));
    }

    /// <summary>Two cases with one name are a problem.</summary>
    [Fact]
    public void TryParse_DuplicateCaseName_ReturnsAnError_Test()
    {
        bool parsed = RuleTestFile.TryParse(
            "rule: a\ncases:\n  - { name: x, expect: { result: True } }\n  - { name: x, expect: { result: True } }\n",
            SamplesDirectory,
            out _,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.False(parsed);
        Assert.Equal("$.cases[1].name", Assert.Single(errors).Path);
    }

    /// <summary>A rule file or data file that does not exist is a problem of the test file.</summary>
    [Fact]
    public void TryParse_MissingFiles_ReturnsAnErrorForEach_Test()
    {
        bool parsed = RuleTestFile.TryParse(
            "ruleFile: missing.rule\ncases:\n  - { name: x, variableFiles: { user: missing.json }, expect: { result: True } }\n",
            SamplesDirectory,
            out _,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.False(parsed);
        Assert.Equal(["$.ruleFile", "$.cases[0].variableFiles['user']"], errors.Select(e => e.Path));
    }

    /// <summary>The static RunAsync throws a RuleTestException that lists the problems of an unusable file.</summary>
    [Fact]
    public async Task RunAsync_UnusableFile_ThrowsRuleTestException_Test()
    {
        string path = Path.Combine(SamplesDirectory, "missing.test.yaml");

        RuleTestException exception = await Assert.ThrowsAsync<RuleTestException>(() =>
            RuleTestFile.RunAsync(path, cancellationToken: TestContext.Current.CancellationToken)
        );

        Assert.Contains("is not usable", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>ShouldPass throws with the report text when a case failed.</summary>
    [Fact]
    public async Task ShouldPass_FailedCase_ThrowsWithTheReportText_Test()
    {
        RuleTestReport report = await Parse(OneCase("True", "False"))
            .RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        RuleTestException exception = Assert.Throws<RuleTestException>(report.ShouldPass);

        Assert.Equal(
            "FAIL case\n  Expected the result False, but the result is True.\n1 case(s): 0 passed, 1 failed.",
            exception.Message
        );
    }

    /// <summary>The JUnit XML of passing, failing and erroring cases validates against the JUnit schema.</summary>
    [Fact]
    public async Task PrintJUnit_MixedResults_ValidatesAgainstTheJUnitSchema_Test()
    {
        RuleTestFile file = Parse(
            """
            name: mixed <&> "suite"
            rule: isActive
            cases:
              - { name: passes, given: { isActive: True }, expect: { result: True } }
              - { name: fails, given: { isActive: True }, expect: { result: False } }
            """
        );
        RuleTestReport passedAndFailed = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);
        RuleTestReport errored = await Parse("rule: isActive AND\ncases:\n  - { name: broken, expect: { result: True } }\n")
            .RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (RuleTestReport report in new[] { passedAndFailed, errored })
        {
            string xml = report.PrintJUnit();
            List<string> problems = ValidateJUnit(xml);
            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        XElement suite = XDocument.Parse(passedAndFailed.PrintJUnit()).Root!.Element("testsuite")!;
        Assert.Equal("mixed <&> \"suite\"", suite.Attribute("name")!.Value);
        Assert.Equal(
            ("2", "1", "0"),
            (suite.Attribute("tests")!.Value, suite.Attribute("failures")!.Value, suite.Attribute("errors")!.Value)
        );
        Assert.Equal("1", XDocument.Parse(errored.PrintJUnit()).Root!.Element("testsuite")!.Attribute("errors")!.Value);
    }

    /// <summary>The JUnit XML is the same on each run: no time, LF line endings and a final line feed.</summary>
    [Fact]
    public async Task PrintJUnit_SameRun_GivesTheSameText_Test()
    {
        RuleTestReport report = await Parse(OneCase("True", "True"))
            .RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        string xml = report.PrintJUnit();

        Assert.Equal(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <testsuites name="rule-test" tests="1" failures="0" errors="0">
              <testsuite name="rule-test" tests="1" failures="0" errors="0" skipped="0">
                <testcase name="case" classname="rule-test" />
              </testsuite>
            </testsuites>

            """.ReplaceLineEndings("\n"),
            xml
        );
    }

    /// <summary>Every sample test file validates against the shipped JSON schema.</summary>
    [Theory]
    [InlineData("access.test.yaml")]
    [InlineData("region.test.yaml")]
    public void Schema_SampleFile_IsValid_Test(string name)
    {
        JsonNode? document = YamlToJson(File.ReadAllText(Path.Combine(SamplesDirectory, name)));

        EvaluationResults results = Schema.Evaluate(
            JsonSerializer.SerializeToElement(document),
            new global::Json.Schema.EvaluationOptions { OutputFormat = OutputFormat.List }
        );

        Assert.True(
            results.IsValid,
            string.Join('\n', (results.Details ?? []).Where(d => !d.IsValid).Select(d => d.ToString()))
        );
    }

    /// <summary>The schema rejects what the reader rejects: an unknown key, both rule keys, a value that is not a truth value.</summary>
    [Theory]
    [InlineData("rule: a\nextra: 1\ncases: [{ name: x, expect: { result: True } }]")]
    [InlineData("rule: a\nruleFile: a.rule\ncases: [{ name: x, expect: { result: True } }]")]
    [InlineData("rule: a\ncases: [{ name: x, expect: { result: maybe } }]")]
    [InlineData("rule: a\ncases: [{ name: x, given: { a: 2 }, expect: { result: True } }]")]
    [InlineData("rule: a\ncases: []")]
    public void Schema_InvalidFile_IsRejected_Test(string yaml)
    {
        JsonNode? document = YamlToJson(yaml);

        EvaluationResults results = Schema.Evaluate(JsonSerializer.SerializeToElement(document));

        Assert.False(results.IsValid);
        Assert.False(RuleTestFile.TryParse(yaml, SamplesDirectory, out _, out _));
    }

    private static string OneCase(string given, string expected)
    {
        return $"rule: isActive\ncases:\n  - name: case\n    given: {{ isActive: {given} }}\n    expect: {{ result: {expected} }}\n";
    }

    private static RuleTestFile Parse(string yaml)
    {
        Assert.True(
            RuleTestFile.TryParse(yaml, SamplesDirectory, out RuleTestFile? file, out IReadOnlyList<RuleTestFileError> errors),
            string.Join("\n", errors)
        );
        return file;
    }

    private static List<string> ValidateJUnit(string xml)
    {
        XmlSchemaSet schemas = new();
        using (XmlReader schemaReader = XmlReader.Create(Path.Combine(SamplesDirectory, "junit-10.xsd")))
        {
            schemas.Add(null, schemaReader);
        }

        List<string> problems = [];
        XmlReaderSettings settings = new() { ValidationType = ValidationType.Schema, Schemas = schemas };
        settings.ValidationEventHandler += (_, e) => problems.Add(e.Message);
        using StringReader text = new(xml);
        using XmlReader reader = XmlReader.Create(text, settings);
        while (reader.Read())
        {
            // Reading the whole document runs the validation; the handler collects the problems.
        }

        return problems;
    }

    /// <summary>Reads YAML into JSON with the YAML 1.2 core types for plain scalars: integers, Booleans and null.</summary>
    private static JsonNode? YamlToJson(string yaml)
    {
        YamlStream stream = [];
        using StringReader reader = new(yaml);
        stream.Load(reader);
        return Convert(stream.Documents[0].RootNode);

        static JsonNode? Convert(YamlNode node)
        {
            switch (node)
            {
                case YamlMappingNode mapping:
                    JsonObject result = [];
                    foreach ((YamlNode key, YamlNode value) in mapping.Children)
                    {
                        result[((YamlScalarNode)key).Value!] = Convert(value);
                    }

                    return result;
                case YamlSequenceNode sequence:
                    return new JsonArray([.. sequence.Children.Select(Convert)]);
                default:
                    YamlScalarNode scalar = (YamlScalarNode)node;
                    string text = scalar.Value ?? string.Empty;
                    if (scalar.Style is YamlDotNet.Core.ScalarStyle.Plain)
                    {
                        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long number))
                        {
                            return JsonValue.Create(number);
                        }

                        if (text is "true" or "True" or "TRUE" or "false" or "False" or "FALSE")
                        {
                            return JsonValue.Create(text.StartsWith('t') || text.StartsWith('T'));
                        }
                    }

                    return JsonValue.Create(text);
            }
        }
    }
}
