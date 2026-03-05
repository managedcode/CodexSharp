using System.Text.Json.Nodes;

namespace ManagedCode.CodexSharpSDK;

public enum StructuredOutputSchemaType
{
    PlainText,
    Numeric,
    Flag,
    Map,
    Sequence,
}

public sealed record StructuredOutputSchema
{
    public required StructuredOutputSchemaType Type { get; init; }

    public IReadOnlyDictionary<string, StructuredOutputSchema>? Properties { get; init; }

    public IReadOnlyList<string>? Required { get; init; }

    public StructuredOutputSchema? Items { get; init; }

    public bool? AdditionalProperties { get; init; }

    public static StructuredOutputSchema PlainText()
    {
        return new StructuredOutputSchema
        {
            Type = StructuredOutputSchemaType.PlainText,
        };
    }

    public static StructuredOutputSchema Numeric()
    {
        return new StructuredOutputSchema
        {
            Type = StructuredOutputSchemaType.Numeric,
        };
    }

    public static StructuredOutputSchema Flag()
    {
        return new StructuredOutputSchema
        {
            Type = StructuredOutputSchemaType.Flag,
        };
    }

    public static StructuredOutputSchema Sequence(StructuredOutputSchema items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new StructuredOutputSchema
        {
            Type = StructuredOutputSchemaType.Sequence,
            Items = items,
        };
    }

    public static StructuredOutputSchema Map(
        IReadOnlyDictionary<string, StructuredOutputSchema> properties,
        IReadOnlyList<string>? required = null,
        bool? additionalProperties = null)
    {
        ArgumentNullException.ThrowIfNull(properties);

        foreach (var propertyName in properties.Keys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        }

        if (required is not null)
        {
            foreach (var requiredProperty in required)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(requiredProperty);
                if (!properties.ContainsKey(requiredProperty))
                {
                    throw new ArgumentException(
                        $"Required property '{requiredProperty}' must exist in schema properties.",
                        nameof(required));
                }
            }
        }

        return new StructuredOutputSchema
        {
            Type = StructuredOutputSchemaType.Map,
            Properties = new Dictionary<string, StructuredOutputSchema>(properties),
            Required = required?.ToArray(),
            AdditionalProperties = additionalProperties,
        };
    }

    internal JsonObject ToJsonObject()
    {
        return Type switch
        {
            StructuredOutputSchemaType.PlainText => PrimitiveSchema(JsonTypeTokens.String),
            StructuredOutputSchemaType.Numeric => PrimitiveSchema(JsonTypeTokens.Number),
            StructuredOutputSchemaType.Flag => PrimitiveSchema(JsonTypeTokens.Boolean),
            StructuredOutputSchemaType.Sequence => ArraySchema(),
            StructuredOutputSchemaType.Map => ObjectSchema(),
            _ => throw new InvalidOperationException($"Unsupported schema type: {Type}."),
        };
    }

    private static JsonObject PrimitiveSchema(string typeToken)
    {
        return new JsonObject
        {
            [JsonTypeTokens.Type] = typeToken,
        };
    }

    private JsonObject ArraySchema()
    {
        if (Items is null)
        {
            throw new InvalidOperationException("Array schema requires Items.");
        }

        return new JsonObject
        {
            [JsonTypeTokens.Type] = JsonTypeTokens.Array,
            [JsonTypeTokens.Items] = Items.ToJsonObject(),
        };
    }

    private JsonObject ObjectSchema()
    {
        if (Properties is null)
        {
            throw new InvalidOperationException("Object schema requires Properties.");
        }

        var properties = new JsonObject();
        foreach (var (name, schema) in Properties)
        {
            properties[name] = schema.ToJsonObject();
        }

        var result = new JsonObject
        {
            [JsonTypeTokens.Type] = JsonTypeTokens.Object,
            [JsonTypeTokens.Properties] = properties,
        };

        if (Required is { Count: > 0 })
        {
            var required = new JsonArray();
            foreach (var requiredProperty in Required)
            {
                required.Add((JsonNode?)JsonValue.Create(requiredProperty));
            }

            result[JsonTypeTokens.Required] = required;
        }

        if (AdditionalProperties.HasValue)
        {
            result[JsonTypeTokens.AdditionalProperties] = AdditionalProperties.Value;
        }

        return result;
    }

    private static class JsonTypeTokens
    {
        public const string Type = "type";
        public const string String = "string";
        public const string Number = "number";
        public const string Boolean = "boolean";
        public const string Object = "object";
        public const string Array = "array";
        public const string Properties = "properties";
        public const string Required = "required";
        public const string AdditionalProperties = "additionalProperties";
        public const string Items = "items";
    }
}
