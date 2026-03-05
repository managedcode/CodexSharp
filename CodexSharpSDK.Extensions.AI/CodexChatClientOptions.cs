using ManagedCode.CodexSharpSDK.Client;
using ManagedCode.CodexSharpSDK.Configuration;

namespace ManagedCode.CodexSharpSDK.Extensions.AI;

public sealed record CodexChatClientOptions
{
    public CodexOptions? CodexOptions { get; set; }
    public string? DefaultModel { get; set; }
    public ThreadOptions? DefaultThreadOptions { get; set; }
}
