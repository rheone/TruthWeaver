namespace TruthWeaver.Tool.Tui.Sessions;

using System.Text.Json.Nodes;
using TruthWeaver.Diagnostics;
using TruthWeaver.Testing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.RuleCases;

/// <summary>
/// The rule test file of the workbench: its path, the name of the case to save, the cases it holds with the outcome of the
/// last run, and the operations on them. It has no view. A case is the current assignment saved in the format of
/// <c>docs/rule-tests.md</c>, so the file runs with <c>truthweaver test</c>.
/// </summary>
internal sealed class RuleTestSession
{
    /// <summary>Gets or sets the path of the rule test file. A relative path is relative to the current folder.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the case that <see cref="SaveCase"/> writes. A case with the same name is replaced.</summary>
    public string CaseName { get; set; } = string.Empty;

    /// <summary>Gets the cases of the file, with the outcome of the last run.</summary>
    public IReadOnlyList<RuleTestRow> Rows { get; private set; } = [];

    /// <summary>Gets the last message about the file, such as the result of a save or why a run did not start. It is empty when there is none.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Reads the cases of the file at <see cref="FilePath"/>. The outcome of an earlier run is dropped.</summary>
    /// <returns><see langword="true"/> when the file was read.</returns>
    public bool Open()
    {
        if (!this.TryReadFile(out string? text))
        {
            return false;
        }

        if (
            !RuleTestCaseText
                .ReadCases(text, this.FolderPath())
                .TryGetValue(out IReadOnlyList<RuleTestCaseSpec>? cases, out EquatableArray<Diagnostic> diagnostics)
        )
        {
            this.Message = Describe(diagnostics, "The file is not a usable rule test file.");
            return false;
        }

        this.Rows = [.. cases.Select(c => new RuleTestRow(c.Name, c, null, null, []))];
        this.Message = $"{cases.Count} case(s) in {this.FilePath}.";
        return true;
    }

    /// <summary>
    /// Saves the assignment as a case. A new file gets the open rule as its inline rule; an existing file keeps its own rule
    /// and its other cases, and a case with the same name is replaced.
    /// </summary>
    /// <param name="assignment">The values and the variables.</param>
    /// <param name="outcome">The outcome of the open rule under the assignment, which gives the expected result.</param>
    /// <param name="ruleText">The open rule as rule text.</param>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool SaveCase(AssignmentSession assignment, AssignmentOutcome outcome, string ruleText)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(ruleText);

        if (string.IsNullOrWhiteSpace(this.FilePath))
        {
            return this.Fail("Give the path of the rule test file first.");
        }

        if (string.IsNullOrWhiteSpace(this.CaseName))
        {
            return this.Fail("Give the case a name first.");
        }

        if (outcome.Decision is not { } decision || !outcome.IsRuleCurrent)
        {
            return this.Fail("Fix the errors of the rule text first: a case needs a rule that compiles.");
        }

        // A given term that the rule does not use fails the case, so only the terms of the open rule are written.
        string[] keys = [.. outcome.Terms.Where(t => t.Key is not null).Select(t => t.Key!)];
        Dictionary<string, TruthValue> given = assignment
            .Values.Where(v => keys.Contains(v.Key))
            .ToDictionary(v => v.Key, v => v.Value);
        JsonObject variables = [];
        if (outcome.Terms.Any(t => t.HasDataReference))
        {
            if (!AssignmentVariables.Read(assignment.VariablesText).TryGetValue(out JsonObject? data, out _))
            {
                return this.Fail("Fix the variables first: they are not well-formed.");
            }

            variables = data;
        }

        RuleTestCaseSpec spec = new(this.CaseName.Trim(), given, variables, decision.Result, decision.Faults.Count);
        string path = this.FullPath();
        string text;
        if (File.Exists(path))
        {
            if (!this.TryReadFile(out string? existing))
            {
                return false;
            }

            if (
                !RuleTestCaseText
                    .AddOrReplaceCase(existing, spec)
                    .TryGetValue(out string? edited, out EquatableArray<Diagnostic> diagnostics)
            )
            {
                return this.Fail(Describe(diagnostics, "The file cannot take the case."));
            }

            text = edited;
        }
        else
        {
            text = RuleTestCaseText.NewFile(ruleText, spec);
        }

        if (!OutputWriter.WriteFile(path, text).TryGetValue(out _, out EquatableArray<Diagnostic> writeDiagnostics))
        {
            return this.Fail(Describe(writeDiagnostics, $"Cannot write '{path}'."));
        }

