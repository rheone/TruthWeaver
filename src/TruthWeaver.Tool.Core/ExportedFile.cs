namespace TruthWeaver.Tool.Core;

/// <summary>One text file of an export of every artifact, before it is written.</summary>
/// <param name="FileName">The file name in the export folder.</param>
/// <param name="Text">The text. <see cref="OutputWriter"/> writes it with line feeds and a final newline.</param>
public sealed record ExportedFile(string FileName, string Text);
