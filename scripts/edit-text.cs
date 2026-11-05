#!/usr/bin/env dotnet

// Edits a text file in two ways that the Edit tool cannot do in one call.
//
//   insert-before <file> <anchor> <snippet-file>
//     Puts the text of <snippet-file> on the line before the first line that contains <anchor>. Use it to add a test before
//     the private helpers of a test class (SA1202 wants public members first). The snippet gets one blank line after it.
//   move-to-end <file> <signature>
//     Moves the member whose signature is on the first line that contains <signature>, with its attribute and XML comment
//     lines above and through its closing "    }" line, to the end of the class. Use it after you append a new test below a
//     private helper.
//
// Both fail with exit code 1 and change nothing when the anchor or signature is missing or not unique.
//
// Usage: dotnet run scripts/edit-text.cs -- <insert-before|move-to-end> <file> <anchor-or-signature> [<snippet-file>]
if (args.Length < 3)
{
    await Console.Error.WriteLineAsync(
        "Usage: dotnet run scripts/edit-text.cs -- <insert-before|move-to-end> <file> <anchor-or-signature> [<snippet-file>]"
    );
    return 2;
}

string command = args[0];
string path = args[1];
string anchor = args[2];
string text = (await File.ReadAllTextAsync(path)).ReplaceLineEndings("\n");

int index = text.IndexOf(anchor, StringComparison.Ordinal);
if (index < 0 || text.IndexOf(anchor, index + 1, StringComparison.Ordinal) >= 0)
{
    await Console.Error.WriteLineAsync($"'{anchor}' must occur exactly once in {path}.");
    return 1;
}

int lineStart = text.LastIndexOf('\n', index) + 1;
switch (command)
{
    case "insert-before" when args.Length == 4:
        string snippet = (await File.ReadAllTextAsync(args[3])).ReplaceLineEndings("\n").Trim('\n');
        text = text[..lineStart] + snippet + "\n\n" + text[lineStart..];
        break;
    case "move-to-end":
        // Take the attribute and XML comment lines directly above the signature along with it.
        while (lineStart > 0)
        {
            int previousStart = text.LastIndexOf('\n', lineStart - 2) + 1;
            string previous = text[previousStart..(lineStart - 1)].TrimStart();
            if (!previous.StartsWith('[') && !previous.StartsWith("///", StringComparison.Ordinal))
            {
                break;
            }

            lineStart = previousStart;
        }

        const string Close = "\n    }\n";
        int close = text.IndexOf(Close, lineStart, StringComparison.Ordinal);
        if (close < 0)
        {
            await Console.Error.WriteLineAsync($"No closing '    }}' line after '{anchor}'.");
            return 1;
        }

        int end = close + Close.Length;
        string member = text[lineStart..end].TrimEnd('\n');
        text = text.Remove(lineStart, end - lineStart);

        // The blank line that ran after the member now follows a blank line or an opening brace: drop it.
        if (
            lineStart >= 2
            && lineStart < text.Length
            && text[lineStart] == '\n'
            && text[lineStart - 1] == '\n'
            && text[lineStart - 2] is '\n' or '{'
        )
        {
            text = text.Remove(lineStart, 1);
        }

        int classEnd = text.LastIndexOf("\n}", StringComparison.Ordinal);
        text = text[..classEnd].TrimEnd('\n') + "\n\n" + member + text[classEnd..];
        break;
    default:
        await Console.Error.WriteLineAsync("Unknown command or missing snippet file.");
        return 2;
}

await File.WriteAllTextAsync(path, text);
return 0;
