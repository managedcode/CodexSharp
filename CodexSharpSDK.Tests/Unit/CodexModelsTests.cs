using System.Reflection;
using System.Text.Json;
using ManagedCode.CodexSharpSDK.Models;

namespace ManagedCode.CodexSharpSDK.Tests.Unit;

public class CodexModelsTests
{
    private const string SolutionFileName = "ManagedCode.CodexSharpSDK.slnx";
    private const string BundledModelsFileName = "models.json";

    [Test]
    public async Task CodexModels_ContainAllBundledUpstreamModelSlugs()
    {
        var bundledModelSlugs = await ReadBundledModelSlugsAsync();
        var sdkModelSlugs = GetSdkModelSlugs();
        var missingBundledSlugs = bundledModelSlugs
            .Except(sdkModelSlugs, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(missingBundledSlugs).IsEmpty();
    }

    private static string[] GetSdkModelSlugs()
    {
        return typeof(CodexModels)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false, FieldType: not null } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();
    }

    private static async Task<string[]> ReadBundledModelSlugsAsync()
    {
        var modelsPath = ResolveBundledModelsFilePath();
        using var stream = File.OpenRead(modelsPath);
        using var document = await JsonDocument.ParseAsync(stream);

        return document.RootElement
            .GetProperty("models")
            .EnumerateArray()
            .Select(model => model.GetProperty("slug").GetString())
            .OfType<string>()
            .ToArray();
    }

    private static string ResolveBundledModelsFilePath()
    {
        return Path.Combine(
            ResolveRepositoryRootPath(),
            "submodules",
            "openai-codex",
            "codex-rs",
            "core",
            BundledModelsFileName);
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
}
