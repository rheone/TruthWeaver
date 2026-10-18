namespace TruthWeaver.Tool.Core.Loading;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Documents;

/// <summary>Text that was compiled without a document, ready for <see cref="RuleDocument.SetText(CheckedText)"/>.</summary>
/// <param name="Text">The text that was compiled.</param>
/// <param name="Format">The format that the text was compiled as.</param>
/// <param name="Result">The result of the compilation.</param>
public sealed record CheckedText(string Text, RuleFormat Format, CompilationResult<StubAssignment> Result);
