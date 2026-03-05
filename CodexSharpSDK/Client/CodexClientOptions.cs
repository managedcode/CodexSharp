using ManagedCode.CodexSharpSDK.Configuration;

namespace ManagedCode.CodexSharpSDK.Client;

public sealed record CodexClientOptions
{
    public CodexOptions? CodexOptions { get; init; }

    public bool AutoStart { get; init; } = true;
}
