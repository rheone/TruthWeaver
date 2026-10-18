namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Tui.Workbench;
using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;

/// <summary>
/// The banner that says what a conversion between rule text, JSON and YAML keeps and what it does not. The tree and the
/// notes always stay. The layout and spelling of the old text do not, and YAML <c>#</c> comments cannot be written in the
/// other formats.
/// </summary>
internal static class ConversionNotice
{
    /// <summary>Describes a conversion.</summary>
    /// <param name="from">The old format.</param>
    /// <param name="to">The new format.</param>
    /// <param name="oldText">The text before the conversion.</param>
    /// <returns>The banner text.</returns>
    public static string Describe(RuleFormat from, RuleFormat to, string oldText)
    {
        ArgumentNullException.ThrowIfNull(oldText);

        string banner =
            $"Converted {Workbench.FormatName(from)} to {Workbench.FormatName(to)}: the tree and the notes are the same.";
        int comments = from == RuleFormat.Yaml && to != RuleFormat.Yaml ? CountYamlComments(oldText) : 0;
        if (comments > 0)
        {
            banner += $" {comments} YAML comment{(comments == 1 ? string.Empty : "s")} cannot be kept.";
        }

        return banner + " The layout and spelling of the old text are not kept. Ctrl+Z undoes it.";
    }

    /// <summary>Counts the <c>#</c> comments of YAML text. A text that is not YAML has none.</summary>
    /// <param name="text">The YAML text.</param>
    /// <returns>The number of comments.</returns>
    public static int CountYamlComments(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        int count = 0;
        try
        {
            using StringReader reader = new(text);
            Scanner scanner = new(reader, skipComments: false);
            while (scanner.MoveNext())
            {
                if (scanner.Current is Comment)
                {
                    count++;
                }
            }
        }
        catch (YamlException)
        {
            // Text that does not scan is not a document to count comments in.
        }

        return count;
    }
}
