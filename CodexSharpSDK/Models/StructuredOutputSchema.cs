using System.Linq.Expressions;
using System.Text.Json.Nodes;

namespace ManagedCode.CodexSharpSDK.Models;

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

    public static StructuredOutputSchema Map<TModel>(
        bool? additionalProperties = null,
        params (Expression<Func<TModel, object?>> Property, StructuredOutputSchema Schema)[] properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Length == 0)
        {
            throw new ArgumentException("At least one property mapping is required.", nameof(properties));
        }

        var mappedProperties = new Dictionary<string, StructuredOutputSchema>(StringComparer.Ordinal);
        var requiredProperties = new List<string>(properties.Length);

        foreach (var (propertyExpression, schema) in properties)
        {
            ArgumentNullException.ThrowIfNull(propertyExpression);
            ArgumentNullException.ThrowIfNull(schema);

            var propertyName = ResolvePropertyName(propertyExpression.Body);
            mappedProperties[propertyName] = schema;
            requiredProperties.Add(propertyName);
        }

        return Map(mappedProperties, requiredProperties, additionalProperties);
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
                required.Add(CreateStringJsonNode(requiredProperty));
            }

            result[JsonTypeTokens.Required] = required;
        }

        if (AdditionalProperties.HasValue)
        {
            result[JsonTypeTokens.AdditionalProperties] = AdditionalProperties.Value;
        }

        return result;
    }

    private static string ResolvePropertyName(Expression expression)
    {
        if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
        {
            expression = unaryExpression.Operand;
        }

        if (expression is MemberExpression memberExpression && memberExpression.Member.MemberType == System.Reflection.MemberTypes.Property)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Property selector must point to a model property.");
    }

    private static JsonNode CreateStringJsonNode(string value)
    {
        var escapedValue = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return JsonNode.Parse($"\"{escapedValue}\"")
               ?? throw new InvalidOperationException("Failed to create JSON node for required property.");
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
