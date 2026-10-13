namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class OutputWriterTests
{
    /// <summary>Normalizing turns every line ending into LF and ends non-empty text with exactly one LF.</summary>
    /// <param name="text">The input.</param>
    /// <param name="expected">The normalized output.</param>
    [Theory]
    [InlineData("a\r\nb", "a\nb\n")]
    [InlineData("a\rb\n\n\n", "a\nb\n")]
    [InlineData("a\nb\n", "a\nb\n")]
    [InlineData("", "")]
    [InlineData("\r\n\n", "")]
    public void Normalize_LineEndings_GivesLfAndOneFinalNewline_Test(string text, string expected)
    {
        Assert.Equal(expected, OutputWriter.Normalize(text));
    }

    /// <summary>A written file holds the normalized text as UTF-8 with no byte order mark, the same bytes on each write.</summary>
    [Fact]
    public void WriteFile_CrlfText_WritesUtf8LfBytesDeterministically_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("out/rule.json");

        ToolResult<string> first = OutputWriter.WriteFile(path, "{\r\n  \"predicate\": \"é\"\r\n}");
        byte[] firstBytes = File.ReadAllBytes(path);
        OutputWriter.WriteFile(path, "{\r\n  \"predicate\": \"é\"\r\n}");

        Assert.True(first.Succeeded);
        Assert.Equal(Encoding.UTF8.GetBytes("{\n  \"predicate\": \"é\"\n}\n"), firstBytes);
        Assert.Equal(firstBytes, File.ReadAllBytes(path));
    }

    /// <summary>A file written by the writer checks as identical to the same output.</summary>
    [Fact]
    public void Check_FileWrittenByWriter_IsIdentical_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.yaml");
        OutputWriter.WriteFile(path, "op: and\r\n");

        ToolResult<OutputCheck> result = OutputWriter.Check(path, "op: and");

        Assert.True(result.Succeeded);
        Assert.True(result.Value.IsIdentical);
        Assert.Null(result.Value.FirstDifferentLine);
    }

    /// <summary>A file with other content differs, and the check names the first line that differs.</summary>
    [Fact]
    public void Check_DifferentContent_DiffersAtFirstChangedLine_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.yaml");
        File.WriteAllText(path, "op: and\noperands:\n  - predicate: a\n");

        ToolResult<OutputCheck> result = OutputWriter.Check(path, "op: and\noperands:\n  - predicate: b\n");

        Assert.Equal(new OutputCheck(path, OutputCheckStatus.Differs, 3), result.Value);
    }

    /// <summary>A file with CRLF line endings differs from the LF output, although the text is the same.</summary>
    [Fact]
    public void Check_CrlfFile_Differs_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.rule");
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes("isActive\r\n"));

        ToolResult<OutputCheck> result = OutputWriter.Check(path, "isActive");

        Assert.Equal(OutputCheckStatus.Differs, result.Value?.Status);
        Assert.Equal(1, result.Value?.FirstDifferentLine);
    }

    /// <summary>A file that is a prefix of the output differs on the line where it ends.</summary>
    [Fact]
    public void Check_TruncatedFile_DiffersWhereItEnds_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("rule.rule");
        File.WriteAllText(path, "a\n");

        ToolResult<OutputCheck> result = OutputWriter.Check(path, "a\nb\n");

        Assert.Equal(new OutputCheck(path, OutputCheckStatus.Differs, 2), result.Value);
    }

    /// <summary>A file that does not exist is missing, which is a result and not an error.</summary>
    [Fact]
    public void Check_MissingFile_IsMissing_Test()
    {
        using TemporaryDirectory directory = new();

        ToolResult<OutputCheck> result = OutputWriter.Check(directory.File("absent.json"), "{}");

        Assert.True(result.Succeeded);
        Assert.Equal(OutputCheckStatus.Missing, result.Value.Status);
    }

    /// <summary>Writing to a path that is a directory is an OutputFailed error, not an exception.</summary>
    [Fact]
    public void WriteFile_PathIsDirectory_ReportsOutputFailed_Test()
    {
        using TemporaryDirectory directory = new();

        ToolResult<string> result = OutputWriter.WriteFile(directory.FullPath, "x");

        Assert.Equal(ToolDiagnosticCodes.OutputFailed, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>Writing to a text writer applies the same normalization.</summary>
    [Fact]
    public void Write_TextWriter_WritesNormalizedText_Test()
    {
        using StringWriter writer = new();

        OutputWriter.Write(writer, "a\r\nb");

        Assert.Equal("a\nb\n", writer.ToString());
    }
}
