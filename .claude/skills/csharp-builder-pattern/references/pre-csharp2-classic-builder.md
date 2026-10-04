# Classic (Non-Generic) Builder (C# 1.0, .NET Framework 1.0/1.1)

C# 1.0 (2002) has none of the language features later tiers lean on: no generics (C# 2.0), no
object initializers (C# 3.0), no init-only setters (C# 9.0). What it has is enough to write the
GoF Builder pattern exactly as the *Design Patterns* book describes it: a mutable product built up
through a series of setter-style calls, with a final step that hands back the finished object. This
tier is the baseline every later tier either simplifies or partially replaces.

The motivating problem is the **telescoping constructor**: a type with many optional parameters
either grows one constructor overload per combination, or forces every caller to pass every
parameter positionally, including ones they don't care about. The builder pattern's answer is to
move construction out of the constructor and into a sequence of named, single-purpose calls.

## Syntax

```csharp
public class Report
{
    private string _title;
    private string _author;
    private bool _includeSummary;
    private int _columnCount;

    // Constructor kept internal/private — callers go through the builder instead.
    internal Report(string title, string author, bool includeSummary, int columnCount)
    {
        _title = title;
        _author = author;
        _includeSummary = includeSummary;
        _columnCount = columnCount;
    }

    public string Title { get { return _title; } }
    public string Author { get { return _author; } }
}

public class ReportBuilder
{
    private string _title = "Untitled";
    private string _author = "Unknown";
    private bool _includeSummary;
    private int _columnCount = 1;

    public void SetTitle(string title) { _title = title; }
    public void SetAuthor(string author) { _author = author; }
    public void SetIncludeSummary(bool includeSummary) { _includeSummary = includeSummary; }
    public void SetColumnCount(int columnCount) { _columnCount = columnCount; }

    public Report Build()
    {
        return new Report(_title, _author, _includeSummary, _columnCount);
    }
}
```

## Basic use case

```csharp
ReportBuilder builder = new ReportBuilder();
builder.SetTitle("Q3 Results");
builder.SetAuthor("Finance");
builder.SetColumnCount(4);
Report report = builder.Build();
```

Every call is a statement of its own — there is no method chaining here, because none of the
`Set*` methods return anything. Chaining (`builder.SetTitle(...).SetAuthor(...)`) is not a C#
version feature at all — it needs nothing beyond a method returning a reference to something the
next call can be invoked on, which C# 1.0 already supports. This tier's examples don't chain only
because chaining wasn't yet idiomatic C# style in 2002; the *capability* was always there. See
[csharp2-generic-builders.md](csharp2-generic-builders.md) for the first tier that actually
exploits it, once generics make a reusable chaining base worth writing.

## Advanced use case: a director separating construction steps from assembly order

```csharp
public class ReportDirector
{
    public Report BuildStandardReport(ReportBuilder builder)
    {
        builder.SetTitle("Standard Report");
        builder.SetIncludeSummary(true);
        builder.SetColumnCount(2);
        return builder.Build();
    }

    public Report BuildExecutiveSummary(ReportBuilder builder)
    {
        builder.SetTitle("Executive Summary");
        builder.SetIncludeSummary(true);
        builder.SetColumnCount(1);
        return builder.Build();
    }
}
```

The **director** is the other half of the classic GoF pattern: it owns a fixed *sequence* of
builder calls representing a named recipe, while the builder owns *how* each individual step
stores its state. Later tiers mostly collapse the director into the builder itself (a fluent
builder's chained calls read as their own recipe at the call site), but the separation is still
worth knowing when the same construction sequence needs to run against interchangeable builder
implementations — the classic use case is one director producing both an object graph and, from a
different builder implementation, a string/XML representation of the same steps.

## Requirements and restrictions

- No compile-time enforcement of call order or of which setters are mandatory — a caller can call
  `Build()` having set nothing, or call `SetTitle` twice. Validation, if any, has to happen inside
  `Build()` itself, and only fails at run time.
- No generics means every builder is written by hand for its one product type; there is no shared,
  reusable `Builder<T>` base to factor the "accumulate state, then construct" shape out of.

## Fallback

This is the first tier — there is no earlier fallback. It is also the tier every later reference
file's Fallback section points back to as the base pattern to hand-write on a target with no newer
language feature available.
