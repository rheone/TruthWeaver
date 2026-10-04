# AST Manipulation

Parsing Markdown with `Markdown.Parse` produces a `MarkdownDocument` — a tree of `Block` nodes
(each of which may contain `Inline` nodes) that you can inspect or mutate before rendering, instead
of treating Markdig as a pure text-to-text converter.

## Parsing without rendering

```csharp
using Markdig;
using Markdig.Syntax;

MarkdownDocument document = Markdown.Parse(markdownText, pipeline);
```

`MarkdownDocument` derives from `ContainingBlock`, so it's a `List<Block>`-like container you can
enumerate directly for top-level blocks (headings, paragraphs, lists, tables) in document order.

## Walking the tree

```csharp
foreach (var heading in document.Descendants<HeadingBlock>())
{
    string text = heading.Inline is not null
        ? string.Concat(heading.Inline.Select(i => i.ToString()))
        : string.Empty;

    Console.WriteLine($"H{heading.Level}: {text}");
}
```

- `Descendants<T>()` recursively enumerates every node of type `T` anywhere in the tree, regardless
  of nesting depth (a heading inside a block quote, a link inside a table cell) — reach for it over
  hand-rolling recursive tree traversal.
- Extension-specific node types (`YamlFrontMatterBlock`, `Table`, `TaskList`) are ordinary `Block`
  subtypes once their extension is enabled on the pipeline used to parse — `Descendants<T>()` finds
  them the same way it finds built-in CommonMark node types.

## Extracting YAML frontmatter

```csharp
using Markdig.Extensions.Yaml;

var frontMatterBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
if (frontMatterBlock is not null)
{
    string rawYaml = frontMatterBlock.Lines.ToString();
    // Deserialize rawYaml with a YAML library into a typed model or dictionary.
}
```

`YamlFrontMatterBlock.Lines` holds the raw text between the `---` delimiters (Markdig does not
parse YAML itself — enabling `UseYamlFrontMatter()` only carves the block out of the Markdown
stream so it doesn't render as document content).

## Mutating the tree before rendering

`Block` and `Inline` collections support ordinary list mutation (`Add`, `Remove`, `Insert`) because
`ContainerBlock`/`ContainerInline` implement `IList<T>`:

```csharp
// Strip every image from the document before rendering (e.g. for a plain-text digest).
foreach (var paragraph in document.Descendants<ParagraphBlock>().ToList())
{
    var images = paragraph.Inline?.Descendants<LinkInline>()
        .Where(link => link.IsImage)
        .ToList();

    if (images is not null)
    {
        foreach (var image in images)
        {
            paragraph.Inline!.Remove(image);
        }
    }
}
```

Materialize the query with `.ToList()` before mutating — removing nodes while `Descendants<T>()`'s
lazy enumeration is still walking the same collection throws or skips nodes, the same hazard as
mutating any collection while foreach-ing over it directly.

## Rendering the mutated tree

Once mutated, render the same `document` instance through the document overload rather than
re-parsing the original text:

```csharp
string html = Markdown.ToHtml(document, pipeline);
```

## Common pitfall

`Inline` can be `null` on a block that has no inline content (an empty paragraph, a thematic
break/horizontal rule, a fenced code block whose content lives in `.Lines` rather than `.Inline`).
Null-check `.Inline` before calling `Descendants<T>()` or LINQ methods on it, or use the
null-conditional operator (`heading.Inline?.Descendants<T>()`) throughout.
