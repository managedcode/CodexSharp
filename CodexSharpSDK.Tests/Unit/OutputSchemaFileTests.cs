using ManagedCode.CodexSharpSDK.Internal;

namespace ManagedCode.CodexSharpSDK.Tests;

public class OutputSchemaFileTests
{
    [Test]
    public async Task CreateAsync_ReturnsEmptyHandleWhenSchemaMissing()
    {
        await using var handle = await OutputSchemaFile.CreateAsync(null, CancellationToken.None);

        await Assert.That(handle.SchemaPath).IsNull();
    }

    [Test]
    public async Task CreateAsync_WritesAndCleansSchemaFile()
    {
        var schema = StructuredOutputSchema.Map(
            new Dictionary<string, StructuredOutputSchema>
            {
                ["answer"] = StructuredOutputSchema.PlainText(),
            },
            required: ["answer"],
            additionalProperties: false);

        string? schemaPath;

        await using (var handle = await OutputSchemaFile.CreateAsync(schema, CancellationToken.None))
        {
            schemaPath = handle.SchemaPath;
            await Assert.That(schemaPath).IsNotNull();
            await Assert.That(File.Exists(schemaPath!)).IsTrue();

            var contents = await File.ReadAllTextAsync(schemaPath!);
            await Assert.That(contents).Contains("\"type\":\"object\"");
            await Assert.That(contents).Contains("\"answer\"");
        }

        await Assert.That(schemaPath).IsNotNull();
        await Assert.That(File.Exists(schemaPath!)).IsFalse();
    }
}
