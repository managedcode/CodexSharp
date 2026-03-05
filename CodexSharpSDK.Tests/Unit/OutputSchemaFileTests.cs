using ManagedCode.CodexSharpSDK.Internal;
using ManagedCode.CodexSharpSDK.Models;

namespace ManagedCode.CodexSharpSDK.Tests.Unit;

public class OutputSchemaFileTests
{
    private sealed record AnswerPayload(string Answer);

    [Test]
    public async Task CreateAsync_ReturnsEmptyHandleWhenSchemaMissing()
    {
        await using var handle = await OutputSchemaFile.CreateAsync(null, logger: null, CancellationToken.None);

        await Assert.That(handle.SchemaPath).IsNull();
    }

    [Test]
    public async Task CreateAsync_WritesAndCleansSchemaFile()
    {
        var schema = StructuredOutputSchema.Map<AnswerPayload>(
            additionalProperties: false,
            (response => response.Answer, StructuredOutputSchema.PlainText()));

        string? schemaPath;

        await using (var handle = await OutputSchemaFile.CreateAsync(schema, logger: null, CancellationToken.None))
        {
            schemaPath = handle.SchemaPath;
            await Assert.That(schemaPath).IsNotNull();
            await Assert.That(File.Exists(schemaPath!)).IsTrue();

            var contents = await File.ReadAllTextAsync(schemaPath!);
            await Assert.That(contents).Contains("\"type\":\"object\"");
            await Assert.That(contents).Contains($"\"{nameof(AnswerPayload.Answer)}\"");
        }

        await Assert.That(schemaPath).IsNotNull();
        await Assert.That(File.Exists(schemaPath!)).IsFalse();
    }
}
