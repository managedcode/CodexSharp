using System.Text.Json;
using ManagedCode.CodexSharpSDK.Models;

namespace ManagedCode.CodexSharpSDK.Tests.Shared;

internal sealed record StatusResponse(string Status);

internal sealed record RepositorySummaryResponse(string Summary, string Status);

internal static class IntegrationOutputSchemas
{
    public static StructuredOutputSchema StatusOnly()
    {
        return StructuredOutputSchema.Map<StatusResponse>(
            additionalProperties: false,
            (response => response.Status, StructuredOutputSchema.PlainText()));
    }

    public static StructuredOutputSchema SummaryAndStatus()
    {
        return StructuredOutputSchema.Map<RepositorySummaryResponse>(
            additionalProperties: false,
            (response => response.Summary, StructuredOutputSchema.PlainText()),
            (response => response.Status, StructuredOutputSchema.PlainText()));
    }
}

internal static class IntegrationOutputDeserializer
{
    public static T Deserialize<T>(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var model = JsonSerializer.Deserialize<T>(payload) ?? throw new InvalidOperationException($"Failed to deserialize integration payload to {typeof(T).Name}.");

        return model;
    }
}
