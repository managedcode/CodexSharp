using ManagedCode.CodexSharpSDK.Models;

namespace ManagedCode.CodexSharpSDK.Client;

public sealed record TurnOptions
{
    public StructuredOutputSchema? OutputSchema { get; init; }

    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}
