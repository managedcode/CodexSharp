using Microsoft.Extensions.Logging;

namespace ManagedCode.CodexSharpSDK.Logging;

internal static partial class CodexThreadLog
{
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Warning,
        Message = "Failed to delete temporary image file '{TempFilePath}' during cleanup.")]
    public static partial void TemporaryImageDeleteFailed(ILogger logger, string tempFilePath, Exception exception);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Warning,
        Message = "Failed to delete output schema directory '{SchemaDirectory}' during cleanup.")]
    public static partial void OutputSchemaDeleteFailed(ILogger logger, string schemaDirectory, Exception exception);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "Failed to dispose owned local image input stream during cleanup.")]
    public static partial void InputStreamDisposeFailed(ILogger logger, Exception exception);
}
