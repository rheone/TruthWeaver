namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;

/// <summary>
/// Loads a rule from a file, a stream (such as standard input) or a string: reads and decodes the input
/// (<see cref="SourceReader"/>), finds its format (<see cref="FormatDetector"/>), and, with
/// <see cref="RuleLoadOptions.Path"/>, selects one rule inside a JSON or YAML document. Every failure is a diagnostic in
/// the result; nothing here throws for a bad input.
/// </summary>
public static class RuleLoader
{
    /// <summary>Loads a rule from a file. The file extension takes part in format detection.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleLoadOptions.Default"/>.</param>
    /// <returns>The loaded rule, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<LoadedRule> LoadFile(string path, RuleLoadOptions? options = null)
    {
        return Load(SourceReader.ReadFile(path), path, options);
    }

    /// <summary>Loads a rule from a stream, such as standard input. The stream stays open.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="origin">The name to report the input by; <see cref="SourceReader.StandardInput"/> for standard input.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleLoadOptions.Default"/>.</param>
    /// <returns>The loaded rule, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="origin"/> is <see langword="null"/>.</exception>
    public static ToolResult<LoadedRule> LoadStream(
        Stream stream,
        string origin = SourceReader.StandardInput,
        RuleLoadOptions? options = null
    )
    {
        // Standard input has no file name, so only the content decides its format.
        return Load(SourceReader.ReadStream(stream, origin), null, options);
    }

    /// <summary>Loads a rule from a string. When <paramref name="origin"/> ends in a known extension, it takes part in format detection.</summary>
    /// <param name="text">The text.</param>
    /// <param name="origin">The name to report the input by.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleLoadOptions.Default"/>.</param>
    /// <returns>The loaded rule, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="origin"/> is <see langword="null"/>.</exception>
    public static ToolResult<LoadedRule> LoadString(string text, string origin = "<string>", RuleLoadOptions? options = null)
    {
        return Load(SourceReader.ReadString(text, origin), origin, options);
    }

    private static ToolResult<LoadedRule> Load(ToolResult<SourceText> read, string? fileName, RuleLoadOptions? options)
    {
        if (!read.TryGetValue(out SourceText? source, out EquatableArray<Diagnostics.Diagnostic> diagnostics))
        {
            return ToolResults.Failure<LoadedRule>(diagnostics);
        }

        options ??= RuleLoadOptions.Default;
        FormatDetection detection = FormatDetector.Detect(source.Text, fileName, options.Format);
        if (options.Path is null)
        {
            return ToolResults.Success(new LoadedRule(source, detection, null, detection.Format, source.Text));
        }

        if (!DocumentPath.TryParse(options.Path, out DocumentPath? path, out string? problem))
        {
            return ToolResults.Failure<LoadedRule>(ToolDiagnosticCodes.InvalidPath, problem);
        }

        if (detection.Format == RuleFormat.Text)
        {
            return ToolResults.Failure<LoadedRule>(
                ToolDiagnosticCodes.NotARule,
                $"The input '{source.Origin}' is rule text, which has no paths. A path selects a rule inside a JSON or YAML document.",
                path.Text
            );
        }

        ToolResult<RuleSelection> selected = EmbeddedRuleReader.Select(source.Text, detection.Format, path);
        return selected.TryGetValue(out RuleSelection? selection, out diagnostics)
            ? ToolResults.Success(new LoadedRule(source, detection, path.Text, selection.Format, selection.Text))
            : ToolResults.Failure<LoadedRule>(diagnostics);
    }
}
