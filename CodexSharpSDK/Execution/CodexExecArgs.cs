using ManagedCode.CodexSharpSDK.Client;

namespace ManagedCode.CodexSharpSDK.Execution;

public sealed record CodexExecArgs
{
    private bool _ephemeral;
    private bool _hasEphemeralOverride;

    public required string Input { get; init; }

    public string? BaseUrl { get; init; }

    public string? ApiKey { get; init; }

    public string? ThreadId { get; init; }

    public IReadOnlyList<string>? Images { get; init; }

    public string? Model { get; init; }

    public SandboxMode? SandboxMode { get; init; }

    public string? WorkingDirectory { get; init; }

    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    public string? Profile { get; init; }

    public bool UseOss { get; init; }

    public OssProvider? LocalProvider { get; init; }

    public bool FullAuto { get; init; }

    public bool DangerouslyBypassApprovalsAndSandbox { get; init; }

    public bool Ephemeral
    {
        get => _ephemeral;
        init
        {
            _ephemeral = value;
            _hasEphemeralOverride = true;
        }
    }

    public ExecOutputColor? Color { get; init; }

    public bool ProgressCursor { get; init; }

    public string? OutputLastMessageFile { get; init; }

    public IReadOnlyList<string>? EnabledFeatures { get; init; }

    public IReadOnlyList<string>? DisabledFeatures { get; init; }

    public IReadOnlyList<string>? AdditionalCliArguments { get; init; }

    public bool SkipGitRepoCheck { get; init; }

    public string? OutputSchemaFile { get; init; }

    public ModelReasoningEffort? ModelReasoningEffort { get; init; }

    public bool? NetworkAccessEnabled { get; init; }

    public WebSearchMode? WebSearchMode { get; init; }

    public bool? WebSearchEnabled { get; init; }

    public ApprovalMode? ApprovalPolicy { get; init; }

    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    internal bool HasEphemeralOverride => _hasEphemeralOverride;
}
