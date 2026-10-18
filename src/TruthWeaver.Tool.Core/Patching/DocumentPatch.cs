namespace TruthWeaver.Tool.Core.Patching;

using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;

/// <summary>A document with edited rules written back into it by <see cref="RulePatcher"/>.</summary>
/// <param name="Text">
/// The new text of the document. It keeps the line endings of the document; write it with
/// <see cref="RulePatcher.WriteFile"/>, not with <see cref="OutputWriter"/>, which changes line endings.
/// </param>
/// <param name="Encoding">The encoding of the original document, which <see cref="RulePatcher.Encode"/> writes again.</param>
/// <param name="Rules">One outcome for each edit, in the order of the edits.</param>
public sealed record DocumentPatch(string Text, SourceEncoding Encoding, EquatableArray<RulePatchOutcome> Rules);
