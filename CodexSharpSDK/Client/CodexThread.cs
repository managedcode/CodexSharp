using System.Runtime.CompilerServices;
using ManagedCode.CodexSharpSDK.Configuration;
using ManagedCode.CodexSharpSDK.Exceptions;
using ManagedCode.CodexSharpSDK.Execution;
using ManagedCode.CodexSharpSDK.Internal;
using ManagedCode.CodexSharpSDK.Logging;
using ManagedCode.CodexSharpSDK.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManagedCode.CodexSharpSDK.Client;

public sealed class CodexThread
    : IDisposable
{
    private readonly CodexExec _exec;
    private readonly CodexOptions _options;
    private readonly ThreadOptions _threadOptions;
    // One active turn per thread instance (ADR 002).
    private readonly SemaphoreSlim _turnGate = new(1, 1);
    private int _disposed;
    private string? _id;

    internal CodexThread(
        CodexExec exec,
        CodexOptions options,
        ThreadOptions threadOptions,
        string? id = null)
    {
        _exec = exec;
        _options = options;
        _threadOptions = threadOptions;
        _id = id;
    }

    public string? Id => Volatile.Read(ref _id);

    public Task<RunStreamedResult> RunStreamedAsync(string input, TurnOptions? turnOptions = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(input);
        var normalizedInput = new NormalizedInput(input, []);
        var resolvedTurnOptions = turnOptions ?? new TurnOptions();
        return Task.FromResult(new RunStreamedResult(RunStreamedWithTurnGateAsync(normalizedInput, resolvedTurnOptions)));
    }

    public Task<RunStreamedResult> RunStreamedAsync(IReadOnlyList<UserInput> input, TurnOptions? turnOptions = null)
    {
        ThrowIfDisposed();
        var normalizedInput = NormalizeInput(input);
        var resolvedTurnOptions = turnOptions ?? new TurnOptions();
        return Task.FromResult(new RunStreamedResult(RunStreamedWithTurnGateAsync(normalizedInput, resolvedTurnOptions)));
    }

    public Task<RunResult> RunAsync(string input, TurnOptions? turnOptions = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(input);
        var normalizedInput = new NormalizedInput(input, []);
        return RunInternalAsync(normalizedInput, turnOptions ?? new TurnOptions());
    }

    public Task<RunResult> RunAsync(IReadOnlyList<UserInput> input, TurnOptions? turnOptions = null)
    {
        ThrowIfDisposed();
        var normalizedInput = NormalizeInput(input);
        return RunInternalAsync(normalizedInput, turnOptions ?? new TurnOptions());
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _turnGate.Dispose();
    }

    private async Task<RunResult> RunInternalAsync(NormalizedInput normalizedInput, TurnOptions turnOptions)
    {
        var items = new List<ThreadItem>();
        var finalResponse = string.Empty;
        Usage? usage = null;
        ThreadError? turnFailure = null;

        await foreach (var threadEvent in RunStreamedWithTurnGateAsync(normalizedInput, turnOptions)
                           .ConfigureAwait(false))
        {
            switch (threadEvent)
            {
                case ItemCompletedEvent itemCompletedEvent:
                    if (itemCompletedEvent.Item is AgentMessageItem message)
                    {
                        finalResponse = message.Text;
                    }

                    items.Add(itemCompletedEvent.Item);
                    break;

                case TurnCompletedEvent turnCompletedEvent:
                    usage = turnCompletedEvent.Usage;
                    break;

                case TurnFailedEvent turnFailedEvent:
                    turnFailure = turnFailedEvent.Error;
                    break;
            }

            if (turnFailure is not null)
            {
                break;
            }
        }

        if (turnFailure is not null)
        {
            throw new ThreadRunException(turnFailure.Message);
        }

        return new RunResult(items, finalResponse, usage);
    }

    private async IAsyncEnumerable<ThreadEvent> RunStreamedWithTurnGateAsync(
        NormalizedInput normalizedInput,
        TurnOptions turnOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            turnOptions.CancellationToken,
            cancellationToken);

        var linkedCancellationToken = linkedCancellationTokenSource.Token;

        await _turnGate.WaitAsync(linkedCancellationToken).ConfigureAwait(false);
        try
        {
            await foreach (var threadEvent in RunStreamedInternalAsync(normalizedInput, turnOptions, linkedCancellationToken)
                               .WithCancellation(linkedCancellationToken)
                               .ConfigureAwait(false))
            {
                yield return threadEvent;
            }
        }
        finally
        {
            _turnGate.Release();
        }
    }

    private async IAsyncEnumerable<ThreadEvent> RunStreamedInternalAsync(
        NormalizedInput normalizedInput,
        TurnOptions turnOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var resolvedImages = await ResolvedImages
            .CreateAsync(normalizedInput.Images, _options.Logger, cancellationToken)
            .ConfigureAwait(false);

        await using var outputSchemaFile = await OutputSchemaFile
            .CreateAsync(turnOptions.OutputSchema, _options.Logger, cancellationToken)
            .ConfigureAwait(false);

        var execArgs = new CodexExecArgs
        {
            Input = normalizedInput.Prompt,
            BaseUrl = _options.BaseUrl,
            ApiKey = _options.ApiKey,
            ThreadId = _id,
            Images = resolvedImages.Paths,
            Model = _threadOptions.Model,
            SandboxMode = _threadOptions.SandboxMode,
            WorkingDirectory = _threadOptions.WorkingDirectory,
            AdditionalDirectories = _threadOptions.AdditionalDirectories,
            SkipGitRepoCheck = _threadOptions.SkipGitRepoCheck,
            OutputSchemaFile = outputSchemaFile.SchemaPath,
            ModelReasoningEffort = _threadOptions.ModelReasoningEffort,
            NetworkAccessEnabled = _threadOptions.NetworkAccessEnabled,
            WebSearchMode = _threadOptions.WebSearchMode,
            WebSearchEnabled = _threadOptions.WebSearchEnabled,
            ApprovalPolicy = _threadOptions.ApprovalPolicy,
            CancellationToken = cancellationToken,
        };

        await foreach (var line in _exec.RunAsync(execArgs)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            ThreadEvent parsedEvent;
            try
            {
                parsedEvent = ThreadEventParser.Parse(line);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to parse item: {line}", exception);
            }

            if (parsedEvent is ThreadStartedEvent startedEvent)
            {
                Volatile.Write(ref _id, startedEvent.ThreadId);
            }

            yield return parsedEvent;
        }
    }

    private static NormalizedInput NormalizeInput(IReadOnlyList<UserInput> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var promptParts = new List<string>();
        var images = new List<LocalImageInput>();

        foreach (var item in input)
        {
            switch (item)
            {
                case TextInput textInput:
                    promptParts.Add(textInput.Text);
                    break;
                case LocalImageInput localImageInput:
                    images.Add(localImageInput);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported input type: {item.GetType().Name}");
            }
        }

        return new NormalizedInput(string.Join("\n\n", promptParts), images);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, nameof(CodexThread));
    }

    private readonly record struct NormalizedInput(string Prompt, IReadOnlyList<LocalImageInput> Images);

    private sealed class ResolvedImages : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyList<string> _paths;
        private readonly IReadOnlyList<string> _temporaryFiles;
        private readonly IReadOnlyList<Stream> _streamsToDispose;

        private ResolvedImages(
            ILogger? logger,
            IReadOnlyList<string> paths,
            IReadOnlyList<string> temporaryFiles,
            IReadOnlyList<Stream> streamsToDispose)
        {
            _logger = logger ?? NullLogger.Instance;
            _paths = paths;
            _temporaryFiles = temporaryFiles;
            _streamsToDispose = streamsToDispose;
        }

        public IReadOnlyList<string> Paths => _paths;

        public static async Task<ResolvedImages> CreateAsync(
            IReadOnlyList<LocalImageInput> images,
            ILogger? logger,
            CancellationToken cancellationToken)
        {
            if (images.Count == 0)
            {
                return new ResolvedImages(logger, [], [], []);
            }

            var resolvedPaths = new List<string>(images.Count);
            var temporaryFiles = new List<string>();
            var streamsToDispose = new List<Stream>();

            foreach (var image in images)
            {
                if (image.Path is not null)
                {
                    resolvedPaths.Add(image.Path);
                    continue;
                }

                if (image.File is not null)
                {
                    resolvedPaths.Add(image.File.FullName);
                    continue;
                }

                if (image.Content is null)
                {
                    throw new InvalidOperationException("Unsupported local image input shape.");
                }

                var tempPath = BuildTempImagePath(image.FileName);
                await using (var tempFile = new FileStream(
                                 tempPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.Read,
                                 bufferSize: 81920,
                                 FileOptions.Asynchronous))
                {
                    await image.Content.CopyToAsync(tempFile, cancellationToken).ConfigureAwait(false);
                }

                resolvedPaths.Add(tempPath);
                temporaryFiles.Add(tempPath);

                if (!image.LeaveOpen)
                {
                    streamsToDispose.Add(image.Content);
                }
            }

            return new ResolvedImages(logger, resolvedPaths, temporaryFiles, streamsToDispose);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var stream in _streamsToDispose)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            foreach (var tempFile in _temporaryFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (IOException exception)
                {
                    CodexThreadLog.TemporaryImageDeleteFailed(_logger, tempFile, exception);
                }
                catch (UnauthorizedAccessException exception)
                {
                    CodexThreadLog.TemporaryImageDeleteFailed(_logger, tempFile, exception);
                }
            }
        }

        private static string BuildTempImagePath(string? fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".img";
            }

            return Path.Combine(
                Path.GetTempPath(),
                $"codexsharp-image-{Guid.NewGuid():N}{extension}");
        }
    }
}
