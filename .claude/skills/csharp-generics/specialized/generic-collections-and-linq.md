# Generic Collections and LINQ

From the BCL's ready-made generic collections to implementing your own. Assumes
[references/csharp2-generics-fundamentals.md](../references/csharp2-generics-fundamentals.md).

## Basic: the BCL collections

```csharp
List<int> numbers = [1, 2, 3];
Dictionary<string, int> wordCounts = new() { ["cat"] = 2, ["dog"] = 1 };
HashSet<string> tags = ["urgent", "billing"];
Queue<Job> pending = new();
Stack<UndoAction> undoHistory = new();
```

Every one of these is `IEnumerable<T>` (or `IEnumerable<KeyValuePair<TKey, TValue>>` for
`Dictionary`), which is what makes LINQ apply uniformly across all of them.

## LINQ is generic methods over `IEnumerable<T>`

```csharp
List<Order> pendingHighValue = orders
    .Where(o => o.Status == OrderStatus.Pending)
    .OrderByDescending(o => o.Total)
    .Take(10)
    .ToList();
```

`Where<TSource>`, `OrderByDescending<TSource, TKey>`, `Take<TSource>` — every LINQ operator is a
generic extension method (classic `this`-parameter syntax) with its type parameters inferred from
the lambda's input, chaining without ever naming a type argument explicitly.

## Advanced: implementing `IEnumerable<T>` by hand

```csharp
public class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _items;
    private int _start;
    private int _count;

    public CircularBuffer(int capacity) => _items = new T[capacity];

    public void Add(T item)
    {
        int index = (_start + _count) % _items.Length;
        _items[index] = item;
        if (_count < _items.Length)
        {
            _count++;
        }
        else
        {
            _start = (_start + 1) % _items.Length;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _items[(_start + i) % _items.Length];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

Implementing the non-generic `IEnumerable.GetEnumerator()` explicitly (interface-explicit
implementation) alongside the generic one is the standard shape — every custom generic collection
in the BCL follows it, so `foreach` and every LINQ operator work against `CircularBuffer<T>`
exactly as they do against `List<T>`.

## Advanced: a generic collection with its own constraint

```csharp
public class SortedBag<T> : IEnumerable<T> where T : IComparable<T>
{
    private readonly List<T> _items = new();

    public void Add(T item)
    {
        int index = _items.BinarySearch(item);
        _items.Insert(index < 0 ? ~index : index, item);
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

The constraint (`where T : IComparable<T>`) belongs on the class, not just the `Add` method,
because every member — including a future `Contains`/`Remove` using the same ordering — relies on
comparability; constraining the class once avoids repeating it per method.

## Fallback

Everything here is plain C# 2.0+ generics; `List<T>`/`Dictionary<TKey,TValue>` and hand-rolled
`IEnumerable<T>` implementations have worked unchanged since .NET Framework 2.0. LINQ specifically
needs .NET Framework 3.5 / C# 3.0 (`System.Linq`) — before that, iterate and filter with `foreach`
and manual conditionals over the same generic collections.
