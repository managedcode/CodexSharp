using ManagedCode.CodexSharpSDK.Extensions.AI.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.CodexSharpSDK.Extensions.AI.Tests;

public class CodexServiceCollectionExtensionsTests
{
    private const string ConfiguredDefaultModel = "configured-default-model";

    [Test]
    public async Task AddCodexChatClient_RegistersIChatClient()
    {
        var services = new ServiceCollection();
        services.AddCodexChatClient();
        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IChatClient>();
        await Assert.That(client).IsNotNull();
        await Assert.That(client).IsTypeOf<CodexChatClient>();
    }

    [Test]
    public async Task AddCodexChatClient_WithConfiguration_RegistersIChatClient()
    {
        var services = new ServiceCollection();
        services.AddCodexChatClient(options => options.DefaultModel = ConfiguredDefaultModel);
        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IChatClient>();
        await Assert.That(client).IsNotNull();

        var metadata = client!.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata!.DefaultModelId).IsEqualTo(ConfiguredDefaultModel);
    }

    [Test]
    public async Task AddKeyedCodexChatClient_RegistersWithKey()
    {
        var services = new ServiceCollection();
        services.AddKeyedCodexChatClient("codex");
        var provider = services.BuildServiceProvider();
        var client = provider.GetKeyedService<IChatClient>("codex");
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task AddKeyedCodexChatClient_WithConfiguration_AppliesConfiguredDefaultModel()
    {
        var services = new ServiceCollection();
        services.AddKeyedCodexChatClient("codex", options => options.DefaultModel = ConfiguredDefaultModel);
        var provider = services.BuildServiceProvider();
        var client = provider.GetKeyedService<IChatClient>("codex");

        await Assert.That(client).IsNotNull();

        var metadata = client!.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata!.DefaultModelId).IsEqualTo(ConfiguredDefaultModel);
    }
}
