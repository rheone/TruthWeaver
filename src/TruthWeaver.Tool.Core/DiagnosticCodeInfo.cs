namespace TruthWeaver.Tool.Core;

using TruthWeaver.Diagnostics;

/// <summary>One row of the <see cref="DiagnosticCodeTable"/>: a diagnostic code and what it means.</summary>
/// <param name="Code">The code, such as <c>TRE0001</c> or <c>TWT0003</c>. It is the rule id in SARIF output.</param>
/// <param name="Name">The name of the constant that holds the code, such as <c>SyntaxError</c>.</param>
/// <param name="DefaultSeverity">The severity that the code usually has.</param>
/// <param name="Description">A one-sentence description of the problem.</param>
public sealed record DiagnosticCodeInfo(string Code, string Name, DiagnosticSeverity DefaultSeverity, string Description);
