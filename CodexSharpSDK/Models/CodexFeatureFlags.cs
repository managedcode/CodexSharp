namespace ManagedCode.CodexSharpSDK.Models;

/// <summary>
/// Known Codex CLI feature flag identifiers for use with
/// <see cref="ManagedCode.CodexSharpSDK.Client.ThreadOptions.EnabledFeatures"/> and
/// <see cref="ManagedCode.CodexSharpSDK.Client.ThreadOptions.DisabledFeatures"/>.
/// Feature flags are passed to the CLI via <c>--enable</c> and <c>--disable</c>.
/// </summary>
public static class CodexFeatureFlags
{
    // --- Approval / sandbox ---
    public const string GuardianApproval = "guardian_approval";
    public const string RequestPermissions = "request_permissions";
    public const string RequestPermissionsTool = "request_permissions_tool";

    // --- Execution ---
    public const string UnifiedExec = "unified_exec";
    public const string ExperimentalUseUnifiedExecTool = "experimental_use_unified_exec_tool";
    public const string ShellTool = "shell_tool";
    public const string ShellSnapshot = "shell_snapshot";
    public const string ShellZshFork = "shell_zsh_fork";
    public const string UseLinuxSandboxBwrap = "use_linux_sandbox_bwrap";

    // --- Apply patch ---
    public const string ApplyPatchFreeform = "apply_patch_freeform";
    public const string ExperimentalUseFreeformApplyPatch = "experimental_use_freeform_apply_patch";
    public const string IncludeApplyPatchTool = "include_apply_patch_tool";

    // --- MCP / tool calling ---
    public const string ToolCallMcpElicitation = "tool_call_mcp_elicitation";

    // --- Multi-agent / collaboration ---
    public const string MultiAgent = "multi_agent";
    public const string Collab = "collab";
    public const string CollaborationModes = "collaboration_modes";
    public const string ChildAgentsMd = "child_agents_md";
    public const string Steer = "steer";

    // --- Search ---
    public const string SearchTool = "search_tool";
    public const string WebSearch = "web_search";
    public const string WebSearchCached = "web_search_cached";
    public const string WebSearchRequest = "web_search_request";

    // --- Memory ---
    public const string Memories = "memories";
    public const string MemoryTool = "memory_tool";

    // --- Image ---
    public const string ImageGeneration = "image_generation";
    public const string ImageDetailOriginal = "image_detail_original";

    // --- Plugins / apps ---
    public const string Plugins = "plugins";
    public const string Apps = "apps";
    public const string AppsMcpGateway = "apps_mcp_gateway";
    public const string Connectors = "connectors";

    // --- JS REPL ---
    public const string JsRepl = "js_repl";
    public const string JsReplToolsOnly = "js_repl_tools_only";

    // --- Realtime ---
    public const string RealtimeConversation = "realtime_conversation";
    public const string VoiceTranscription = "voice_transcription";

    // --- Windows sandbox ---
    public const string ExperimentalWindowsSandbox = "experimental_windows_sandbox";
    public const string EnableExperimentalWindowsSandbox = "enable_experimental_windows_sandbox";
    public const string ElevatedWindowsSandbox = "elevated_windows_sandbox";
    public const string PowershellUtf8 = "powershell_utf8";

    // --- Storage / DB ---
    public const string Sqlite = "sqlite";
    public const string RemoteModels = "remote_models";

    // --- Networking / transport ---
    public const string ResponsesWebsockets = "responses_websockets";
    public const string ResponsesWebsocketsV2 = "responses_websockets_v2";
    public const string EnableRequestCompression = "enable_request_compression";

    // --- Misc ---
    public const string FastMode = "fast_mode";
    public const string Artifact = "artifact";
    public const string RequestRule = "request_rule";
    public const string RuntimeMetrics = "runtime_metrics";
    public const string Undo = "undo";
    public const string Personality = "personality";
    public const string SkillEnvVarDependencyPrompt = "skill_env_var_dependency_prompt";
    public const string SkillMcpDependencyInstall = "skill_mcp_dependency_install";
    public const string CodexGitCommit = "codex_git_commit";
    public const string DefaultModeRequestUserInput = "default_mode_request_user_input";
    public const string PreventIdleSleep = "prevent_idle_sleep";
}
