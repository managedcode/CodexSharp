using System.Runtime.InteropServices;

namespace ManagedCode.CodexSharpSDK.Internal;

internal static class CodexCliLocator
{
    internal const string CodexExecutableName = "codex";
    internal const string CodexWindowsExecutableName = "codex.exe";

    private static readonly string[] WindowsPathExecutableCandidates =
    [
        CodexWindowsExecutableName,
        $"{CodexExecutableName}.cmd",
        $"{CodexExecutableName}.bat",
        CodexExecutableName,
    ];

    private static readonly string[] UnixPathExecutableCandidates =
    [
        CodexExecutableName,
    ];

    private static readonly Dictionary<string, string> PlatformPackageByTarget =
        new(StringComparer.Ordinal)
        {
            ["x86_64-unknown-linux-musl"] = "@openai/codex-linux-x64",
            ["aarch64-unknown-linux-musl"] = "@openai/codex-linux-arm64",
            ["x86_64-apple-darwin"] = "@openai/codex-darwin-x64",
            ["aarch64-apple-darwin"] = "@openai/codex-darwin-arm64",
            ["x86_64-pc-windows-msvc"] = "@openai/codex-win32-x64",
            ["aarch64-pc-windows-msvc"] = "@openai/codex-win32-arm64",
        };

    public static string FindCodexPath(string? codexPathOverride)
    {
        if (!string.IsNullOrWhiteSpace(codexPathOverride))
        {
            return codexPathOverride;
        }

        if (TryResolveNpmInstalledBinary(out var resolvedPath))
        {
            return resolvedPath;
        }

        if (TryResolvePathExecutable(Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsWindows(), out var pathExecutable))
        {
            return pathExecutable;
        }

        return OperatingSystem.IsWindows()
            ? CodexWindowsExecutableName
            : CodexExecutableName;
    }

    internal static bool TryResolvePathExecutable(string? pathVariable, bool isWindows, out string executablePath)
    {
        executablePath = string.Empty;

        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return false;
        }

        var candidateNames = GetPathExecutableCandidates(isWindows);
        foreach (var pathEntry in SplitPathVariable(pathVariable))
        {
            foreach (var candidateName in candidateNames)
            {
                var candidatePath = Path.Combine(pathEntry, candidateName);
                if (File.Exists(candidatePath))
                {
                    executablePath = candidatePath;
                    return true;
                }
            }
        }

        return false;
    }

    internal static IReadOnlyList<string> GetPathExecutableCandidates(bool isWindows)
    {
        return isWindows
            ? WindowsPathExecutableCandidates
            : UnixPathExecutableCandidates;
    }

    private static bool TryResolveNpmInstalledBinary(out string binaryPath)
    {
        binaryPath = string.Empty;

        var targetTriple = GetTargetTriple();
        if (targetTriple is null)
        {
            return false;
        }

        if (!PlatformPackageByTarget.TryGetValue(targetTriple, out var packageName))
        {
            return false;
        }

        const string scopePrefix = "@openai/";
        if (!packageName.StartsWith(scopePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var packageDirectory = packageName[scopePrefix.Length..];
        var executableName = OperatingSystem.IsWindows() ? "codex.exe" : "codex";

        foreach (var root in EnumerateSearchRoots())
        {
            var primaryPath = Path.Combine(
                root,
                "node_modules",
                "@openai",
                packageDirectory,
                "vendor",
                targetTriple,
                "codex",
                executableName);

            if (File.Exists(primaryPath))
            {
                binaryPath = primaryPath;
                return true;
            }

            var nestedPath = Path.Combine(
                root,
                "node_modules",
                "@openai",
                "codex",
                "node_modules",
                "@openai",
                packageDirectory,
                "vendor",
                targetTriple,
                "codex",
                executableName);

            if (File.Exists(nestedPath))
            {
                binaryPath = nestedPath;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> SplitPathVariable(string pathVariable)
    {
        foreach (var rawPathEntry in pathVariable.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmedPathEntry = rawPathEntry.Trim('"');
            if (string.IsNullOrWhiteSpace(trimmedPathEntry))
            {
                continue;
            }

            yield return trimmedPathEntry;
        }
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in EnumerateUpwards(Environment.CurrentDirectory))
        {
            if (seen.Add(root))
            {
                yield return root;
            }
        }

        foreach (var root in EnumerateUpwards(AppContext.BaseDirectory))
        {
            if (seen.Add(root))
            {
                yield return root;
            }
        }
    }

    private static IEnumerable<string> EnumerateUpwards(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            yield break;
        }

        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static string? GetTargetTriple()
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x86_64-unknown-linux-musl",
                Architecture.Arm64 => "aarch64-unknown-linux-musl",
                _ => null,
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x86_64-apple-darwin",
                Architecture.Arm64 => "aarch64-apple-darwin",
                _ => null,
            };
        }

        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x86_64-pc-windows-msvc",
                Architecture.Arm64 => "aarch64-pc-windows-msvc",
                _ => null,
            };
        }

        return null;
    }
}
