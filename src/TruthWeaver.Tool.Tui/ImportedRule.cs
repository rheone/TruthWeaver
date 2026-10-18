namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;

/// <summary>A document that <see cref="RuleImport"/> made, and what decided its format.</summary>
/// <param name="Document">The document.</param>
/// <param name="Detection">The format and the evidence for it: an override, the extension or the content.</param>
internal sealed record ImportedRule(RuleDocument Document, FormatDetection Detection);
