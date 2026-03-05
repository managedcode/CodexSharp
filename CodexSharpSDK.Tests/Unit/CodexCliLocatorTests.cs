using ManagedCode.CodexSharpSDK.Internal;

namespace ManagedCode.CodexSharpSDK.Tests.Unit;

public class CodexCliLocatorTests
{
    [Test]
    public async Task GetPathExecutableCandidates_Windows_IncludeCommandWrappers()
    {
        var candidates = CodexCliLocator.GetPathExecutableCandidates(isWindows: true);

        await Assert.That(candidates).IsEquivalentTo(
        [
            CodexCliLocator.CodexWindowsExecutableName,
            $"{CodexCliLocator.CodexExecutableName}.cmd",
            $"{CodexCliLocator.CodexExecutableName}.bat",
            CodexCliLocator.CodexExecutableName,
        ]);
    }

    [Test]
    public async Task TryResolvePathExecutable_Windows_ResolvesCodexCmdWhenExeMissing()
    {
        var sandboxDirectory = CreateSandboxDirectory();

        try
        {
            var firstPathEntry = Path.Combine(sandboxDirectory, "first");
            var secondPathEntry = Path.Combine(sandboxDirectory, "second");
            Directory.CreateDirectory(firstPathEntry);
            Directory.CreateDirectory(secondPathEntry);

            var cmdPath = Path.Combine(secondPathEntry, $"{CodexCliLocator.CodexExecutableName}.cmd");
            await File.WriteAllTextAsync(cmdPath, "@echo off");

            var pathVariable = string.Join(Path.PathSeparator, firstPathEntry, secondPathEntry);

            var resolved = CodexCliLocator.TryResolvePathExecutable(pathVariable, isWindows: true, out var executablePath);
            await Assert.That(resolved).IsTrue();
            await Assert.That(executablePath).IsEqualTo(cmdPath);
        }
        finally
        {
            Directory.Delete(sandboxDirectory, recursive: true);
        }
    }

    [Test]
    public async Task TryResolvePathExecutable_Unix_DoesNotTreatCmdAsExecutable()
    {
        var sandboxDirectory = CreateSandboxDirectory();

        try
        {
            var pathEntry = Path.Combine(sandboxDirectory, "unix");
            Directory.CreateDirectory(pathEntry);

            var cmdPath = Path.Combine(pathEntry, $"{CodexCliLocator.CodexExecutableName}.cmd");
            await File.WriteAllTextAsync(cmdPath, "#!/usr/bin/env bash");

            var pathVariable = pathEntry;

            var resolved = CodexCliLocator.TryResolvePathExecutable(pathVariable, isWindows: false, out var executablePath);
            await Assert.That(resolved).IsFalse();
            await Assert.That(executablePath).IsEqualTo(string.Empty);
        }
        finally
        {
            Directory.Delete(sandboxDirectory, recursive: true);
        }
    }

    [Test]
    public async Task TryResolvePathExecutable_Unix_ResolvesCodexBinary()
    {
        var sandboxDirectory = CreateSandboxDirectory();

        try
        {
            var pathEntry = Path.Combine(sandboxDirectory, "unix");
            Directory.CreateDirectory(pathEntry);

            var binaryPath = Path.Combine(pathEntry, CodexCliLocator.CodexExecutableName);
            await File.WriteAllTextAsync(binaryPath, "#!/usr/bin/env bash");

            var resolved = CodexCliLocator.TryResolvePathExecutable(pathEntry, isWindows: false, out var executablePath);
            await Assert.That(resolved).IsTrue();
            await Assert.That(executablePath).IsEqualTo(binaryPath);
        }
        finally
        {
            Directory.Delete(sandboxDirectory, recursive: true);
        }
    }

    private static string CreateSandboxDirectory()
    {
        var sandboxDirectory = Path.Combine(
            Environment.CurrentDirectory,
            "tests",
            ".sandbox",
            $"CodexCliLocatorTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sandboxDirectory);
        return sandboxDirectory;
    }
}
