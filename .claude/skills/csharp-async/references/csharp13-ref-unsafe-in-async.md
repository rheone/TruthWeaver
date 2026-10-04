# `ref` locals and `unsafe` contexts in async methods (C# 13 / .NET 9)

C# 13 shipped November 2024 alongside .NET 9. Before this release, an `async` method (and,
identically, an iterator method using `yield return`) could not declare `ref` local variables, a
local of a `ref struct` type, or contain an `unsafe` block at all — the compiler-generated state
machine had no way to represent those on its heap-allocated state. C# 13 relaxes both restrictions,
with a boundary that keeps the state machine sound.

## Syntax

```csharp
public async Task<int> SumAsync(int[] values)
{
    ref int first = ref values[0]; // now legal inside an async method
    int sum = first;
    for (int i = 1; i < values.Length; i++)
    {
        sum += values[i];
    }
    await Task.Yield();
    return sum;
}
```

## Basic use case: a `ref` local confined to synchronous spans of an async method

```csharp
public async Task<long> ChecksumAsync(byte[] buffer)
{
    long checksum;
    ref byte start = ref buffer[0]; // ref local — fine, not read across an await
    checksum = ComputeChecksum(ref start, buffer.Length);
    await _sink.WriteAsync(checksum);
    return checksum;
}
```

## Advanced use case: `unsafe` code inside an async method

```csharp
public async Task<int> HashAsync(byte[] data)
{
    int hash;
    unsafe
    {
        fixed (byte* p = data)
        {
            hash = ComputeHashUnmanaged(p, data.Length); // synchronous unsafe block — no await inside
        }
    }
    await _auditLog.RecordAsync(hash);
    return hash;
}
```

The `unsafe` block itself still can't contain `await` or `yield return` — it's confined to a
synchronous span within the async method, same restriction as `ref` locals below.

## Requirements and restrictions

- A `ref` local (or a local of a `ref struct` type) declared inside an `async` method **cannot be
  live across an `await` expression** — the compiler still can't put a `ref` into the heap-allocated
  state machine state. It must be read and finished with entirely between `await`s (or never cross
  one at all, as in both examples above).
- The same "cannot cross an `await`/`yield return`" rule applies to `unsafe` blocks: the block itself
  is fine, but it can't contain an `await` or `yield return`, and no `ref`/pointer produced inside
  it may be held across one.
- Requires `<LangVersion>13.0</LangVersion>` or higher; no project-level opt-in flag beyond the
  language version is needed (unlike the runtime-level Runtime Async feature — see
  [specialized/runtime-async-performance.md](../specialized/runtime-async-performance.md) — which
  is a separate, later change to how async is compiled, not a language syntax change).

## Fallback

Below C# 13, restructure so the `ref`/`unsafe` work happens in a **separate synchronous method**
called from the async one, since a synchronous method has never had this restriction:

```csharp
public async Task<long> ChecksumAsync(byte[] buffer)
{
    long checksum = ComputeChecksumSync(buffer); // ref local lives entirely inside this call
    await _sink.WriteAsync(checksum);
    return checksum;
}

private static long ComputeChecksumSync(byte[] buffer)
{
    ref byte start = ref buffer[0];
    return ComputeChecksum(ref start, buffer.Length);
}
```

Everything else about the surrounding `async`/`await` code is unchanged from
[csharp8-async-streams.md](csharp8-async-streams.md) and
[csharp5-async-await.md](csharp5-async-await.md).
