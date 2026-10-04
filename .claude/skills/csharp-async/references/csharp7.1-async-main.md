# `async Main` (C# 7.1 / .NET Core 2.0+)

C# 7.1 shipped August 2017, the first of C#'s "point releases," alongside .NET Core 2.0. Before
this release, an application's entry point (`Main`) could only be `void` or `int`, which forced any
top-level asynchronous startup logic into a blocking `.Wait()`/`.Result` call or a synchronous
wrapper method. C# 7.1 allows `Main` itself to be `async`, returning `Task` or `Task<int>`.

## Syntax

```csharp
public static async Task<int> Main(string[] args)
{
    IHost host = await BuildHostAsync(args);
    return await host.RunAsync();
}
```

Requires `<LangVersion>7.1</LangVersion>` or higher in the project file (or a target framework
whose default language version is 7.1+).

## Basic use case

```csharp
public static async Task Main(string[] args)
{
    using HttpClient client = new();
    string result = await client.GetStringAsync(args[0]);
    Console.WriteLine(result);
}
```

No `.Wait()`, no `.GetAwaiter().GetResult()`, no risk of the deadlock those blocking calls can
cause when a `SynchronizationContext` is present — the compiler generates the equivalent wrapper
for you.

## Advanced use case: returning a process exit code from awaited work

```csharp
public static async Task<int> Main(string[] args)
{
    try
    {
        await RunApplicationAsync(args);
        return 0;
    }
    catch (ApplicationException ex)
    {
        await Console.Error.WriteLineAsync(ex.Message);
        return 1;
    }
}
```

`Task<int>` as `Main`'s return type maps directly to the process exit code, the same way a
synchronous `static int Main()` would — `async` doesn't change that contract, just how the value is
produced.

## Requirements and restrictions

- Only one of the following signatures is valid per entry point: `static async Task Main()`,
  `static async Task Main(string[] args)`, `static async Task<int> Main()`, or
  `static async Task<int> Main(string[] args)`. `async void Main` is not permitted.
- A project with multiple candidate entry points still needs the usual `StartupObject` disambiguation;
  `async Main` doesn't change entry-point selection rules.

## Fallback

Below C# 7.1, write a synchronous `Main` that blocks on an async `Task`-returning method:

```csharp
public static int Main(string[] args)
{
    return MainAsync(args).GetAwaiter().GetResult();
}

private static async Task<int> MainAsync(string[] args)
{
    await RunApplicationAsync(args);
    return 0;
}
```

`.GetAwaiter().GetResult()` (rather than `.Result`) avoids wrapping a faulting task's exception in
`AggregateException` — see
[specialized/exception-handling-in-async-code.md](../specialized/exception-handling-in-async-code.md).
Everything else about the awaited work itself is unchanged from
[csharp5-async-await.md](csharp5-async-await.md).
