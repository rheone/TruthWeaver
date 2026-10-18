namespace TruthWeaver.Tool.Core.Patching;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Documents;
using YamlDotNet.Core;

/// <summary>
/// The in-place half of <see cref="RulePatcher"/>: finds the values that differ between the rule in the document and the
/// edited rule, and replaces each one in the document text, so nothing else changes.
/// </summary>
internal static class ValuePatch
{
    /// <summary>Replaces the changed values of a tree-format rule in its document.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="format">The document format, <see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</param>
    /// <param name="rulePath">The normalized path of the rule, <c>$</c> for the whole document.</param>
    /// <param name="before">The tree-format JSON, with notes, of the rule in the document.</param>
    /// <param name="after">The tree-format JSON, with notes, of the edited rule.</param>
    /// <param name="reason">Why the values cannot be patched in place, when this method returns <see langword="null"/>.</param>
    /// <returns>The patched document, or <see langword="null"/> when the change does not fit the values in place.</returns>
    public static string? TryApply(
        string document,
        RuleFormat format,
        string rulePath,
        string before,
        string after,
        out string? reason
    )
    {
        List<(string Path, JsonNode? Value)> changes = [];
        if (!Compare(JsonNode.Parse(before), JsonNode.Parse(after), rulePath, changes))
        {
            reason = "its structure changed: a node, a key or an operand was added or removed, or a value changed its kind";
            return null;
        }

        SourceNodeIndex index = SourceNodeIndex.Create(document, format);
        List<(int Start, int End, string Text)> replacements = [];
        foreach ((string path, JsonNode? value) in changes)
        {
            // A block scalar ends with its line breaks and a shared value changes its aliases too, so neither is replaced.
            if (
                index.Find(path) is not { Kind: SourceNodeKind.Scalar, IsShared: false } node
                || node.Style is ScalarStyle.Literal or ScalarStyle.Folded
            )
            {
                reason = $"the value at '{path}' is not written as a single value that can be replaced in place";
                return null;
            }

            string text = format == RuleFormat.Json ? PatchText.JsonScalar(value) : PatchText.YamlScalar(value, node.Style);
            replacements.Add((node.Start, node.End, text));
        }

        reason = null;
        return PatchText.Replace(document, replacements);
    }

    // Collects the scalar values that differ, by path. Returns false when the two trees do not have the same shape: an
    // object with other keys, an array of another length, or a value that changed between a container and a scalar.
    private static bool Compare(JsonNode? before, JsonNode? after, string path, List<(string Path, JsonNode? Value)> changes)
    {
        switch (before, after)
        {
            case (JsonObject old, JsonObject edited):
                if (old.Count != edited.Count)
                {
                    return false;
                }

                foreach ((string key, JsonNode? value) in edited)
                {
                    string child = path + DocumentPath.Segment.Key(key).Text;
                    if (!old.TryGetPropertyValue(key, out JsonNode? oldValue) || !Compare(oldValue, value, child, changes))
                    {
                        return false;
                    }
                }

                return true;

            case (JsonArray old, JsonArray edited):
                if (old.Count != edited.Count)
                {
                    return false;
                }

                for (int i = 0; i < edited.Count; i++)
                {
                    if (!Compare(old[i], edited[i], path + DocumentPath.Segment.Index(i).Text, changes))
                    {
                        return false;
                    }
                }

                return true;

            case (JsonObject or JsonArray, _):
            case (_, JsonObject or JsonArray):
                return false;

            default:
                if (!JsonNode.DeepEquals(before, after))
                {
                    changes.Add((path, after));
                }

                return true;
        }
    }
}
