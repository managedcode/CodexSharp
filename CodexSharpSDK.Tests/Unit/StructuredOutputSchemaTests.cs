using ManagedCode.CodexSharpSDK.Models;

namespace ManagedCode.CodexSharpSDK.Tests.Unit;

public class StructuredOutputSchemaTests
{
    private sealed record ScoreStatusResponse(string Status, double Score, bool Ok);
    private sealed record MissingPropertyResponse(string Missing);

    [Test]
    public async Task Object_BuildsExpectedSchema()
    {
        var schema = StructuredOutputSchema.Map<ScoreStatusResponse>(
            additionalProperties: false,
            (response => response.Status, StructuredOutputSchema.PlainText()),
            (response => response.Score, StructuredOutputSchema.Numeric()),
            (response => response.Ok, StructuredOutputSchema.Flag()));

        var json = schema.ToJsonObject();
        await Assert.That(json["type"]!.GetValue<string>()).IsEqualTo("object");
        await Assert.That(json["properties"]![nameof(ScoreStatusResponse.Status)]!["type"]!.GetValue<string>()).IsEqualTo("string");
        await Assert.That(json["properties"]![nameof(ScoreStatusResponse.Score)]!["type"]!.GetValue<string>()).IsEqualTo("number");
        await Assert.That(json["properties"]![nameof(ScoreStatusResponse.Ok)]!["type"]!.GetValue<string>()).IsEqualTo("boolean");
        await Assert.That(json["required"]![0]!.GetValue<string>()).IsEqualTo(nameof(ScoreStatusResponse.Status));
        await Assert.That(json["additionalProperties"]!.GetValue<bool>()).IsFalse();
    }

    [Test]
    public async Task Object_ThrowsForInvalidPropertyName()
    {
        var action = () => StructuredOutputSchema.Map(
            new Dictionary<string, StructuredOutputSchema>
            {
                [" "] = StructuredOutputSchema.PlainText(),
            });

        var exception = await Assert.That(action).ThrowsException();
        await Assert.That(exception).IsTypeOf<ArgumentException>();
    }

    [Test]
    public async Task Object_ThrowsWhenRequiredPropertyMissingFromProperties()
    {
        var action = () => StructuredOutputSchema.Map(
            new Dictionary<string, StructuredOutputSchema>
            {
                [nameof(ScoreStatusResponse.Status)] = StructuredOutputSchema.PlainText(),
            },
            required: [nameof(MissingPropertyResponse.Missing)]);

        var exception = await Assert.That(action).ThrowsException();
        await Assert.That(exception).IsTypeOf<ArgumentException>();
        await Assert.That(exception!.Message).Contains("must exist in schema properties");
    }

    [Test]
    public async Task Object_ThrowsForUnsupportedPropertySelector()
    {
        var action = () => StructuredOutputSchema.Map<ScoreStatusResponse>(
            additionalProperties: false,
            (response => response.Status.Trim(), StructuredOutputSchema.PlainText()));

        var exception = await Assert.That(action).ThrowsException();
        await Assert.That(exception).IsTypeOf<ArgumentException>();
    }
}
