namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Diagnostics;
using TruthWeaver.Yaml;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>
/// Loads data files for the <c>from("source", "query")</c> references of a rule. A data file is a JSON or YAML document;
/// the file extension, then the content, decides which. The document becomes a <see cref="JsonDataSource"/> or a
/// <see cref="YamlDataSource"/>, which both answer JSONPath queries.
/// </summary>
public static class DataSourceFiles
{
    /// <summary>Loads one data file.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="format">The format of the file, or <see langword="null"/> to detect it.</param>
    /// <returns>The data source, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<IDataSource> Load(string path, RuleFormat? format = null)
    {
        ToolResult<SourceText> read = SourceReader.ReadFile(path);
        if (!read.TryGetValue(out SourceText? source, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<IDataSource>(diagnostics);
        }

        return Parse(source.Text, FormatDetector.DetectDocument(source.Text, path, format));
    }

    /// <summary>Reads a data document from text.</summary>
    /// <param name="text">The JSON or YAML text.</param>
    /// <param name="format"><see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>; <see cref="RuleFormat.Text"/> is read as YAML.</param>
    /// <returns>The data source, or a <see cref="ToolDiagnosticCodes.MalformedDocument"/> error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static ToolResult<IDataSource> Parse(string text, RuleFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);

        // The library readers report only the position of a problem, never document content, which may be sensitive.
        if (format == RuleFormat.Json)
        {
            return JsonDataSource.TryParse(text, out JsonDataSource? json, out string? jsonError)
                ? ToolResults.Success<IDataSource>(json)
                : ToolResults.Failure<IDataSource>(ToolDiagnosticCodes.MalformedDocument, jsonError);
        }

        return YamlDataSource.TryParse(text, out YamlDataSource? yaml, out string? yamlError)
            ? ToolResults.Success<IDataSource>(yaml)
            : ToolResults.Failure<IDataSource>(ToolDiagnosticCodes.MalformedDocument, yamlError);
    }

    /// <summary>Loads several data files, each under its source name, in the order given.</summary>
    /// <param name="files">The source name and file path of each file. A name is case-sensitive, as in <c>from("name", ...)</c>.</param>
    /// <returns>The data sources, or every diagnostic of every file that failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public static ToolResult<DataSourceSet> LoadAll(IEnumerable<KeyValuePair<string, string>> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        DataSourceSet sources = [];
        List<Diagnostic> diagnostics = [];
        foreach ((string name, string path) in files)
        {
            if (sources.TryGet(name, out _))
            {
                diagnostics.Add(
                    ToolResults.Error(
                        ToolDiagnosticCodes.DuplicateDataSource,
                        $"The data source name '{name}' is given more than once."
                    )
                );
                continue;
            }

            ToolResult<IDataSource> loaded = Load(path);
            diagnostics.AddRange(loaded.Diagnostics);
            if (loaded.Succeeded)
            {
                sources.Add(name, loaded.Value);
            }
        }

        return diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error)
            ? ToolResults.Failure<DataSourceSet>(diagnostics)
            : ToolResults.Success(sources, diagnostics);
    }
}
