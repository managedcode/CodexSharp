using ManagedCode.CodexSharpSDK.Internal;

namespace ManagedCode.CodexSharpSDK.Tests;

internal static class RealCodexTestSupport
{
    private const string RealIntegrationEnvVar = "CODEX_REAL_INTEGRATION";
    private const string ModelEnvVar = "CODEX_TEST_MODEL";
    private const string ApiKeyEnvVar = "OPENAI_API_KEY";
    private const string DefaultModel = "gpt-5.3-codex";

    public static RealCodexTestSettings? TryGetSettings()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RealIntegrationEnvVar), "1", StringComparison.Ordinal))
        {
            return null;
        }

        if (!IsCodexAvailable())
        {
            throw new InvalidOperationException("Real Codex tests require the codex CLI. Install it first and ensure it is available in PATH.");
        }

        var model = ResolveModel();
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);

        return new RealCodexTestSettings(model, apiKey);
    }

    public static CodexClient CreateClient(RealCodexTestSettings settings)
    {
        var options = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? new CodexOptions()
            : new CodexOptions { ApiKey = settings.ApiKey };

        return new CodexClient(options);
    }

    private static string ResolveModel()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ModelEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var fromConfig = TryReadModelFromCodexConfig();
        return string.IsNullOrWhiteSpace(fromConfig)
            ? DefaultModel
            : fromConfig;
    }

    private static string? TryReadModelFromCodexConfig()
    {
        try
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(homeDirectory))
            {
                return null;
            }

            var configPath = Path.Combine(homeDirectory, ".codex", "config.toml");
            if (!File.Exists(configPath))
            {
                return null;
            }

            foreach (var line in File.ReadLines(configPath))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("model", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                if (!string.Equals(key, "model", StringComparison.Ordinal))
                {
                    continue;
                }

                var rawValue = trimmed[(separatorIndex + 1)..].Trim();
                if (rawValue.Length >= 2
                    && rawValue.StartsWith('"')
                    && rawValue.EndsWith('"'))
                {
                    rawValue = rawValue[1..^1];
                }

                return string.IsNullOrWhiteSpace(rawValue)
                    ? null
                    : rawValue;
            }
        }
        catch
        {
            // If reading config fails, tests fall back to DefaultModel.
        }

        return null;
    }

    private static bool IsCodexAvailable()
    {
        var resolvedPath = CodexCliLocator.FindCodexPath(null);
        if (Path.IsPathRooted(resolvedPath))
        {
            return File.Exists(resolvedPath);
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return false;
        }

        var candidateNames = OperatingSystem.IsWindows()
            ? new[] { resolvedPath, $"{resolvedPath}.exe", $"{resolvedPath}.cmd", $"{resolvedPath}.bat" }
            : new[] { resolvedPath };

        foreach (var pathEntry in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var candidateName in candidateNames)
            {
                var candidatePath = Path.Combine(pathEntry, candidateName);
                if (File.Exists(candidatePath))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

internal sealed record RealCodexTestSettings(string Model, string? ApiKey);
