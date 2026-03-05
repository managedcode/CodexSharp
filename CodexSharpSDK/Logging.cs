using Microsoft.Extensions.Logging;

namespace ManagedCode.CodexSharpSDK;

internal static partial class CodexExecLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Starting Codex CLI '{ExecutablePath}' with {ArgumentCount} arguments.")]
    public static partial void Starting(ILogger logger, string executablePath, int argumentCount);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Codex CLI execution was cancelled.")]
    public static partial void Cancelled(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Codex CLI execution failed.")]
    public static partial void Failed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Codex CLI finished successfully with {LineCount} output lines.")]
    public static partial void Completed(ILogger logger, int lineCount);
}
