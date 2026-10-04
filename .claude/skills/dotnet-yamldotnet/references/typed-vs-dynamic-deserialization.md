# Typed vs. Dynamic Deserialization

YamlDotNet offers three shapes for turning YAML into something a C# program can work with, each
suited to a different situation: you know the schema, you don't know the schema but need typed
navigation, or you want minimal ceremony over an ad hoc structure.

## Strongly-typed POCO deserialization

Reach for this whenever the YAML's shape is known and stable (a config file your own project owns,
a documented external schema):

```csharp
public sealed class AppConfig
{
    public string Environment { get; set; } = "";
    public DatabaseSettings Database { get; set; } = new();
}

public sealed class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
    public int TimeoutSeconds { get; set; }
}

AppConfig config = deserializer.Deserialize<AppConfig>(yaml);
```

Gives compile-time checking, IntelliSense, and refactoring support — the default choice unless a
specific reason below applies.

## `YamlNode` tree (`YamlStream`, `YamlDocument`, `YamlMappingNode`)

Reach for this when the YAML's shape isn't known ahead of time, varies per document, or the code
needs to inspect structure generically (a YAML linter, a generic config-merging tool, a
schema-agnostic diff):

```csharp
using YamlDotNet.RepresentationModel;

var yamlStream = new YamlStream();
yamlStream.Load(new StringReader(yamlText));

var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;

foreach (var entry in root.Children)
{
    string key = ((YamlScalarNode)entry.Key).Value!;
    Console.WriteLine(key);
}
```

- `YamlStream.Load` parses every document in the stream into `Documents`, unlike
  `Deserializer.Deserialize<T>()`'s single-document default — see
  [anchors-aliases-and-multi-document.md](anchors-aliases-and-multi-document.md).
- Every node is one of `YamlScalarNode`, `YamlMappingNode`, or `YamlSequenceNode` — casting to the
  wrong one throws `InvalidCastException`, so code walking an unknown structure typically pattern-
  matches on node type rather than assuming a shape.
- This is the right tool for surgical edits to part of a YAML document while leaving the rest
  byte-for-byte (or at least structurally) untouched — round-tripping through a POCO model loses
  any YAML content not mapped to a property, while a `YamlNode` tree preserves everything.

## `dynamic` deserialization

```csharp
var deserializer = new DeserializerBuilder().Build();
dynamic config = deserializer.Deserialize<dynamic>(yaml);

string host = config["database"]["host"];
```

- Deserializes into nested `Dictionary<object, object>`/`List<object>` structures accessed via
  `dynamic`, trading compile-time safety for the least ceremony when a script or one-off tool just
  needs to poke at a value or two.
- Least recommended of the three for anything beyond a quick script — no compile-time checking, no
  IntelliSense, and runtime `RuntimeBinderException` on a typo instead of a compile error. Prefer
  the `YamlNode` tree for any code that will be maintained or tested, since it at least fails with a
  clear cast exception rather than a dynamic dispatch failure.

## Choosing between the three

| Situation | Choice |
| --- | --- |
| YAML shape is known, owned, and stable | Strongly-typed POCO |
| YAML shape varies or is unknown ahead of time, and the code needs to preserve/inspect structure generically | `YamlNode` tree (`YamlStream`) |
| A quick script needing one or two values from an ad hoc document | `dynamic` |
