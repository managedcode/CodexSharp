using System.Text.Json.Nodes;

namespace ManagedCode.CodexSharpSDK.Tests;

public class RealCodexIntegrationTests
{
    [Test]
    public async Task RunAsync_WithRealCodexCli_ReturnsStructuredOutput()
    {
        var settings = RealCodexTestSupport.TryGetSettings();
        if (settings is null)
        {
            return;
        }

        await using var client = RealCodexTestSupport.CreateClient(settings);
        var thread = StartRealIntegrationThread(client, settings.Model);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var schema = StructuredOutputSchema.Map(
            new Dictionary<string, StructuredOutputSchema>
            {
                ["status"] = StructuredOutputSchema.PlainText(),
            },
            required: ["status"],
            additionalProperties: false);

        var result = await thread.RunAsync(
            "Reply with a JSON object where status is exactly \"ok\".",
            new TurnOptions
            {
                OutputSchema = schema,
                CancellationToken = cancellation.Token,
            });

        var json = JsonNode.Parse(result.FinalResponse)?.AsObject();
        await Assert.That(json).IsNotNull();
        await Assert.That(json!["status"]!.GetValue<string>()).IsEqualTo("ok");
        await Assert.That(result.Usage).IsNotNull();
    }

    [Test]
    public async Task RunStreamedAsync_WithRealCodexCli_YieldsCompletedTurnEvent()
    {
        var settings = RealCodexTestSupport.TryGetSettings();
        if (settings is null)
        {
            return;
        }

        await using var client = RealCodexTestSupport.CreateClient(settings);
        var thread = StartRealIntegrationThread(client, settings.Model);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var streamed = await thread.RunStreamedAsync(
            "Reply with short plain text: ok.",
            new TurnOptions { CancellationToken = cancellation.Token });

        var hasTurnCompleted = false;
        var hasTurnFailed = false;
        var hasCompletedItem = false;

        await foreach (var threadEvent in streamed.Events.WithCancellation(cancellation.Token))
        {
            hasTurnCompleted |= threadEvent is TurnCompletedEvent;
            hasTurnFailed |= threadEvent is TurnFailedEvent;
            hasCompletedItem |= threadEvent is ItemCompletedEvent;
        }

        await Assert.That(hasCompletedItem).IsTrue();
        await Assert.That(hasTurnCompleted).IsTrue();
        await Assert.That(hasTurnFailed).IsFalse();
        await Assert.That(thread.Id).IsNotNull();
    }

    [Test]
    public async Task RunAsync_WithRealCodexCli_SecondTurnKeepsThreadId()
    {
        var settings = RealCodexTestSupport.TryGetSettings();
        if (settings is null)
        {
            return;
        }

        await using var client = RealCodexTestSupport.CreateClient(settings);
        var thread = StartRealIntegrationThread(client, settings.Model);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        var schema = StructuredOutputSchema.Map(
            new Dictionary<string, StructuredOutputSchema>
            {
                ["status"] = StructuredOutputSchema.PlainText(),
            },
            required: ["status"],
            additionalProperties: false);

        var first = await thread.RunAsync(
            "Reply with a JSON object where status is exactly \"ok\".",
            new TurnOptions
            {
                OutputSchema = schema,
                CancellationToken = cancellation.Token,
            });

        var firstThreadId = thread.Id;
        await Assert.That(firstThreadId).IsNotNull();
        await Assert.That(first.Usage).IsNotNull();

        var second = await thread.RunAsync(
            "Again: reply with a JSON object where status is exactly \"ok\".",
            new TurnOptions
            {
                OutputSchema = schema,
                CancellationToken = cancellation.Token,
            });

        var secondJson = JsonNode.Parse(second.FinalResponse)?.AsObject();
        await Assert.That(secondJson).IsNotNull();
        await Assert.That(secondJson!["status"]!.GetValue<string>()).IsEqualTo("ok");
        await Assert.That(second.Usage).IsNotNull();
        await Assert.That(thread.Id).IsEqualTo(firstThreadId);
    }

    private static CodexThread StartRealIntegrationThread(CodexClient client, string model)
    {
        return client.StartThread(new ThreadOptions
        {
            Model = model,
            ModelReasoningEffort = ModelReasoningEffort.Minimal,
            SandboxMode = SandboxMode.WorkspaceWrite,
            NetworkAccessEnabled = true,
        });
    }
}
