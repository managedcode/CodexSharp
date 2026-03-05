# ManagedCode.CodexSharpSDK

[![CI](https://github.com/managedcode/CodexSharpSDK/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/managedcode/CodexSharpSDK/actions/workflows/ci.yml)
[![Release](https://github.com/managedcode/CodexSharpSDK/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/managedcode/CodexSharpSDK/actions/workflows/release.yml)
[![CodeQL](https://github.com/managedcode/CodexSharpSDK/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/managedcode/CodexSharpSDK/actions/workflows/codeql.yml)
[![NuGet](https://img.shields.io/nuget/v/ManagedCode.CodexSharpSDK.svg)](https://www.nuget.org/packages/ManagedCode.CodexSharpSDK)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

`ManagedCode.CodexSharpSDK` is an open-source .NET SDK for driving the Codex CLI from C#.

It ports the TypeScript SDK from `openai/codex` to `.NET 10 / C# 14` with:
- thread-based API (`start` / `resume`)
- streamed JSONL events
- structured output schema support
- image attachments
- `--config` flattening to TOML
- NativeAOT-friendly implementation and tests on TUnit

All consumer usage examples are documented in this README; this repository intentionally does not keep standalone sample projects.

## Installation

```bash
dotnet add package ManagedCode.CodexSharpSDK
```

## Namespace Migration

`ManagedCode.CodexSharp` was renamed to `ManagedCode.CodexSharpSDK`.

Consumer update:

```csharp
// old
using ManagedCode.CodexSharp;

// new
using ManagedCode.CodexSharpSDK;
```

## Quickstart

```csharp
using ManagedCode.CodexSharpSDK;

await using var client = new CodexClient();

var thread = client.StartThread(new ThreadOptions
{
    Model = "gpt-5.3-codex",
    ModelReasoningEffort = ModelReasoningEffort.Medium,
});

var turn = await thread.RunAsync("Diagnose failing tests and propose a fix");

Console.WriteLine(turn.FinalResponse);
Console.WriteLine($"Items: {turn.Items.Count}");
```

`AutoStart` is enabled by default, so `StartThread()` works immediately.

## Advanced Configuration (Optional)

```csharp
await using var client = new CodexClient(new CodexClientOptions
{
    CodexOptions = new CodexOptions
    {
        // Override only when `codex` is not discoverable via npm/PATH.
        CodexPathOverride = "/custom/path/to/codex",
    },
});

var thread = client.StartThread(new ThreadOptions
{
    Model = "gpt-5.3-codex",
    ModelReasoningEffort = ModelReasoningEffort.High,
    SandboxMode = SandboxMode.WorkspaceWrite,
});
```

## Client Lifecycle and Thread Safety

- `CodexClient` is safe for concurrent use from multiple threads.
- `StartAsync()` is idempotent and guarded.
- `StopAsync()` cleanly disconnects client state.
- `Dispose()/DisposeAsync()` transition client to `Disposed`.
- A single `CodexThread` instance serializes turns (`RunAsync` and `RunStreamedAsync`) to prevent race conditions in shared conversation state.

## Streaming

```csharp
var streamed = await thread.RunStreamedAsync("Implement the fix");

await foreach (var evt in streamed.Events)
{
    switch (evt)
    {
        case ItemCompletedEvent completed:
            Console.WriteLine($"Item: {completed.Item.Type}");
            break;
        case TurnCompletedEvent done:
            Console.WriteLine($"Output tokens: {done.Usage.OutputTokens}");
            break;
    }
}
```

## Structured Output

```csharp
var schema = StructuredOutputSchema.Map(
    new Dictionary<string, StructuredOutputSchema>
    {
        ["summary"] = StructuredOutputSchema.PlainText(),
        ["status"] = StructuredOutputSchema.PlainText(),
    },
    required: ["summary", "status"],
    additionalProperties: false);

var result = await thread.RunAsync(
    "Summarize repository status",
    new TurnOptions { OutputSchema = schema });
```

## Diagnostics Logging (Optional)

```csharp
using Microsoft.Extensions.Logging;

public sealed class ConsoleCodexLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        if (exception is not null)
        {
            Console.WriteLine(exception);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

await using var client = new CodexClient(new CodexOptions
{
    Logger = new ConsoleCodexLogger(),
});
```

## Images + Text Input

```csharp
using var imageStream = File.OpenRead("./photo.png");

var result = await thread.RunAsync(
[
    new TextInput("Describe these images"),
    new LocalImageInput("./ui.png"),
    new LocalImageInput(new FileInfo("./diagram.jpg")),
    new LocalImageInput(imageStream, "photo.png"),
]);
```

## Resume an Existing CodexThread

```csharp
var resumed = client.ResumeThread("thread_123");
await resumed.RunAsync("Continue from previous plan");
```

## Build and Test

```bash
dotnet build ManagedCode.CodexSharpSDK.slnx -c Release -warnaserror
dotnet test --solution ManagedCode.CodexSharpSDK.slnx -c Release
```

## AOT Smoke Check

```bash
dotnet publish tests/AotSmoke/ManagedCode.CodexSharpSDK.AotSmoke.csproj \
  -c Release -r linux-x64 -p:PublishAot=true --self-contained true
```

## Porting References

- TypeScript source of truth: [`submodules/openai-codex/sdk/typescript`](submodules/openai-codex/sdk/typescript)
- Detailed migration checklist: [`PORTING_TODO.md`](PORTING_TODO.md)
- Dotnet style reference: [github/copilot-sdk](https://github.com/github/copilot-sdk/tree/main/dotnet)
- CI/release style reference: [managedcode/Storage](https://github.com/managedcode/Storage)
