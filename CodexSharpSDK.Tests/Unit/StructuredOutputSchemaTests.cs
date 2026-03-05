namespace ManagedCode.CodexSharpSDK.Tests;

public class StructuredOutputSchemaTests
{
    [Test]
    public async Task Object_BuildsExpectedSchema()
    {
        var schema = StructuredOutputSchema.Map(
            new Dictionary<string, StructuredOutputSchema>
            {
                ["status"] = StructuredOutputSchema.PlainText(),
                ["score"] = StructuredOutputSchema.Numeric(),
                ["ok"] = StructuredOutputSchema.Flag(),
            },
            required: ["status"],
            additionalProperties: false);

        var json = schema.ToJsonObject();
        await Assert.That(json["type"]!.GetValue<string>()).IsEqualTo("object");
        await Assert.That(json["properties"]!["status"]!["type"]!.GetValue<string>()).IsEqualTo("string");
        await Assert.That(json["properties"]!["score"]!["type"]!.GetValue<string>()).IsEqualTo("number");
        await Assert.That(json["properties"]!["ok"]!["type"]!.GetValue<string>()).IsEqualTo("boolean");
        await Assert.That(json["required"]![0]!.GetValue<string>()).IsEqualTo("status");
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
                ["status"] = StructuredOutputSchema.PlainText(),
            },
            required: ["missing"]);

        var exception = await Assert.That(action).ThrowsException();
        await Assert.That(exception).IsTypeOf<ArgumentException>();
        await Assert.That(exception!.Message).Contains("must exist in schema properties");
    }
}
