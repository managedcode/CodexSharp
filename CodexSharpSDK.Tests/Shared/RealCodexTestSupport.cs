using System.Collections;
using System.Diagnostics;
using ManagedCode.CodexSharpSDK.Client;
using ManagedCode.CodexSharpSDK.Configuration;
using ManagedCode.CodexSharpSDK.Execution;
using ManagedCode.CodexSharpSDK.Internal;
using Microsoft.Extensions.Logging;

namespace ManagedCode.CodexSharpSDK.Tests.Shared;

internal static class RealCodexTestSupport
{
    private const string ModelEnvVar = "CODEX_TEST_MODEL";
    private const string SolutionFileName = "ManagedCode.CodexSharpSDK.slnx";
    private const string TestsDirectoryName = "tests";
    private const string SandboxDirectoryName = ".sandbox";
    private const string SandboxPrefix = "RealCodexTestSupport-";
    private const string CodexHomeEnvironmentVariable = "CODEX_HOME";
    private const string HomeEnvironmentVariable = "HOME";
    private const string UserProfileEnvironmentVariable = "USERPROFILE";
    private const string XdgConfigHomeEnvironmentVariable = "XDG_CONFIG_HOME";
    private const string AppDataEnvironmentVariable = "APPDATA";
    private const string LocalAppDataEnvironmentVariable = "LOCALAPPDATA";
    private const string OpenAiApiKeyEnvironmentVariable = "OPENAI_API_KEY";
    private const string OpenAiBaseUrlEnvironmentVariable = "OPENAI_BASE_URL";
    private const string CodexApiKeyEnvironmentVariable = "CODEX_API_KEY";
    private const string CodexHomeDirectoryName = ".codex";
    private const string ConfigDirectoryName = ".config";
    private const string AppDataDirectoryName = "AppData";
    private const string RoamingDirectoryName = "Roaming";
    private const string LocalDirectoryName = "Local";
    private const string ConfigFileName = "config.toml";
    private const string SessionsDirectoryName = "sessions";
    private const string AuthFileName = "auth.json";
    private const string KosAuthFileName = "kos-auth.json";
    private const string SandboxCleanupFailureDataKey = "SandboxCleanupFailure";

    public static RealCodexTestSettings GetRequiredSettings()
    {
        if (!IsCodexAvailable())
        {
            throw new InvalidOperationException(
                "Real Codex tests require the codex CLI. Install it first and ensure it is available in PATH.");
        }

        var sandboxDirectory = CreateSandboxDirectory();
        try
        {
            var model = ResolveModel();
            var environmentOverrides = CreateAuthenticatedEnvironmentOverrides(sandboxDirectory, model);
            return new RealCodexTestSettings(
                model,
                sandboxDirectory,
                environmentOverrides[CodexHomeEnvironmentVariable],
                environmentOverrides);
        }
        catch (Exception exception)
        {
            AttachCleanupFailure(exception, sandboxDirectory);
            throw;
        }
    }

    public static CodexClient CreateClient(RealCodexTestSettings settings, CodexOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new CodexClient(CreateCodexOptions(settings, options));
    }

    public static CodexExec CreateExec(RealCodexTestSettings settings, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new CodexExec(environmentOverride: settings.EnvironmentOverrides, logger: logger);
    }

    public static async Task<string?> FindPersistedRolloutPathAsync(
        RealCodexTestSettings settings,
        string threadId,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);

        var sessionsPath = Path.Combine(settings.CodexHomeDirectory, SessionsDirectoryName);
        if (!Directory.Exists(sessionsPath))
        {
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var rolloutPath = Directory
                .EnumerateFiles(sessionsPath, $"rollout-*{threadId}.jsonl", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rolloutPath))
            {
                return rolloutPath;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
        }

