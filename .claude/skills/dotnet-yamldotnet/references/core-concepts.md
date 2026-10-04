# Core Concepts

YamlDotNet's high-level API mirrors the shape of `System.Text.Json`/`Newtonsoft.Json`: a builder
configures options, `.Build()` produces an immutable serializer or deserializer, and you call
`Serialize`/`Deserialize<T>()` against plain C# objects.

## Building a serializer and deserializer

```csharp
using YamlDotNet.Serialization;

var serializer = new SerializerBuilder().Build();
var deserializer = new DeserializerBuilder().Build();
```

Both builders default to PascalCase property matching (i.e., no naming convention transform) and
no special handling for unmatched YAML keys — an unrecognized key during deserialization throws
`YamlException` by default unless you opt out (see below).

## Serializing an object to YAML

```csharp
public sealed class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public List<string> AllowedOrigins { get; set; } = [];
}

var config = new ServerConfig { Host = "localhost", Port = 8080, AllowedOrigins = ["a.com", "b.com"] };

string yaml = serializer.Serialize(config);
```

```yaml
Host: localhost
Port: 8080
AllowedOrigins:
- a.com
- b.com
```

## Deserializing YAML into an object

```csharp
ServerConfig config = deserializer.Deserialize<ServerConfig>(yaml);
```

`Deserialize<T>()` also accepts a `TextReader` directly, which avoids buffering the whole YAML
document into a string first when reading from a file or stream:

```csharp
using var reader = new StreamReader("config.yaml");
ServerConfig config = deserializer.Deserialize<ServerConfig>(reader);
```

## Ignoring unmatched properties

By default, a YAML key with no matching C# property throws. To tolerate extra/unknown keys (common
when deserializing a config file you don't fully control the schema of):

```csharp
var deserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .Build();
```

## Handling required vs. optional fields

YamlDotNet does not enforce "required" fields by default — a missing YAML key simply leaves the
corresponding C# property at its default/initializer value. For validation beyond "did it parse,"
run the deserialized object through a separate validation step (data annotations, FluentValidation,
or manual checks) rather than expecting the deserializer itself to enforce presence.

## Common pitfall

Properties without a public setter (`{ get; }`-only, or `init`-only in older YamlDotNet versions)
are skipped silently during deserialization rather than erroring — a POCO with `init` accessors
deserializes correctly on current YamlDotNet, but a read-only property backed only by a constructor
parameter with no corresponding YAML-visible setter never gets populated. Verify the property has a
writable accessor (`set` or `init`) if a value isn't showing up after deserialization.
