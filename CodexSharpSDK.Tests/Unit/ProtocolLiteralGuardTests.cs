namespace ManagedCode.CodexSharpSDK.Tests.Unit;

public class ProtocolLiteralGuardTests
{
    private static readonly string[] SourceDirectories = ["CodexSharpSDK", "src"];

    [Test]
    public async Task ItemsFile_DoesNotContainInlineThreadItemTypeLiterals()
    {
        var content = await File.ReadAllTextAsync(ResolveSdkSourceFilePath("Items.cs"));
        await Assert.That(content.Contains("ThreadItem(Id, \"", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task EventsFile_DoesNotContainInlineThreadEventTypeLiterals()
    {
        var content = await File.ReadAllTextAsync(ResolveSdkSourceFilePath("Events.cs"));
        await Assert.That(content.Contains("ThreadEvent(\"", StringComparison.Ordinal)).IsFalse();
    }

    private static string ResolveSdkSourceFilePath(string fileName)
    {
        var repositoryRoot = ResolveRepositoryRootPath();
        foreach (var sourceDirectory in SourceDirectories)
        {
            var sourceRoot = Path.Combine(repositoryRoot, sourceDirectory);
            if (!Directory.Exists(sourceRoot))
            {
                continue;
            }

            foreach (var candidatePath in Directory.EnumerateFiles(sourceRoot, fileName, SearchOption.AllDirectories))
            {
                return candidatePath;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate source file '{fileName}' under any known source directories: {string.Join(", ", SourceDirectories)}.");
    }

    private static string ResolveRepositoryRootPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ManagedCode.CodexSharpSDK.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }
}
