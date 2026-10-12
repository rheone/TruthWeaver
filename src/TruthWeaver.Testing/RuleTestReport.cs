namespace TruthWeaver.Testing;

using System.Diagnostics;
using System.Xml;
using TruthWeaver.Diagnostics;

/// <summary>The result of a run of a <see cref="RuleTestFile"/>: one result for each case, and the diagnostics of the rule.</summary>
public sealed class RuleTestReport
{
    /// <summary>Initializes a new instance of the <see cref="RuleTestReport"/> class.</summary>
    /// <param name="name">The name of the test file.</param>
    /// <param name="ruleDiagnostics">The diagnostics of the rule.</param>
    /// <param name="results">The result of each case, in file order.</param>
    internal RuleTestReport(string name, EquatableArray<Diagnostic> ruleDiagnostics, IReadOnlyList<RuleTestResult> results)
    {
        this.Name = name;
        this.RuleDiagnostics = ruleDiagnostics;
        this.Results = results;
        this.Failures = [.. results.Where(r => !r.Passed)];
    }

    /// <summary>Gets the name of the test file.</summary>
    public string Name { get; }

    /// <summary>Gets the diagnostics of the rule: the errors when it does not compile, and any warnings when it does.</summary>
    public EquatableArray<Diagnostic> RuleDiagnostics { get; }

    /// <summary>Gets the result of each case, in file order.</summary>
    public IReadOnlyList<RuleTestResult> Results { get; }

    /// <summary>Gets the results of the cases that failed.</summary>
    public IReadOnlyList<RuleTestResult> Failures { get; }

    /// <summary>Gets a value indicating whether every case passed.</summary>
    public bool Passed => this.Failures.Count == 0;

    /// <summary>Throws when a case failed, so that a test fails with the list of failures.</summary>
    /// <exception cref="RuleTestException">One or more cases failed. The message is the report text.</exception>
    [StackTraceHidden]
    public void ShouldPass()
    {
        if (!this.Passed)
        {
            throw new RuleTestException(this.ToString());
        }
    }

    /// <summary>
    /// Writes the report as JUnit XML for a CI server: one <c>testsuite</c> named after the file, with one <c>testcase</c>
    /// for each case. A failed expectation is a <c>failure</c>. A rule that does not compile is an <c>error</c> in each case,
    /// with the error diagnostics. The XML holds no time, so the same run gives the same bytes.
    /// </summary>
    /// <returns>The XML, with a UTF-8 declaration, LF line endings and a final line feed.</returns>
    public string PrintJUnit()
    {
        int errors = this.Results.Count(r => !r.RuleCompiles);
        string tests = this.Results.Count.ToString(CultureInfo.InvariantCulture);
        string failureCount = (this.Failures.Count - errors).ToString(CultureInfo.InvariantCulture);
        string errorCount = errors.ToString(CultureInfo.InvariantCulture);

        using MemoryStream stream = new();
        XmlWriterSettings settings = new()
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
        };
        using (XmlWriter xml = XmlWriter.Create(stream, settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("testsuites");
            xml.WriteAttributeString("name", this.Name);
            xml.WriteAttributeString("tests", tests);
            xml.WriteAttributeString("failures", failureCount);
            xml.WriteAttributeString("errors", errorCount);
            xml.WriteStartElement("testsuite");
            xml.WriteAttributeString("name", this.Name);
            xml.WriteAttributeString("tests", tests);
            xml.WriteAttributeString("failures", failureCount);
            xml.WriteAttributeString("errors", errorCount);
            xml.WriteAttributeString("skipped", "0");
            foreach (RuleTestResult result in this.Results)
            {
                this.WriteCase(xml, result);
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    /// <summary>
    /// Returns the report text: each case with <c>PASS</c> or <c>FAIL</c> and the reasons, then a count line such as
    /// <c>3 case(s): 2 passed, 1 failed.</c>.
    /// </summary>
    /// <returns>The text, with LF line endings.</returns>
    public override string ToString()
    {
        StringBuilder text = new();
        foreach (RuleTestResult result in this.Results)
        {
            text.Append(result).Append('\n');
        }

        text.Append(
            CultureInfo.InvariantCulture,
            $"{this.Results.Count} case(s): {this.Results.Count - this.Failures.Count} passed, {this.Failures.Count} failed."
        );
        return text.ToString();
    }

    private void WriteCase(XmlWriter xml, RuleTestResult result)
    {
        xml.WriteStartElement("testcase");
        xml.WriteAttributeString("name", result.Case.Name);
        xml.WriteAttributeString("classname", this.Name);
        if (!result.RuleCompiles)
        {
            xml.WriteStartElement("error");
            xml.WriteAttributeString("message", "The rule does not compile.");
            xml.WriteAttributeString("type", "RuleDoesNotCompile");
            xml.WriteString(
                string.Join(
                    "\n",
                    this.RuleDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => DiagnosticFormatter.Format(d))
                )
            );
            xml.WriteEndElement();
        }
        else if (!result.Passed)
        {
            // The first line of the first reason is the short message; the element text has every reason in full.
            xml.WriteStartElement("failure");
            xml.WriteAttributeString("message", result.Failures[0].Split('\n')[0]);
            xml.WriteAttributeString("type", "ExpectationFailed");
            xml.WriteString(string.Join("\n", result.Failures));
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }
}
