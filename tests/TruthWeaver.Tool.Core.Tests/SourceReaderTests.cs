namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class SourceReaderTests
{
    /// <summary>A UTF-8 file with a byte order mark reads without the mark, and the reader reports the encoding.</summary>
    [Fact]
    public void ReadFile_Utf8BomFixture_RemovesMarkAndReportsEncoding_Test()
    {
        ToolResult<SourceText> result = SourceReader.ReadFile(Fixtures.Path("access-bom.rule"));

        Assert.True(result.Succeeded);
        Assert.Equal(SourceEncoding.Utf8WithBom, result.Value.Encoding);
        Assert.Equal(Fixtures.Read("access.rule"), result.Value.Text);
    }

    /// <summary>A file with no byte order mark reads as UTF-8.</summary>
    [Fact]
    public void ReadFile_NoMark_ReadsUtf8_Test()
    {
        ToolResult<SourceText> result = SourceReader.ReadFile(Fixtures.Path("access.rule"));

        Assert.True(result.Succeeded);
        Assert.Equal(SourceEncoding.Utf8, result.Value.Encoding);
        Assert.StartsWith("/* Editors", result.Value.Text, StringComparison.Ordinal);
    }

    /// <summary>Each UTF-16 and UTF-32 byte order mark selects its encoding, and the decoded text has no mark.</summary>
    /// <param name="encodingName">The .NET encoding name to write the input with.</param>
    /// <param name="expected">The encoding the reader should report.</param>
    [Theory]
    [InlineData("utf-16", SourceEncoding.Utf16LittleEndian)]
    [InlineData("utf-16BE", SourceEncoding.Utf16BigEndian)]
    [InlineData("utf-32", SourceEncoding.Utf32LittleEndian)]
    [InlineData("utf-32BE", SourceEncoding.Utf32BigEndian)]
    public void Decode_UnicodeMarks_SelectEncodingAndRemoveMark_Test(string encodingName, SourceEncoding expected)
    {
        Encoding encoding = Encoding.GetEncoding(encodingName);
        byte[] bytes = [.. encoding.GetPreamble(), .. encoding.GetBytes("isActive AND hasRole(role: \"é\")")];

        ToolResult<SourceText> result = SourceReader.Decode(bytes, "input");

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.Value.Encoding);
        Assert.Equal("isActive AND hasRole(role: \"é\")", result.Value.Text);
    }

    /// <summary>An invalid UTF-8 byte is an error with its offset, not a replacement character.</summary>
    [Fact]
    public void Decode_InvalidUtf8_ReportsInvalidEncodingWithOffset_Test()
    {
        byte[] bytes = [(byte)'a', (byte)' ', 0xC3, 0x28];

        ToolResult<SourceText> result = SourceReader.Decode(bytes, "input");

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.InvalidEncoding, diagnostic.Code);
        Assert.Contains("offset 2", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A missing file is an InputUnreadable error, not an exception.</summary>
    [Fact]
    public void ReadFile_MissingFile_ReportsInputUnreadable_Test()
    {
        ToolResult<SourceText> result = SourceReader.ReadFile(Fixtures.Path("does-not-exist.rule"));

        Assert.False(result.TryGetValue(out SourceText? text, out EquatableArray<Diagnostic> diagnostics));
        Assert.Null(text);
        Assert.Equal(ToolDiagnosticCodes.InputUnreadable, Assert.Single(diagnostics).Code);
    }

    /// <summary>A stream, such as standard input, reads to its end with the standard-input origin name.</summary>
    [Fact]
    public void ReadStream_Utf8Bytes_ReadsTextWithStandardInputOrigin_Test()
    {
        using MemoryStream stream = new([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("isActive")]);

        ToolResult<SourceText> result = SourceReader.ReadStream(stream);

        Assert.True(result.Succeeded);
        Assert.Equal(new SourceText("isActive", "-", SourceEncoding.Utf8WithBom), result.Value);
    }

    /// <summary>A string that still starts with U+FEFF loses it.</summary>
    [Fact]
    public void ReadString_LeadingMarkCharacter_RemovesIt_Test()
    {
        ToolResult<SourceText> result = SourceReader.ReadString("﻿isActive", "pasted");

        Assert.Equal(new SourceText("isActive", "pasted", SourceEncoding.Utf8WithBom), result.Value);
    }
}
