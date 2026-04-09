using System.Diagnostics;
using ManagedCode.CodexSharpSDK.Internal;
using ManagedCode.CodexSharpSDK.Models;

namespace ManagedCode.CodexSharpSDK.Tests.Performance;

public class ThreadEventParserPerformanceTests
{
    private static readonly string[] SupportedEventStream =
    [
        """{"type":"thread.started","thread_id":"thread_1"}""",
        """{"type":"turn.started"}""",
        """{"type":"turn.completed","usage":{"input_tokens":1,"cached_input_tokens":0,"output_tokens":2}}""",
        """{"type":"turn.failed","error":{"message":"failed"}}""",
        """{"type":"error","message":"fatal"}""",
        """{"type":"item.started","item":{"id":"started_1","type":"agent_message","text":"hello"}}""",
        """{"type":"item.updated","item":{"id":"updated_1","type":"reasoning","text":"plan"}}""",
        """{"type":"item.completed","item":{"id":"agent_1","type":"agent_message","text":"ok"}}""",
        """{"type":"item.completed","item":{"id":"reasoning_1","type":"reasoning","text":"thinking"}}""",
        """{"type":"item.completed","item":{"id":"cmd_1","type":"command_execution","command":"ls","aggregated_output":"","status":"in_progress"}}""",
        """{"type":"item.completed","item":{"id":"cmd_2","type":"command_execution","command":"ls","aggregated_output":"done","exit_code":0,"status":"completed"}}""",
        """{"type":"item.completed","item":{"id":"cmd_3","type":"command_execution","command":"ls","aggregated_output":"boom","exit_code":1,"status":"failed"}}""",
        """{"type":"item.started","item":{"id":"change_0","type":"file_change","changes":[{"path":"progress.txt","kind":"add"}],"status":"in_progress"}}""",
        """{"type":"item.completed","item":{"id":"change_1","type":"file_change","changes":[{"path":"a.txt","kind":"add"},{"path":"b.txt","kind":"update"},{"path":"c.txt","kind":"delete"}],"status":"completed"}}""",
        """{"type":"item.completed","item":{"id":"change_2","type":"file_change","changes":[{"path":"d.txt","kind":"update"}],"status":"failed"}}""",
        """{"type":"item.completed","item":{"id":"mcp_1","type":"mcp_tool_call","server":"srv","tool":"tool","arguments":{"x":1},"result":{"content":[{"type":"text","text":"ok"}],"structured_content":{"ok":true}},"status":"in_progress"}}""",
        """{"type":"item.completed","item":{"id":"mcp_2","type":"mcp_tool_call","server":"srv","tool":"tool","arguments":null,"result":{"content":[],"structured_content":null},"status":"completed"}}""",
        """{"type":"item.completed","item":{"id":"mcp_3","type":"mcp_tool_call","server":"srv","tool":"tool","arguments":{},"error":{"message":"mcp failed"},"status":"failed"}}""",
        """{"type":"item.completed","item":{"id":"web_1","type":"web_search","query":"q"}}""",
        """{"type":"item.completed","item":{"id":"todo_1","type":"todo_list","items":[{"text":"a","completed":true},{"text":"b","completed":false}]}}""",
        """{"type":"item.completed","item":{"id":"err_1","type":"error","message":"boom"}}""",
        """{"type":"item.completed","item":{"id":"collab_1","type":"collab_tool_call","tool":"spawn_agent","sender_thread_id":"sender","receiver_thread_ids":["a1"],"prompt":"delegate","agents_states":{"a1":{"status":"pending_init","message":null}},"status":"in_progress"}}""",
        """{"type":"item.completed","item":{"id":"collab_2","type":"collab_tool_call","tool":"send_input","sender_thread_id":"sender","receiver_thread_ids":["a2"],"prompt":"ping","agents_states":{"a2":{"status":"running","message":null}},"status":"completed"}}""",
        """{"type":"item.completed","item":{"id":"collab_3","type":"collab_tool_call","tool":"wait","sender_thread_id":"sender","receiver_thread_ids":["a3"],"prompt":null,"agents_states":{"a3":{"status":"completed","message":"done"},"a4":{"status":"errored","message":"failed"},"a5":{"status":"shutdown","message":null},"a6":{"status":"not_found","message":"missing"}},"status":"failed"}}""",
        """{"type":"item.completed","item":{"id":"collab_4","type":"collab_tool_call","tool":"close_agent","sender_thread_id":"sender","receiver_thread_ids":["a7"],"prompt":null,"agents_states":{"a7":{"status":"completed","message":"closed"}},"status":"completed"}}""",
    ];

