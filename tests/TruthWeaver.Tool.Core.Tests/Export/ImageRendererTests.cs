namespace TruthWeaver.Tool.Core.Tests.Export;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>
/// Images come from the external renderers on the search path. The tests put a fake <c>dot</c> or <c>mmdc</c> in a
/// temporary directory and use that directory as the search path; the fake writes its arguments to the output file.
/// </summary>
public sealed class ImageRendererTests
{
    /// <summary>With no Graphviz on the search path, the render fails with a <c>TWT0016</c> error that names <c>dot</c> and where to get it.</summary>
    [Fact]
    public void Render_DotMissing_ReportsTheMissingProgram_Test()
    {
        using TemporaryDirectory empty = new();

        ToolResult<string> result = ImageRenderer.Render(
            "digraph { }",
            DiagramLanguage.Dot,
            empty.File("rule.svg"),
            new ImageRenderOptions { SearchPath = empty.FullPath }
        );

        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.RendererNotFound, error.Code);
        Assert.Equal(
            "Graphviz 'dot' is not on the PATH, so the DOT diagram cannot be rendered as an image. Install Graphviz "
                + "(https://graphviz.org/download/) and put 'dot' on the PATH. The tools have no renderer of their own.",
            error.Message
        );
        Assert.False(File.Exists(empty.File("rule.svg")));
    }

    /// <summary>With no Mermaid CLI on the search path, the render fails with a <c>TWT0016</c> error that names <c>mmdc</c> and how to install it.</summary>
    [Fact]
    public void Render_MermaidCliMissing_ReportsTheMissingProgram_Test()
    {
        using TemporaryDirectory empty = new();

        ToolResult<string> result = ImageRenderer.Render(
            "flowchart TD",
            DiagramLanguage.Mermaid,
            empty.File("rule.svg"),
            new ImageRenderOptions { SearchPath = empty.FullPath }
        );

        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.RendererNotFound, error.Code);
        Assert.Equal(
            "The Mermaid CLI 'mmdc' is not on the PATH, so the Mermaid diagram cannot be rendered as an image. Install it "
                + "with 'npm install -g @mermaid-js/mermaid-cli' and put 'mmdc' on the PATH. The tools have no renderer of their own.",
            error.Message
        );
        Assert.Equal(ToolExitCode.Unusable, SeverityPolicy.ExitCodeFor(result.Diagnostics, strict: false));
    }

    /// <summary>A fake <c>dot</c> on the search path is called with the format, the output and the diagram file, and the image is its output.</summary>
    [Fact]
    public void Render_FakeDotOnTheSearchPath_RunsItWithTheFormatAndFiles_Test()
    {
        using TemporaryDirectory tools = new();
        FakeTool.Write(tools, "dot", outputArgument: 3);
        string output = tools.File("images/rule.png");

        ToolResult<string> result = ImageRenderer.Render(
            "digraph { a -> b }",
            DiagramLanguage.Dot,
            output,
            new ImageRenderOptions { SearchPath = tools.FullPath, Format = ImageFormat.Png }
        );

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(Path.GetFullPath(output), result.Value);
        string written = File.ReadAllText(output);
        Assert.StartsWith("fake dot -Tpng -o ", written, StringComparison.Ordinal);
        Assert.Contains(".dot", written, StringComparison.Ordinal);
    }

    /// <summary>A fake <c>mmdc</c> on the search path is called with <c>-e</c>, <c>-o</c> and <c>-i</c>.</summary>
    [Fact]
    public void Render_FakeMermaidCliOnTheSearchPath_RunsItWithTheFormatAndFiles_Test()
    {
        using TemporaryDirectory tools = new();
        FakeTool.Write(tools, "mmdc", outputArgument: 4);
        string output = tools.File("rule.svg");

        ToolResult<string> result = ImageRenderer.Render(
            "flowchart TD",
            DiagramLanguage.Mermaid,
            output,
            new ImageRenderOptions { SearchPath = tools.FullPath }
        );

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        string written = File.ReadAllText(output);
        Assert.StartsWith("fake mmdc -e svg -o ", written, StringComparison.Ordinal);
        Assert.Contains(" -i ", written, StringComparison.Ordinal);
    }

    /// <summary>A renderer that stops with a non-zero exit code fails the render with <c>TWT0017</c> and its error output.</summary>
    [Fact]
    public void Render_RendererFails_ReportsTheExitCodeAndErrorOutput_Test()
    {
        using TemporaryDirectory tools = new();
        FakeTool.WriteFailing(tools, "dot");

        ToolResult<string> result = ImageRenderer.Render(
            "digraph { }",
            DiagramLanguage.Dot,
            tools.File("rule.svg"),
            new ImageRenderOptions { SearchPath = tools.FullPath }
        );

        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.RenderFailed, error.Code);
        Assert.Contains("exit code 3", error.Message, StringComparison.Ordinal);
        Assert.Contains("syntax error in line 1", error.Message, StringComparison.Ordinal);
    }

    /// <summary>An export of every artifact with images renders the Mermaid and DOT diagrams and lists the images in the manifest.</summary>
    [Fact]
    public void ExportAll_WithImagesAndFakeRenderers_WritesAndListsTheImages_Test()
    {
        using TemporaryDirectory tools = new();
        using TemporaryDirectory folder = new();
        FakeTool.Write(tools, "dot", outputArgument: 3);
        FakeTool.Write(tools, "mmdc", outputArgument: 4);

        ToolResult<ExportManifest> result = ArtifactExporter.ExportAll(
            RuleLoader.LoadFile(Fixtures.Path("export.rule")).Value!,
            folder.FullPath,
            new ExportOptions { Images = new ImageRenderOptions { SearchPath = tools.FullPath } }
        );

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["mermaid-svg", "dot-svg"], result.Value.Artifacts.TakeLast(2).Select(a => a.Id));
        Assert.True(File.Exists(folder.File("mermaid.svg")));
        Assert.True(File.Exists(folder.File("dot.svg")));
        Assert.Contains(
            "\"source\": \"mermaid.mmd\"",
            File.ReadAllText(folder.File(ExportManifest.FileName)),
            StringComparison.Ordinal
        );
    }

    /// <summary>An export with images and no renderers still writes the text artifacts, reports each missing program, and lists no image.</summary>
    [Fact]
    public void ExportAll_WithImagesAndNoRenderers_WritesTheTextAndReportsTheMissingPrograms_Test()
    {
        using TemporaryDirectory empty = new();
        using TemporaryDirectory folder = new();

        ToolResult<ExportManifest> result = ArtifactExporter.ExportAll(
            RuleLoader.LoadFile(Fixtures.Path("export.rule")).Value!,
            folder.FullPath,
            new ExportOptions { Images = new ImageRenderOptions { SearchPath = empty.FullPath } }
        );

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == ToolDiagnosticCodes.RendererNotFound));
        Assert.True(File.Exists(folder.File("mermaid.mmd")));
        Assert.DoesNotContain("svg", File.ReadAllText(folder.File(ExportManifest.FileName)), StringComparison.Ordinal);
    }

    /// <summary>Writes fake renderers: a Windows batch file or a POSIX shell script.</summary>
    private static class FakeTool
    {
        // Writes a renderer that writes "fake <name> <arguments>" to its output argument (1-based) and succeeds.
        public static void Write(TemporaryDirectory directory, string name, int outputArgument)
        {
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(
                    directory.File(name + ".cmd"),
                    $"@echo off\r\necho fake {name} %*> \"%~{outputArgument}\"\r\n"
                );
            }
            else
            {
                WriteScript(directory.File(name), $"#!/bin/sh\necho \"fake {name} $*\" > \"${outputArgument}\"\n");
            }
        }

        // Writes a renderer that prints an error and stops with exit code 3.
        public static void WriteFailing(TemporaryDirectory directory, string name)
        {
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(
                    directory.File(name + ".cmd"),
                    "@echo off\r\necho syntax error in line 1 1>&2\r\nexit /b 3\r\n"
                );
            }
            else
            {
                WriteScript(directory.File(name), "#!/bin/sh\necho 'syntax error in line 1' >&2\nexit 3\n");
            }
        }

        [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
        private static void WriteScript(string path, string text)
        {
            File.WriteAllText(path, text);
            const UnixFileMode Executable =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead;
            File.SetUnixFileMode(path, Executable);
        }
    }
}
