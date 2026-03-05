namespace ManagedCode.CodexSharpSDK.Models;

public sealed record RunResult(IReadOnlyList<ThreadItem> Items, string FinalResponse, Usage? Usage);

public sealed record RunStreamedResult(IAsyncEnumerable<ThreadEvent> Events);
