namespace TruthWeaver.Tool.Core;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// Renders a Mermaid or DOT diagram as an SVG or PNG image with an external program: Graphviz <c>dot</c> for DOT, and the
/// Mermaid CLI <c>mmdc</c> for Mermaid. The tools have no renderer of their own. When the program is not on the search
/// path, the render fails with a <see cref="ToolDiagnosticCodes.RendererNotFound"/> error that names the program and where
/// to get it.
/// </summary>
/// <remarks>
/// The renderer runs as <c>dot -T&lt;format&gt; -o &lt;output&gt; &lt;input&gt;</c> or
/// <c>mmdc -e &lt;format&gt; -o &lt;output&gt; -i &lt;input&gt;</c>, with the diagram in a temporary input file. The image
/// bytes come from the external program, so they are only as deterministic as that program and its version.
/// </remarks>
public static class ImageRenderer
{
    // On Windows a program on the PATH is an .exe, or a .cmd or .bat script (npm installs mmdc as mmdc.cmd).
    private static readonly string[] WindowsExtensions = [".exe", ".cmd", ".bat"];

    /// <summary>Gets the name of the program that renders a diagram language, as it is looked for on the search path.</summary>
    /// <param name="language">The diagram language.</param>
    /// <returns><c>dot</c> or <c>mmdc</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="language"/> is not a defined value.</exception>
    public static string ProgramName(DiagramLanguage language)
    {
        return language switch
        {
            DiagramLanguage.Dot => "dot",
            DiagramLanguage.Mermaid => "mmdc",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "The value is not a diagram language."),
        };
    }

    /// <summary>Finds the program that renders a diagram language on the search path.</summary>
    /// <param name="language">The diagram language.</param>
    /// <param name="searchPath">The directories to look in, in the form of the <c>PATH</c> environment variable, or <see langword="null"/> for <c>PATH</c>.</param>
    /// <returns>The full path of the program, or <see langword="null"/> when no directory has it.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="language"/> is not a defined value.</exception>
    public static string? FindProgram(DiagramLanguage language, string? searchPath = null)
    {
        string name = ProgramName(language);
        string[] fileNames = OperatingSystem.IsWindows()
            ? [.. WindowsExtensions.Select(extension => name + extension)]
            : [name];
        string path = searchPath ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (string fileName in fileNames)
            {
                string candidate = Path.Combine(directory.Trim('"'), fileName);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        return null;
    }

    /// <summary>Gets the file name extension of an image format, with the dot.</summary>
    /// <param name="format">The image format.</param>
    /// <returns><c>.svg</c> or <c>.png</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined value.</exception>
    public static string Extension(ImageFormat format)
    {
        return "." + FormatName(format);
    }

    /// <summary>Renders a diagram as an image file.</summary>
    /// <param name="diagram">The diagram text, as <c>PrintDot</c> or <c>PrintMermaid</c> writes it.</param>
    /// <param name="language">The language of the diagram.</param>
    /// <param name="outputPath">The image file to write. The renderer creates its directory, and replaces the file when it exists.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ImageRenderOptions.Default"/>.</param>
    /// <returns>
    /// The full path of the image, or a <see cref="ToolDiagnosticCodes.RendererNotFound"/> error when the program is not on
    /// the search path, a <see cref="ToolDiagnosticCodes.RenderFailed"/> error when it fails, or a
    /// <see cref="ToolDiagnosticCodes.OutputFailed"/> error when a file cannot be written.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagram"/> or <paramref name="outputPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="language"/> or the image format is not a defined value.</exception>
    public static ToolResult<string> Render(
        string diagram,
        DiagramLanguage language,
        string outputPath,
        ImageRenderOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(diagram);
        ArgumentNullException.ThrowIfNull(outputPath);
        options ??= ImageRenderOptions.Default;
        string format = FormatName(options.Format);

        string? program = FindProgram(language, options.SearchPath);
        if (program is null)
        {
            return ToolResults.Failure<string>(ToolDiagnosticCodes.RendererNotFound, MissingProgramMessage(language));
        }

        string input = Path.Combine(
            Path.GetTempPath(),
            "truthweaver-" + Guid.NewGuid().ToString("N") + (language == DiagramLanguage.Dot ? ".dot" : ".mmd")
        );
        string output;
        try
        {
            output = Path.GetFullPath(outputPath);
            if (Path.GetDirectoryName(output) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            // A stale image would hide a renderer that writes nothing, so the old file goes first.
            File.Delete(output);
            File.WriteAllBytes(input, OutputWriter.Encode(diagram));
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return ToolResults.Failure<string>(
                ToolDiagnosticCodes.OutputFailed,
                $"The image '{outputPath}' cannot be written: {ex.Message}"
            );
        }

        try
        {
            string[] arguments =
                language == DiagramLanguage.Dot
                    ? ["-T" + format, "-o", output, input]
                    : ["-e", format, "-o", output, "-i", input];
            string? failure = Run(program, arguments, options.Timeout);
            if (failure is null && !File.Exists(output))
            {
                failure = "it wrote no image file";
            }

            return failure is null
                ? ToolResults.Success(output)
                : ToolResults.Failure<string>(
                    ToolDiagnosticCodes.RenderFailed,
                    $"'{ProgramName(language)}' ({program}) did not render '{outputPath}': {failure}"
                );
        }
        finally
        {
            File.Delete(input);
        }
    }

    // Runs the program and gives null on success, or the reason it failed.
    private static string? Run(string program, IEnumerable<string> arguments, TimeSpan timeout)
    {
        ProcessStartInfo start = new(program)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // Both streams are read while the program runs, so a program that writes a lot cannot block on a full pipe.
        StringBuilder error = new();
        using Process process = new() { StartInfo = start };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (error)
                {
                    error.AppendLine(e.Data);
                }
            }
        };
        process.OutputDataReceived += (_, _) => { };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            return "it cannot be started: " + ex.Message;
        }

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return $"it did not finish within {timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} seconds";
        }

        // The overload without a time limit also waits for the end of the redirected streams.
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            return null;
        }

        string message;
        lock (error)
        {
            message = error.ToString().Trim();
        }

        return $"it stopped with exit code {process.ExitCode.ToString(CultureInfo.InvariantCulture)}"
            + (message.Length == 0 ? string.Empty : ": " + message);
    }

    private static string FormatName(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Svg => "svg",
            ImageFormat.Png => "png",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "The value is not an image format."),
        };
    }

    private static string MissingProgramMessage(DiagramLanguage language)
    {
        return language == DiagramLanguage.Dot
            ? "Graphviz 'dot' is not on the PATH, so the DOT diagram cannot be rendered as an image. Install Graphviz "
                + "(https://graphviz.org/download/) and put 'dot' on the PATH. The tools have no renderer of their own."
            : "The Mermaid CLI 'mmdc' is not on the PATH, so the Mermaid diagram cannot be rendered as an image. Install it "
                + "with 'npm install -g @mermaid-js/mermaid-cli' and put 'mmdc' on the PATH. The tools have no renderer of their own.";
    }
}
