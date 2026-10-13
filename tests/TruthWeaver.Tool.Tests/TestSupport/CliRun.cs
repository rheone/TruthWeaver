namespace TruthWeaver.Tool.Tests.TestSupport;

using System.Text.Json.Nodes;

/// <summary>The outcome of one run of the command line.</summary>
/// <param name="ExitCode">The exit code.</param>
/// <param name="Output">Standard output.</param>
/// <param name="Error">Standard error.</param>
internal sealed record CliRun(int ExitCode, string Output, string Error)
{
    /// <summary>Gets standard output read as the JSON envelope of <c>--json</c>.</summary>
    public JsonObject Json => JsonNode.Parse(this.Output)!.AsObject();

    /// <summary>Gets the codes of the diagnostics in the JSON envelope.</summary>
    public IReadOnlyList<string> DiagnosticCodes =>
        [.. this.Json["diagnostics"]!.AsArray().Select(d => d!["code"]!.GetValue<string>())];
}