        return null;
    }

    private static string ResolveModel()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ModelEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var fromConfig = TryReadModelFromCodexConfig();
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        throw new InvalidOperationException(
            $"Real Codex tests require a model. Set {ModelEnvVar} or define model in '~/.codex/config.toml'.");
    }

    private static string? TryReadModelFromCodexConfig()
    {
        var configPath = GetCodexConfigPath();
        if (configPath is null || !File.Exists(configPath))
        {
            return null;
        }

        try
        {
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
        catch (IOException exception)
        {
            throw new InvalidOperationException($"Failed to read Codex config at '{configPath}'.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException($"Failed to read Codex config at '{configPath}'.", exception);
        }

        return null;
    }

    private static string? GetCodexConfigPath()
    {
        var codexHomeDirectory = GetSourceCodexHomePath();
        if (string.IsNullOrWhiteSpace(codexHomeDirectory))
        {
            return null;
        }

        return Path.Combine(codexHomeDirectory, ConfigFileName);
    }

    private static string? GetSourceCodexHomePath()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            return null;
        }

        return Path.Combine(homeDirectory, CodexHomeDirectoryName);
    }

    private static bool IsCodexAvailable()
    {
        var resolvedPath = CodexCliLocator.FindCodexPath(null);
        if (Path.IsPathRooted(resolvedPath))
        {
            return File.Exists(resolvedPath);
        }

        return CodexCliLocator.TryResolvePathExecutable(
            Environment.GetEnvironmentVariable("PATH"),
            OperatingSystem.IsWindows(),
            out _);
    }

    private static CodexOptions CreateCodexOptions(RealCodexTestSettings settings, CodexOptions? options)
    {
        var environmentOverrides = new Dictionary<string, string>(settings.EnvironmentOverrides, StringComparer.Ordinal);
        if (options?.EnvironmentVariables is not null)
        {
            foreach (var (key, value) in options.EnvironmentVariables)
            {
                environmentOverrides[key] = value;
            }
        }

        return new CodexOptions
        {
            CodexExecutablePath = options?.CodexExecutablePath,
            BaseUrl = options?.BaseUrl,
            ApiKey = options?.ApiKey,
            Config = options?.Config,
            EnvironmentVariables = environmentOverrides,
            Logger = options?.Logger,
        };
    }

    private static Dictionary<string, string> CreateAuthenticatedEnvironmentOverrides(string sandboxDirectory, string model)
    {
        var codexHome = Path.Combine(sandboxDirectory, CodexHomeDirectoryName);
        var configHome = Path.Combine(sandboxDirectory, ConfigDirectoryName);
        var appData = Path.Combine(sandboxDirectory, AppDataDirectoryName, RoamingDirectoryName);
        var localAppData = Path.Combine(sandboxDirectory, AppDataDirectoryName, LocalDirectoryName);

        Directory.CreateDirectory(codexHome);
        Directory.CreateDirectory(configHome);
        Directory.CreateDirectory(appData);
        Directory.CreateDirectory(localAppData);

        WriteIsolatedCodexConfig(codexHome, model);
        CopyAuthenticationArtifacts(codexHome);

        var environmentOverrides = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(entry => entry.Key is string && entry.Value is not null)
            .ToDictionary(
                entry => entry.Key.ToString() ?? string.Empty,
                entry => entry.Value?.ToString() ?? string.Empty,
                StringComparer.Ordinal);

        environmentOverrides[CodexHomeEnvironmentVariable] = codexHome;
        environmentOverrides[HomeEnvironmentVariable] = sandboxDirectory;
        environmentOverrides[UserProfileEnvironmentVariable] = sandboxDirectory;
        environmentOverrides[XdgConfigHomeEnvironmentVariable] = configHome;
        environmentOverrides[AppDataEnvironmentVariable] = appData;
        environmentOverrides[LocalAppDataEnvironmentVariable] = localAppData;
        environmentOverrides[OpenAiApiKeyEnvironmentVariable] = string.Empty;
        environmentOverrides[OpenAiBaseUrlEnvironmentVariable] = string.Empty;
        environmentOverrides[CodexApiKeyEnvironmentVariable] = string.Empty;

        return environmentOverrides;
    }

    private static void WriteIsolatedCodexConfig(string codexHomeDirectory, string model)
    {
        var escapedModel = model
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        var configPath = Path.Combine(codexHomeDirectory, ConfigFileName);
        File.WriteAllText(configPath, $"model = \"{escapedModel}\"{Environment.NewLine}");
    }

    private static void CopyAuthenticationArtifacts(string codexHomeDirectory)
    {
        var sourceCodexHome = GetSourceCodexHomePath();
        if (string.IsNullOrWhiteSpace(sourceCodexHome) || !Directory.Exists(sourceCodexHome))
        {
            throw new InvalidOperationException(
                "Real Codex tests require an existing local Codex login. Run 'codex login' first.");
        }

        var copiedFiles = 0;
        copiedFiles += CopyAuthenticationArtifactIfExists(sourceCodexHome, codexHomeDirectory, AuthFileName);
        copiedFiles += CopyAuthenticationArtifactIfExists(sourceCodexHome, codexHomeDirectory, KosAuthFileName);

        if (copiedFiles == 0)
        {
            throw new InvalidOperationException(
                "Real Codex tests require an existing local Codex login. Run 'codex login' first.");
        }
    }

    private static int CopyAuthenticationArtifactIfExists(
        string sourceCodexHome,
        string destinationCodexHome,
        string fileName)
    {
        var sourcePath = Path.Combine(sourceCodexHome, fileName);
        if (!File.Exists(sourcePath))
        {
            return 0;
        }

        var destinationPath = Path.Combine(destinationCodexHome, fileName);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return 1;
    }

    private static string CreateSandboxDirectory()
    {
        var repositoryRoot = ResolveRepositoryRootPath();
        var sandboxDirectory = Path.Combine(
            repositoryRoot,
            TestsDirectoryName,
            SandboxDirectoryName,
            $"{SandboxPrefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(sandboxDirectory);
        return sandboxDirectory;
    }

    private static string ResolveRepositoryRootPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }

    private static void AttachCleanupFailure(Exception originalException, string sandboxDirectory)
    {
        try
        {
            if (Directory.Exists(sandboxDirectory))
            {
                Directory.Delete(sandboxDirectory, recursive: true);
            }
        }
        catch (IOException cleanupException)
        {
            originalException.Data[SandboxCleanupFailureDataKey] = cleanupException;
        }
        catch (UnauthorizedAccessException cleanupException)
        {
            originalException.Data[SandboxCleanupFailureDataKey] = cleanupException;
        }
    }
}

internal sealed class RealCodexTestSettings(
    string model,
    string sandboxDirectory,
    string codexHomeDirectory,
    IReadOnlyDictionary<string, string> environmentOverrides) : IDisposable
{
    public string Model { get; } = model;

    public string SandboxDirectory { get; } = sandboxDirectory;

    public string CodexHomeDirectory { get; } = codexHomeDirectory;

    public IReadOnlyDictionary<string, string> EnvironmentOverrides { get; } = environmentOverrides;

    public void Dispose()
    {
        if (Directory.Exists(SandboxDirectory))
        {
            Directory.Delete(SandboxDirectory, recursive: true);
        }
    }
}