    [Test]
    public async Task Parse_MixedSupportedEventStream_CompletesWithinBudgetAndCoversBranches()
    {
        const int iterations = 2_500;
        var expectedTotal = iterations * SupportedEventStream.Length;

        var eventKinds = new HashSet<string>(StringComparer.Ordinal);
        var itemKinds = new HashSet<string>(StringComparer.Ordinal);
        var commandStatuses = new HashSet<CommandExecutionStatus>();
        var patchStatuses = new HashSet<PatchApplyStatus>();
        var mcpStatuses = new HashSet<McpToolCallStatus>();
        var collabTools = new HashSet<CollabTool>();
        var collabToolStatuses = new HashSet<CollabToolCallStatus>();
        var collabAgentStatuses = new HashSet<CollabAgentStatus>();

        var stopwatch = Stopwatch.StartNew();
        var parsedCount = 0;

        for (var iteration = 0; iteration < iterations; iteration += 1)
        {
            foreach (var line in SupportedEventStream)
            {
                var parsed = ThreadEventParser.Parse(line);
                parsedCount += 1;
                eventKinds.Add(parsed.GetType().FullName ?? parsed.GetType().Name);

                switch (parsed)
                {
                    case ItemStartedEvent started:
                        itemKinds.Add(started.Item.GetType().FullName ?? started.Item.GetType().Name);
                        CollectItemState(started.Item);
                        break;

                    case ItemUpdatedEvent updated:
                        itemKinds.Add(updated.Item.GetType().FullName ?? updated.Item.GetType().Name);
                        CollectItemState(updated.Item);
                        break;

                    case ItemCompletedEvent completed:
                        itemKinds.Add(completed.Item.GetType().FullName ?? completed.Item.GetType().Name);
                        CollectItemState(completed.Item);
                        break;
                }
            }
        }

        stopwatch.Stop();

        await Assert.That(parsedCount).IsEqualTo(expectedTotal);
        await Assert.That(eventKinds).IsEquivalentTo(
        [
            typeof(ThreadStartedEvent).FullName!,
            typeof(TurnStartedEvent).FullName!,
            typeof(TurnCompletedEvent).FullName!,
            typeof(TurnFailedEvent).FullName!,
            typeof(ItemStartedEvent).FullName!,
            typeof(ItemUpdatedEvent).FullName!,
            typeof(ItemCompletedEvent).FullName!,
            typeof(ThreadErrorEvent).FullName!,
        ]);
        await Assert.That(itemKinds).IsEquivalentTo(
        [
            typeof(AgentMessageItem).FullName!,
            typeof(ReasoningItem).FullName!,
            typeof(CommandExecutionItem).FullName!,
            typeof(FileChangeItem).FullName!,
            typeof(McpToolCallItem).FullName!,
            typeof(WebSearchItem).FullName!,
            typeof(TodoListItem).FullName!,
            typeof(ErrorItem).FullName!,
            typeof(CollabToolCallItem).FullName!,
        ]);
        await Assert.That(commandStatuses).IsEquivalentTo(
        [
            CommandExecutionStatus.InProgress,
            CommandExecutionStatus.Completed,
            CommandExecutionStatus.Failed,
        ]);
        await Assert.That(patchStatuses).IsEquivalentTo(
        [
            PatchApplyStatus.InProgress,
            PatchApplyStatus.Completed,
            PatchApplyStatus.Failed,
        ]);
        await Assert.That(mcpStatuses).IsEquivalentTo(
        [
            McpToolCallStatus.InProgress,
            McpToolCallStatus.Completed,
            McpToolCallStatus.Failed,
        ]);
        await Assert.That(collabTools).IsEquivalentTo(
        [
            CollabTool.SpawnAgent,
            CollabTool.SendInput,
            CollabTool.Wait,
            CollabTool.CloseAgent,
        ]);
        await Assert.That(collabToolStatuses).IsEquivalentTo(
        [
            CollabToolCallStatus.InProgress,
            CollabToolCallStatus.Completed,
            CollabToolCallStatus.Failed,
        ]);
        await Assert.That(collabAgentStatuses).IsEquivalentTo(
        [
            CollabAgentStatus.PendingInit,
            CollabAgentStatus.Running,
            CollabAgentStatus.Completed,
            CollabAgentStatus.Errored,
            CollabAgentStatus.Shutdown,
            CollabAgentStatus.NotFound,
        ]);
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(20));

        void CollectItemState(ThreadItem item)
        {
            switch (item)
            {
                case CommandExecutionItem command:
                    commandStatuses.Add(command.Status);
                    break;

                case FileChangeItem fileChange:
                    patchStatuses.Add(fileChange.Status);
                    break;

                case McpToolCallItem mcp:
                    mcpStatuses.Add(mcp.Status);
                    break;

                case CollabToolCallItem collab:
                    collabTools.Add(collab.Tool);
                    collabToolStatuses.Add(collab.Status);
                    foreach (var state in collab.AgentsStates.Values)
                    {
                        collabAgentStatuses.Add(state.Status);
                    }

                    break;
            }
        }
    }
}
