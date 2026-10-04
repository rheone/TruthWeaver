# Custom Type Converters

`IYamlTypeConverter` lets you take full control of how a specific type serializes and deserializes
— reach for it when a type's YAML representation isn't a straightforward property-by-property
mapping: a value object that should collapse to a single scalar, a type requiring custom parsing
logic, or a third-party type you can't add YamlDotNet attributes to.

## The interface

```csharp
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class IpAddressConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(IPAddress);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return IPAddress.Parse(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(((IPAddress)value!).ToString()));
    }
}
```

- `Accepts(Type)` is checked against every type the serializer/deserializer encounters — return
  `true` only for the exact type(s) this converter owns; an overly broad `Accepts` (e.g. checking
  an interface many unrelated types implement) hijacks serialization for types the converter wasn't
  designed for.
- `ReadYaml`/`WriteYaml` work against the low-level `IParser`/`IEmitter` event stream, not against
  a pre-built object graph — `parser.Consume<Scalar>()` reads and advances past exactly one scalar
  event, mirroring how you'd hand-write a JSON converter against `Utf8JsonReader`/`Utf8JsonWriter`.

## Registering the converter

```csharp
var serializer = new SerializerBuilder()
    .WithTypeConverter(new IpAddressConverter())
    .Build();

var deserializer = new DeserializerBuilder()
    .WithTypeConverter(new IpAddressConverter())
    .Build();
```

Register the same converter instance (or an equivalent one) on both builders — a converter that
only exists on the serializer produces YAML the deserializer doesn't know how to read back with the
custom logic, silently falling back to default object-graph deserialization instead.

## Converting a complex object to a single scalar

The most common reason to write a custom converter is collapsing a multi-field type to one line of
YAML instead of a nested mapping — e.g. representing a `Duration` value object as `"5m"` rather
than `{ Value: 5, Unit: Minutes }`:

```csharp
public sealed class DurationConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Duration);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return Duration.Parse(scalar.Value); // e.g. "5m" -> Duration
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(((Duration)value!).ToString())); // Duration -> "5m"
    }
}
```

## Common pitfall

Forgetting to call `parser.Consume<T>()` (or otherwise advance the parser) inside `ReadYaml` leaves
the parser positioned on the same event, which causes the next read to fail or infinite-loop rather
than failing fast with a clear error — always consume exactly the events the converter's YAML
representation actually occupies (one scalar for a single-value type, a full mapping/sequence
start-to-end pair for a structured custom representation).
