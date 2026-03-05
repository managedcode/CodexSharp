namespace ManagedCode.CodexSharpSDK;

public sealed record CodexClientOptions
{
    public CodexOptions? CodexOptions { get; init; }

    public bool AutoStart { get; init; } = true;
}
