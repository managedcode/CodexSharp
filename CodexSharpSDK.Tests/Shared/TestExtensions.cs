namespace ManagedCode.CodexSharpSDK.Tests.Shared;

internal static class TestExtensions
{
    public static int IndexOf(this IReadOnlyList<string> values, string target)
    {
        for (var index = 0; index < values.Count; index += 1)
        {
            if (values[index] == target)
            {
                return index;
            }
        }

        return -1;
    }
}
