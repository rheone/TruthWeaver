namespace TruthWeaver.Tests;

using System.Text;
using TruthWeaver.Tests.DocExamples;

/// <summary>
/// Guards the encoding of the repository's text files: each Markdown, MSBuild, C#, JSON and YAML file must be valid UTF-8
/// and must not start with a byte order mark. A stray Windows-1252 byte (for example 0x85, an ellipsis) makes a file
/// unreadable to tools that require UTF-8.
/// </summary>
public sealed class TextFileEncodingTests
{
    private static readonly string[] TextExtensions =
    [
        ".md",
        ".props",
        ".targets",
        ".csproj",
        ".slnx",
        ".cs",
        ".json",
        ".yaml",
        ".yml",
    ];

    private static readonly string[] SkippedDirectories =
    [
        ".git",
        ".tmp",
        ".codegraph",
        ".vs",
        "bin",
        "obj",
        "artifacts",
        "TestResults",
        "node_modules",
    ];

    /// <summary>Every text file in the repository is valid UTF-8 without a byte order mark.</summary>
    [Fact]
    public void Check_RepositoryTextFiles_AreValidUtf8WithoutByteOrderMark_Test()
    {
        string root = DocExampleChecker.FindRepositoryRoot();
        IEnumerable<(string Path, byte[] Bytes)> files = EnumerateTextFiles(root)
            .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllBytes(path)));

        IReadOnlyList<string> failures = Check(files);

        Assert.Empty(failures);
    }

    /// <summary>A file with a stray Windows-1252 byte is reported, with the offset of the byte.</summary>
    [Fact]
    public void Check_FileWithInvalidUtf8Byte_ReportsTheFileAndOffset_Test()
    {
        byte[] bytes = [.. "HasCount"u8, 0x85, .. " verb"u8];

        IReadOnlyList<string> failures = Check([("docs/page.md", bytes)]);

        string failure = Assert.Single(failures);
        Assert.Contains("docs/page.md", failure, StringComparison.Ordinal);
        Assert.Contains("not valid UTF-8", failure, StringComparison.Ordinal);
        Assert.Contains("offset 8", failure, StringComparison.Ordinal);
    }

    /// <summary>A file that starts with a UTF-8 byte order mark is reported.</summary>
    [Fact]
    public void Check_FileWithByteOrderMark_IsReported_Test()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. "text"u8];

        IReadOnlyList<string> failures = Check([("src/File.cs", bytes)]);

        string failure = Assert.Single(failures);
        Assert.Contains("src/File.cs", failure, StringComparison.Ordinal);
        Assert.Contains("byte order mark", failure, StringComparison.Ordinal);
    }

    /// <summary>Valid UTF-8, including multi-byte characters such as an ellipsis, passes.</summary>
    [Fact]
    public void Check_ValidUtf8WithMultiByteCharacters_ReportsNothing_Test()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HasCount… and é and ⊤");

        IReadOnlyList<string> failures = Check([("CHANGELOG.md", bytes)]);

        Assert.Empty(failures);
    }

    /// <summary>Checks each file and returns one message per problem.</summary>
    private static List<string> Check(IEnumerable<(string Path, byte[] Bytes)> files)
    {
        List<string> failures = [];
        foreach ((string path, byte[] bytes) in files)
        {
            if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
            {
                failures.Add($"{path}: starts with a UTF-8 byte order mark. Save the file as UTF-8 without a byte order mark.");
            }

            if (!System.Text.Unicode.Utf8.IsValid(bytes))
            {
                failures.Add(
                    $"{path}: not valid UTF-8 at offset {FirstInvalidOffset(bytes)}. Replace the stray byte with the intended character."
                );
            }
        }

        return failures;
    }

    private static int FirstInvalidOffset(byte[] bytes)
    {
        // The decoder exception carries no offset, so scan rune by rune until the first invalid sequence.
        ReadOnlySpan<byte> remaining = bytes;
        int offset = 0;
        while (!remaining.IsEmpty)
        {
            System.Buffers.OperationStatus status = Rune.DecodeFromUtf8(remaining, out _, out int consumed);
            if (status != System.Buffers.OperationStatus.Done)
            {
                return offset;
            }

            offset += consumed;
            remaining = remaining[consumed..];
        }

        return offset;
    }

    private static IEnumerable<string> EnumerateTextFiles(string directory)
    {
        foreach (string file in Directory.EnumerateFiles(directory))
        {
            if (TextExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }

        foreach (string child in Directory.EnumerateDirectories(directory))
        {
            if (!SkippedDirectories.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
            {
                foreach (string file in EnumerateTextFiles(child))
                {
                    yield return file;
                }
            }
        }
    }
}