        _ = this.Open();
        this.Message = $"Saved the case '{spec.Name}' to {this.FilePath}.";
        return true;
    }

    /// <summary>Runs the cases of the file on the open rule, and records the outcome of each case.</summary>
    /// <param name="document">The open document. Its text must compile.</param>
    /// <param name="settings">The settings (limits) for compiling the rule.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns><see langword="true"/> when the cases ran; the cases may still fail.</returns>
    public async Task<bool> RunAsync(
        RuleDocument document,
        ToolSettings settings,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);

        if (document.Rule is not { } rule || !document.IsRuleCurrent)
        {
            return this.Fail("Fix the errors of the rule text first: the cases run on a rule that compiles.");
        }

        if (!this.TryReadFile(out string? fileText))
        {
            return false;
        }

        // The runner reads the rule from the file, so the file text is run with the open rule in place of its own.
        if (
            !RuleTestCaseText
                .WithRule(fileText, rule.CanonicalText)
                .TryGetValue(out string? derived, out EquatableArray<Diagnostic> diagnostics)
        )
        {
            return this.Fail(Describe(diagnostics, "The file is not a usable rule test file."));
        }

        if (
            !RuleTestFile.TryParse(
                derived,
                this.FolderPath(),
                out RuleTestFile? file,
                out IReadOnlyList<RuleTestFileError> errors
            )
        )
        {
            return this.Fail(errors[0].ToString());
        }

        RuleTestReport report = await file.RunAsync(settings.ToCompilerOptions(), cancellationToken).ConfigureAwait(false);
        Dictionary<string, RuleTestCaseSpec> specs =
            RuleTestCaseText.ReadCases(fileText, this.FolderPath()).Value?.ToDictionary(c => c.Name, StringComparer.Ordinal)
            ?? [];
        this.Rows = [.. report.Results.Select(result => RowOf(result, specs))];
        this.Message = $"{report.Results.Count - report.Failures.Count} of {report.Results.Count} case(s) passed.";
        return true;
    }

    /// <summary>Loads the assignment of a case into the assignment, and makes the case the one to save over.</summary>
    /// <param name="index">The index in <see cref="Rows"/>.</param>
    /// <param name="assignment">The assignment to replace.</param>
    /// <returns><see langword="true"/> when the case was loaded.</returns>
    public bool LoadCase(int index, AssignmentSession assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        if (index < 0 || index >= this.Rows.Count)
        {
            return this.Fail("Select a case first.");
        }

        RuleTestCaseSpec spec = this.Rows[index].Spec;
        assignment.Load(
            spec.Given,
            spec.Variables.Count == 0 ? null : AssignmentVariables.ToYamlBlock(spec.Variables, 0) + "\n"
        );
        this.CaseName = spec.Name;
        this.Message = $"Loaded the case '{spec.Name}'.";
        return true;
    }

    /// <summary>Removes a case from the file. The last case of a file stays.</summary>
    /// <param name="index">The index in <see cref="Rows"/>.</param>
    /// <returns><see langword="true"/> when the case was removed.</returns>
    public bool DeleteCase(int index)
    {
        if (index < 0 || index >= this.Rows.Count)
        {
            return this.Fail("Select a case first.");
        }

        string name = this.Rows[index].Name;
        if (!this.TryReadFile(out string? text))
        {
            return false;
        }

        if (
            !RuleTestCaseText.RemoveCase(text, name).TryGetValue(out string? edited, out EquatableArray<Diagnostic> diagnostics)
        )
        {
            return this.Fail(Describe(diagnostics, "The case cannot be removed."));
        }

        if (
            !OutputWriter.WriteFile(this.FullPath(), edited).TryGetValue(out _, out EquatableArray<Diagnostic> writeDiagnostics)
        )
        {
            return this.Fail(Describe(writeDiagnostics, "Cannot write the file."));
        }

        _ = this.Open();
        this.Message = $"Removed the case '{name}'.";
        return true;
    }

    // The case as the file holds it, so it can be loaded; a case the file reader skipped keeps what the runner knows.
    private static RuleTestRow RowOf(RuleTestResult result, Dictionary<string, RuleTestCaseSpec> specs)
    {
        RuleTestCaseSpec spec =
            specs.GetValueOrDefault(result.Case.Name)
            ?? new RuleTestCaseSpec(
                result.Case.Name,
                new Dictionary<string, TruthValue>(),
                [],
                result.Case.Expect.Result,
                result.Case.Expect.Faults
            );
        (TruthValue, int)? actual = result.Decision is { } decision ? (decision.Result, decision.Faults.Count) : null;
        return new RuleTestRow(result.Case.Name, spec, result.Passed, actual, result.Failures);
    }

    private static string Describe(EquatableArray<Diagnostic> diagnostics, string fallback)
    {
        Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        return first is null ? fallback : first.Message;
    }

    private string FullPath()
    {
        return Path.GetFullPath(this.FilePath);
    }

    private string FolderPath()
    {
        return Path.GetDirectoryName(this.FullPath()) ?? Environment.CurrentDirectory;
    }

    private bool Fail(string message)
    {
        this.Message = message;
        return false;
    }

    private bool TryReadFile([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? text)
    {
        text = null;
        if (string.IsNullOrWhiteSpace(this.FilePath))
        {
            return this.Fail("Give the path of the rule test file first.");
        }

        if (
            !SourceReader
                .ReadFile(this.FullPath())
                .TryGetValue(out SourceText? source, out EquatableArray<Diagnostic> diagnostics)
        )
        {
            return this.Fail(Describe(diagnostics, $"Cannot read '{this.FilePath}'."));
        }

        text = source.Text;
        return true;
    }
}
