namespace TruthWeaver.Tool;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>
/// Collects what one command found (its text, its JSON result, its diagnostics and its own failure) and writes it in the
/// output mode of the command, then gives the exit code. Every command ends here, so the output shape and the exit codes
/// are the same for each command.
/// </summary>
/// <param name="command">The command name, as the JSON envelope gives it.</param>
internal sealed class CommandOutcome(string command)
{
    private readonly List<DiagnosticDocument> documents = [];

    // Diagnostics that the text of the command already shows (the validation report), so text mode does not repeat them
    // on standard error. They still count for the exit code and are still in a SARIF log.
    private readonly List<DiagnosticDocument> shownInText = [];

    /// <summary>Gets the command name.</summary>
    public string Command { get; } = command;

    /// <summary>Gets the command-specific members of the JSON envelope's <c>result</c> object.</summary>
    public JsonObject Result { get; } = [];

    /// <summary>Gets or sets the text that text mode writes to standard output, or <see langword="null"/> for none.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command failed for a reason that is not a diagnostic: a <c>--check</c>
    /// found a difference, or <c>diff</c> found that the rules are not equivalent. The exit code is then at least 1.
    /// </summary>
    public bool Failed { get; set; }

    /// <summary>Gets or sets a value indicating whether a warning fails the command (<c>--strict</c>).</summary>
    public bool Strict { get; set; }

    /// <summary>Gets a value indicating whether an error was recorded, so the command can stop early.</summary>
    public bool HasErrors =>
        this.documents.Concat(this.shownInText).SelectMany(d => d.Diagnostics).Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>Gets the exit code from the diagnostics and <see cref="Failed"/>. Unusable (2) wins over failed (1).</summary>
    public ToolExitCode ExitCode
    {
        get
        {
            ToolExitCode fromDiagnostics = SeverityPolicy.ExitCodeFor(
                this.documents.Concat(this.shownInText).SelectMany(d => d.Diagnostics),
                this.Strict
            );
            return fromDiagnostics == ToolExitCode.Success && this.Failed ? ToolExitCode.Failed : fromDiagnostics;
        }
    }

    /// <summary>Writes a JSON node as indented JSON with line feeds, without the reflection serializer (trimming and Native AOT safe).</summary>
    /// <param name="node">The node.</param>
    /// <returns>The JSON text.</returns>
    public static string FormatJson(JsonNode node)
    {
        using MemoryStream stream = new();
        using (
            Utf8JsonWriter writer = new(
                stream,
                new JsonWriterOptions
                {
                    Indented = true,
                    NewLine = "\n",
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                }
            )
        )
        {
            node.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads JSON that a tool core writer made, so that it can be put inside the envelope.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The node.</returns>
    public static JsonNode ParseJson(string json)
    {
        return JsonNode.Parse(json) ?? throw new InvalidOperationException("The tool core wrote JSON null.");
    }

    /// <summary>Adds the diagnostics of one input. A document with no diagnostics is left out.</summary>
    /// <param name="document">The input and its diagnostics.</param>
    public void Add(DiagnosticDocument document)
    {
        if (document.Diagnostics.Count > 0)
        {
            this.documents.Add(document);
        }
    }

    /// <summary>Adds diagnostics that have no rule text to point into, such as those of a configuration file.</summary>
    /// <param name="origin">The file the diagnostics are about.</param>
    /// <param name="diagnostics">The diagnostics.</param>
    public void Add(string origin, IEnumerable<Diagnostic> diagnostics)
    {
        this.Add(new DiagnosticDocument(origin, null, null, new EquatableArray<Diagnostic>(diagnostics)));
    }

    /// <summary>Adds diagnostics of a loaded rule, so that text mode shows the source line of each span.</summary>
    /// <param name="rule">The rule the spans point into.</param>
    /// <param name="diagnostics">The diagnostics.</param>
    public void Add(LoadedRule rule, IEnumerable<Diagnostic> diagnostics)
    {
        this.Add(DiagnosticDocument.For(rule, diagnostics));
    }

    /// <summary>Adds diagnostics that <see cref="Text"/> already shows. They count for the exit code and the SARIF log only.</summary>
    /// <param name="document">The input and its diagnostics.</param>
    public void AddShownInText(DiagnosticDocument document)
    {
        this.shownInText.Add(document);
    }

    /// <summary>Writes the outcome in the output mode, and gives the exit code.</summary>
    /// <param name="console">The console.</param>
    /// <param name="mode">The output mode.</param>
    /// <returns>The exit code.</returns>
    public int Write(CliConsole console, OutputMode mode)
    {
        ToolExitCode exitCode = this.ExitCode;
        switch (mode)
        {
            case OutputMode.Json:
                OutputWriter.Write(console.Out, this.Envelope(exitCode));
                break;
            case OutputMode.Sarif:
                OutputWriter.Write(
                    console.Out,
                    DiagnosticWriter.Format([.. this.documents, .. this.shownInText], DiagnosticOutputFormat.Sarif)
                );
                break;
            default:
                if (this.Text is { } text)
                {
                    OutputWriter.Write(console.Out, text);
                }

                if (this.documents.Count > 0)
                {
                    OutputWriter.Write(console.Error, DiagnosticWriter.Format(this.documents, DiagnosticOutputFormat.Text));
                }

                break;
        }

        return (int)exitCode;
    }

    private string Envelope(ToolExitCode exitCode)
    {
        JsonObject diagnostics = ParseJson(DiagnosticWriter.Format(this.documents, DiagnosticOutputFormat.Json)).AsObject();
        JsonObject envelope = new()
        {
            ["command"] = this.Command,
            ["exitCode"] = (int)exitCode,
            ["diagnostics"] = diagnostics["diagnostics"]?.DeepClone(),
            ["result"] = this.Result.DeepClone(),
        };
        return FormatJson(envelope);
    }
}
