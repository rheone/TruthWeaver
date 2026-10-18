namespace TruthWeaver.Tool.Tui;

using System.Diagnostics;
using System.Globalization;
using System.Text;

/// <summary>
/// Finds out whether the terminal background is light or dark. It sends the xterm query OSC 11 and reads the reply, which
/// has the form <c>ESC ] 11 ; rgb:RRRR/GGGG/BBBB</c> and ends with BEL or <c>ESC \</c>. A terminal that does not know the
/// query sends nothing, so the read stops at a time limit.
/// </summary>
internal static class TerminalBackground
{
    private const string Query = "\u001b]11;?\u001b\\";

    /// <summary>Reads the theme out of a reply to the background query.</summary>
    /// <param name="reply">The reply, with or without the escape characters around it.</param>
    /// <returns>Light when the background is brighter than the middle, otherwise dark; <see langword="null"/> when the reply has no color.</returns>
    public static ThemeVariant? Parse(string reply)
    {
        ArgumentNullException.ThrowIfNull(reply);

        int start = reply.IndexOf("rgb:", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        // Each channel has one to four hex digits; the largest value of its width is full intensity.
        string[] parts = reply[(start + 4)..].Split('/');
        if (parts.Length < 3)
        {
            return null;
        }

        double[] channels = new double[3];
        for (int i = 0; i < 3; i++)
        {
            string digits = new([.. parts[i].TakeWhile(Uri.IsHexDigit).Take(4)]);
            if (
                digits.Length == 0
                || !int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value)
            )
            {
                return null;
            }

            channels[i] = value / (Math.Pow(16, digits.Length) - 1);
        }

        double lightness = (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
        return lightness > 0.5 ? ThemeVariant.Light : ThemeVariant.Dark;
    }

    /// <summary>
    /// Asks the terminal for its background. It does nothing when input or output is redirected, and it gives up after
    /// <paramref name="timeout"/>. Call it before the terminal library starts, because both read the same input.
    /// </summary>
    /// <param name="timeout">How long to wait for the reply.</param>
    /// <returns>The theme, or <see langword="null"/> when the terminal did not answer.</returns>
    public static ThemeVariant? Detect(TimeSpan timeout)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return null;
        }

        try
        {
            Console.Out.Write(Query);
            Console.Out.Flush();
            StringBuilder reply = new();
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.Elapsed < timeout)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(5);
                    continue;
                }

                char c = Console.ReadKey(intercept: true).KeyChar;
                reply.Append(c);

                // BEL ends the reply; so does the ESC \ pair.
                if (c == '\a' || (c == '\\' && reply.Length > 1 && reply[^2] == '\u001b'))
                {
                    break;
                }
            }

            return Parse(reply.ToString());
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or PlatformNotSupportedException)
        {
            // A console that cannot be queried is the same as a terminal that does not answer.
            return null;
        }
    }
}
