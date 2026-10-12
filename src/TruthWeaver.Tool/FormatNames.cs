namespace TruthWeaver.Tool;

using TruthWeaver.Compilation;

/// <summary>The lower-case names of the rule formats, as the options take them and the JSON output writes them.</summary>
internal static class FormatNames
{
    /// <summary>Gets the name of a format: <c>text</c>, <c>json</c> or <c>yaml</c>.</summary>
    /// <param name="format">The format.</param>
    /// <returns>The name.</returns>
    public static string Of(RuleFormat format)
    {
        return format switch
        {
            RuleFormat.Json => "json",
            RuleFormat.Yaml => "yaml",
            _ => "text",
        };
    }
}
