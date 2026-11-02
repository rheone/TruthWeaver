namespace TruthWeaver.Tool.Core.Tests.Export;

using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>Finding the renderer programs, the names of formats and the failures of a render that the fake-tool tests do not reach.</summary>
public sealed class ImageRendererSearchTests
{
    /// <summary>A diagram language that is not defined has no program name.</summary>
    [Fact]
    public void ProgramName_UndefinedLanguage_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageRenderer.ProgramName((DiagramLanguage)99));
    }

    /// <summary>An image format that is not defined has no extension.</summary>
    [Fact]
    public void Extension_UndefinedFormat_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageRenderer.Extension((ImageFormat)99));
    }

    /// <summary>Each defined format has its extension with the dot.</summary>
    [Fact]
    public void Extension_DefinedFormats_GiveSvgAndPng_Test()
    {
        Assert.Equal(".svg", ImageRenderer.Extension(ImageFormat.Svg));
        Assert.Equal(".png", ImageRenderer.Extension(ImageFormat.Png));
    }

    /// <summary>The search skips directories that do not exist and quoted entries, and finds the program in a later directory.</summary>
    [Fact]
    public void FindProgram_ProgramInALaterDirectory_SkipsMissingAndQuotedEntries_Test()
    {
        using TemporaryDirectory tools = new();
        string file = Touch(tools, "dot");
        string searchPath = string.Join(Path.PathSeparator, Path.Combine(tools.FullPath, "missing"), $"\"{tools.FullPath}\"");

        string? found = ImageRenderer.FindProgram(DiagramLanguage.Dot, searchPath);

        Assert.Equal(Path.GetFullPath(file), found);
    }

    /// <summary>An empty search path finds nothing, and every language is missing.</summary>
    [Fact]
    public void MissingLanguages_EmptySearchPath_ListsBothInOrder_Test()
    {
        Assert.Null(ImageRenderer.FindProgram(DiagramLanguage.Mermaid, string.Empty));
        Assert.Equal([DiagramLanguage.Mermaid, DiagramLanguage.Dot], ImageRenderer.MissingLanguages(string.Empty));
        Assert.Empty(ImageRenderer.AvailableLanguages(string.Empty));
    }

    /// <summary>Only the language whose program is on the search path is available.</summary>
    [Fact]
    public void AvailableLanguages_OnlyDotPresent_ListsDotAndMissesMermaid_Test()
    {
        using TemporaryDirectory tools = new();
        Touch(tools, "dot");

        Assert.Equal([DiagramLanguage.Dot], ImageRenderer.AvailableLanguages(tools.FullPath));
        Assert.Equal([DiagramLanguage.Mermaid], ImageRenderer.MissingLanguages(tools.FullPath));
    }

    /// <summary>An output path that is an existing directory fails with an output error, and not with an exception.</summary>
    [Fact]
    public void Render_OutputPathIsADirectory_ReportsOutputFailed_Test()
    {
        using TemporaryDirectory tools = new();
        Touch(tools, "dot");
        string output = tools.File("taken");
        Directory.CreateDirectory(output);

        ToolResult<string> result = ImageRenderer.Render(
            "digraph { }",
            DiagramLanguage.Dot,
            output,
            new ImageRenderOptions { SearchPath = tools.FullPath }
        );

        Assert.False(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.OutputFailed, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A null diagram or a null output path is an argument error.</summary>
    [Fact]
    public void Render_NullArguments_Throw_Test()
    {
        Assert.Throws<ArgumentNullException>(() => ImageRenderer.Render(null!, DiagramLanguage.Dot, "x.svg"));
        Assert.Throws<ArgumentNullException>(() => ImageRenderer.Render("d", DiagramLanguage.Dot, null!));
    }

    // Makes an empty file with the name of the program, as the search finds it on this platform.
    private static string Touch(TemporaryDirectory directory, string program)
    {
        string path = directory.File(OperatingSystem.IsWindows() ? program + ".exe" : program);
        File.WriteAllText(path, string.Empty);
        return path;
    }
}
