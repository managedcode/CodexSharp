namespace ManagedCode.CodexSharpSDK;

public abstract record UserInput;

public sealed record TextInput(string Text) : UserInput;

public sealed record LocalImageInput : UserInput
{
    internal string? Path { get; }

    internal FileInfo? File { get; }

    internal Stream? Content { get; }

    internal string? FileName { get; }

    internal bool LeaveOpen { get; }

    public LocalImageInput(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
    }

    public LocalImageInput(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        File = file;
    }

    public LocalImageInput(Stream content, string? fileName = null, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new ArgumentException("Image stream must be readable.", nameof(content));
        }

        Content = content;
        FileName = fileName;
        LeaveOpen = leaveOpen;
    }

    public static LocalImageInput FromPath(string path) => new(path);

    public static LocalImageInput FromFile(FileInfo file) => new(file);

    public static LocalImageInput FromStream(Stream content, string? fileName = null, bool leaveOpen = false)
        => new(content, fileName, leaveOpen);
}
